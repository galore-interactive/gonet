/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GONet.Utils
{
    public static class GCLessAlgorithms
    {
        #region quick sort

        static readonly Stack<ValueTuple<int, int>> quickSortStack = new Stack<ValueTuple<int, int>>(50000);
        
        /// <summary>
        /// Quick sorts a list iteratively versus recursively. This should NOT be called from multiple threads.
        /// </summary>
        public  static void QuickSort<T>(List<T> list, IComparer<T> comparer, int iLeft = -1, int iRight = -1)
        {
            if (list == null ||
                list.Count == 0 ||
                (iLeft != -1 && (iLeft < 0 || (iRight != -1 && iLeft > iRight))) ||
                (iRight != -1 && (iRight >= list.Count || iRight < iLeft)))
            {
                throw new ArgumentOutOfRangeException("Try passing in proper inputs boss hog!");
            }

            iLeft = iLeft == -1 ? 0 : iLeft;
            int length = list.Count;
            iRight = iRight == -1 ? length - 1 : iRight;

            int iPivot;
            while (true)
            {
                if (iLeft < iRight)
                {
                    iPivot = QuickSort_GetNewPartitionPivotIndex(list, comparer, quickSortStack, iLeft, iRight);
                    quickSortStack.Push(ValueTuple.Create(iPivot + 1, iRight));
                    iRight = iPivot - 1;
                    continue;
                }

                if (quickSortStack.Count > 0)
                {
                    var newPair = quickSortStack.Pop();
                    iLeft = newPair.Item1;
                    iRight = newPair.Item2;
                    continue;
                }

                break;
            }
        }

        private static int QuickSort_GetNewPartitionPivotIndex<T>(List<T> list, IComparer<T> comparer, Stack<ValueTuple<int, int>> stack, int iStart, int iEnd)
        {
            int pivotIndex = (iStart + iEnd) / 2;
            T pivotValue = list[pivotIndex];
            list[pivotIndex] = list[iEnd];
            list[iEnd] = pivotValue;
            T temp;
            int iStore = iStart;
            for (int i = iStart; i < iEnd; ++i)
            {
                if (comparer.Compare(list[i], pivotValue) < 0)
                {
                    temp = list[iStore];
                    list[iStore] = list[i];
                    list[i] = temp;
                    ++iStore;
                }
            }

            list[iEnd] = list[iStore];
            list[iStore] = pivotValue;

            return iStore;
        }

        #endregion
    }
}
