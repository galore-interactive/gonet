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

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;

namespace GONet.Utils
{
    public static class FileUtils
    {
        public static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static void WriteBytesToFile(string filePath, byte[] bytes, int bytesUsedCount, FileMode fileMode)
        {
            if (File.Exists(filePath))
            {
                using (Stream stream = new FileStream(filePath, fileMode), bufferedStream = new BufferedStream(stream))
                {
                    bufferedStream.Write(bytes, 0, bytesUsedCount);
                }
            }
            else
            {
                using (Stream stream = File.Create(filePath), bufferedStream = new BufferedStream(stream))
                {
                    bufferedStream.Write(bytes, 0, bytesUsedCount);
                }
            }
        }

        public static bool DoFilesHaveSameContents(string pathA, string pathB)
        {
            // Check if both files exist
            if (!File.Exists(pathA) || !File.Exists(pathB))
            {
                return false;
            }

            // Check file sizes first
            FileInfo fileInfoA = new FileInfo(pathA);
            FileInfo fileInfoB = new FileInfo(pathB);

            if (fileInfoA.Length != fileInfoB.Length)
            {
                return false; // Files have different sizes
            }

            // Compare file contents
            using (FileStream streamA = File.OpenRead(pathA))
            using (FileStream streamB = File.OpenRead(pathB))
            {
                const int bufferSize = 8192; // 8KB buffer
                byte[] bufferA = new byte[bufferSize];
                byte[] bufferB = new byte[bufferSize];

                int bytesReadA;
                int bytesReadB;

                while ((bytesReadA = streamA.Read(bufferA, 0, bufferSize)) > 0 &&
                       (bytesReadB = streamB.Read(bufferB, 0, bufferSize)) > 0)
                {
                    if (bytesReadA != bytesReadB || !bufferA.AsSpan(0, bytesReadA).SequenceEqual(bufferB.AsSpan(0, bytesReadB)))
                    {
                        return false; // Contents are different
                    }
                }
            }

            return true; // Files have the same contents
        }

        public static unsafe bool DoFilesHaveSameContentsFastest(string pathA, string pathB)
        {
            // Check if both files exist
            if (!File.Exists(pathA) || !File.Exists(pathB))
            {
                return false;
            }

            // Check file sizes first
            FileInfo fileInfoA = new FileInfo(pathA);
            FileInfo fileInfoB = new FileInfo(pathB);

            if (fileInfoA.Length != fileInfoB.Length)
            {
                return false; // Files have different sizes
            }

            // Compare file contents
            using (FileStream streamA = File.OpenRead(pathA))
            using (FileStream streamB = File.OpenRead(pathB))
            {
                const int bufferSize = 8192; // 8KB buffer
                byte[] bufferA = new byte[bufferSize];
                byte[] bufferB = new byte[bufferSize];

                int bytesReadA;
                int bytesReadB;

                while ((bytesReadA = streamA.Read(bufferA, 0, bufferSize)) > 0 &&
                       (bytesReadB = streamB.Read(bufferB, 0, bufferSize)) > 0)
                {
                    if (bytesReadA != bytesReadB || !BuffersAreEqualUnsafe(bufferA, bufferB, bytesReadA))
                    {
                        return false; // Contents are different
                    }
                }
            }

            return true; // Files have the same contents
        }

        private static unsafe bool BuffersAreEqualUnsafe(byte[] bufferA, byte[] bufferB, int count)
        {
            fixed (byte* ptrA = bufferA, ptrB = bufferB)
            {
                byte* a = ptrA;
                byte* b = ptrB;

                for (int i = 0; i < count; i++)
                {
                    if (*a != *b)
                    {
                        return false;
                    }
                    a++;
                    b++;
                }
            }
            return true;
        }

        public static void CopyFile(string fromFileAtPath, string overwriteToFileAtPath)
        {
            if (!File.Exists(fromFileAtPath))
            {
                throw new FileNotFoundException($"Source file not found: {fromFileAtPath}");
            }

            string destinationDirectory = Path.GetDirectoryName(overwriteToFileAtPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(fromFileAtPath, overwriteToFileAtPath, overwrite: true);
        }
    }
}