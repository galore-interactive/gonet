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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GONet.Utils
{
    /// <summary>
    /// NOT thread-safe!
    /// </summary>
    public class ArrayPool<T> : ObjectPoolBase<T[]>
    {
        int arraySizeMinimum;
        int arraySizeMaximum;
        int currentMinimumSizeToBorrow;
        Random random = new Random();

        /// <summary>
        /// This can be passed in to <see cref="SortBorrowedArrays(IComparer{T[]})"/> and/or <see cref="SortAvailableArrays(IComparer{T[]})"/>
        /// </summary>
        public static readonly IComparer<T[]> ARRAY_LENGTH_ASCENDING_COMPARER = FunctionalComparer<T[]>.Create(Compare_ArrayLength_Ascending);

        public ArrayPool(int initialSize, int growByCount, int arraySizeMinimum, int arraySizeMaximum, Action<T[]> objectInitializer = null)
            : base(0, growByCount, objectInitializer)
        {
            Debug.Assert(arraySizeMinimum >= 0);
            Debug.Assert(arraySizeMaximum >= arraySizeMinimum);

            this.arraySizeMinimum = arraySizeMinimum;
            this.arraySizeMaximum = arraySizeMaximum;

            currentMinimumSizeToBorrow = random.Next(arraySizeMinimum, arraySizeMaximum + 1);

            CreateAdditionalInstances(initialSize);
            SortAvailableArrays_ByArrayLengthAscending();
        }

        /// <returns>An array of size no less than <paramref name="minimumSize"/></returns>
        public T[] Borrow(int minimumSize)
        {
            currentMinimumSizeToBorrow = minimumSize;
            T[] borrowed = Borrow(); // This will call PreBorrow() to ensure we get a rightly sized array (using currentMinimumSizeToBorrow to make sure the right size is returned!)
            return borrowed;
        }

        /// <summary>
        /// Ensure the array at the <see cref="nextAvailableIndex"/> is a rightly sized array, with a minimum size of <see cref="currentMinimumSizeToBorrow"/>
        /// </summary>
        protected override void PreBorrow()
        {
            base.PreBorrow();

            int iSmallestFit = -1;
            for (int i = nextAvailableIndex; i < poolCapacity; ++i)
            {
                if (pool[i].Length >= currentMinimumSizeToBorrow)
                {
                    iSmallestFit = i;
                    break;
                }
            }

            if (iSmallestFit == -1) // if no fit found, we'll have to create/add one and identify that one as the smallest fit!
            {
                const string OP = "[";
                const string DI = "] array of that size or larger not available to borrow. Debug Info: ";
                // commenting out since we may get to the point where pools need to grow like this and log flood will make things SLOOOOOW: GONetLog.Debug(string.Concat(typeof(T).Name, OP, currentMinimumSizeToBorrow, DI, ToString()));

                int arraySizeMinimum_original = arraySizeMinimum;
                int arraySizeMaximum_original = arraySizeMaximum;
                arraySizeMinimum = currentMinimumSizeToBorrow;  // set the minimum array size to use when growing pool (i.e, creating additional instances) to the value we want for now to ensure we get at least one element of the proper size!
                arraySizeMaximum = (int)(currentMinimumSizeToBorrow * 1.5f);  // set the maximum array size to use when growing pool (i.e, creating additional instances) to the value just larger than what we want for now ... this ASSumes subsequent requests might be of a similar, but perhaps larger size

                iSmallestFit = poolCapacity; // this will be valid after we add the new instances below..it will point to the first new instance
                CreateAdditionalInstances(growByCount);

                /* avoid going back to the smaller/original settings since we are getting requests for larger....just keep it larger and hope we are not gobbling up too much more memory as a result
                arraySizeMinimum = arraySizeMinimum_original;
                arraySizeMaximum = arraySizeMaximum_original;
                */
            }

            // NOTE: by now, iSmallestFit is guaranteed to be valid, but just double check that pure luck did not end up where the next available is the right one to serve up before doing a swap!
            if (iSmallestFit != nextAvailableIndex)
            { // swap the iSmallestFit element to the place that will be used to borrow from in Borrow()
                T[] tmp = pool[nextAvailableIndex];
                pool[nextAvailableIndex] = pool[iSmallestFit];
                pool[iSmallestFit] = tmp;
            }
        }

        protected override T[] CreateSingleInstance()
        {
            int rangedSize = random.Next(arraySizeMinimum, arraySizeMaximum + 1);
            T[] array = new T[rangedSize];
            return array;
        }

        /// <summary>
        /// IMPORTANT: This will create garbage!
        /// </summary>
        public virtual void SortBorrowedArrays(IComparer<T[]> comparer)
        {
            Array.Sort<T[]>(pool, 0, nextAvailableIndex, comparer);
        }

        public virtual void SortBorrowedArrays_ByArrayLengthAscending()
        {
            Sort_Quick(pool, 0, nextAvailableIndex - 1);
        }

        public virtual void SortAvailableArrays_ByArrayLengthAscending()
        {
            Sort_Quick(pool, iLeft: nextAvailableIndex);
        }

        #region SortXxxArrays_ByArrayLengthAscending helpers
        static readonly Stack<ValueTuple<int, int>> Sort_Quick_stack = new Stack<ValueTuple<int, int>>(50000);
        /// <summary>
        /// Quick sorts an array.
        /// Does NOT use recursion.
        /// NOT thread safe.
        /// </summary>
        private static void Sort_Quick(T[][] pool, int iLeft = -1, int iRight = -1)
        {
            if (pool == null ||
                pool.Length == 0 ||
                (iLeft != -1 && (iLeft < 0 || (iRight != -1 && iLeft > iRight))) ||
                (iRight != -1 && (iRight >= pool.Length || iRight < iLeft)))
            {
                throw new ArgumentOutOfRangeException("you were not smart this time!");
            }

            iLeft = iLeft == -1 ? 0 : iLeft;
            int length = pool.Length;
            iRight = iRight == -1 ? length - 1 : iRight;

            int iPivot;
            while (true)
            {
                if (iLeft < iRight)
                {
                    iPivot = Sort_Quick_GetNewPartitionPivotIndex(pool, Sort_Quick_stack, iLeft, iRight);
                    Sort_Quick_stack.Push(ValueTuple.Create(iPivot + 1, iRight));
                    iRight = iPivot - 1;
                    continue;
                }

                if (Sort_Quick_stack.Count > 0)
                {
                    var newPair = Sort_Quick_stack.Pop();
                    iLeft = newPair.Item1;
                    iRight = newPair.Item2;
                    continue;
                }

                break;
            }
        }

        private static int Sort_Quick_GetNewPartitionPivotIndex(T[][] input, Stack<ValueTuple<int, int>> stack, int iStart, int iEnd)
        {
            int pivotIndex = (iStart + iEnd) / 2;
            T[] pivotValue = input[pivotIndex];
            input[pivotIndex] = input[iEnd];
            input[iEnd] = pivotValue;
            T[] temp;
            int iStore = iStart;
            for (int i = iStart; i < iEnd; ++i)
            {
                if (input[i].Length < pivotValue.Length)
                {
                    temp = input[iStore];
                    input[iStore] = input[i];
                    input[i] = temp;
                    ++iStore;
                }
            }

            input[iEnd] = input[iStore];
            input[iStore] = pivotValue;

            return iStore;
        }
        #endregion

        /// <summary>
        /// IMPORTANT: This will create garbage!  If that is a problem, see if this suits your needs: <see cref="SortAvailableArrays_ByArrayLengthAscending"/> as it does not create garbage.
        /// </summary>
        public virtual void SortAvailableArrays(IComparer<T[]> comparer)
        {
            Array.Sort<T[]>(pool, nextAvailableIndex, poolCapacity - nextAvailableIndex, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Compare_ArrayLength_Ascending(T[] x, T[] y)
        {
            int lenX = x.Length;
            int lenY = y.Length;
            return lenX < lenY ? -1 : lenX > lenY ? 1 : 0;
        }
    }
}
