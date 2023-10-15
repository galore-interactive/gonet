using System;
using System.Net;

using System.Runtime.CompilerServices;

namespace NetcodeIO.NET.Utils
{
	internal static class MiscUtils
	{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CompareAddress(this IPEndPoint lhs, IPEndPoint rhs)
		{
			return lhs.Address.Equals(rhs.Address);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CompareEndpoint(this IPEndPoint lhs, IPEndPoint rhs, int port)
		{
			return lhs.Port == port && lhs.Address.Equals(rhs.Address);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreEndPointsEqual(EndPoint lhs, EndPoint rhs)
		{
            IPEndPoint lhsIP = lhs as IPEndPoint;
            IPEndPoint rhsIP = rhs as IPEndPoint;

            return lhsIP == null
                ? false
                : (rhsIP == null ? (lhs == null ? false : lhs.Equals(rhs)) : CompareEndpoint(lhsIP, rhsIP, rhsIP.Port));
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddressEqual(EndPoint lhs, EndPoint rhs)
        {
            IPEndPoint lhsIP = lhs as IPEndPoint;
            IPEndPoint rhsIP = rhs as IPEndPoint;

            return lhsIP == null
                ? false
                : (rhsIP == null ? (lhs == null ? false : lhs.Equals(rhs)) : CompareAddress(lhsIP, rhsIP));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool MatchChars(char[] chars, string match)
		{
			if (chars.Length != match.Length) return false;

			for (int i = 0; i < chars.Length; i++)
				if (chars[i] != match[i]) return false;

			return true;
		}
		
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ByteArraysEqual(byte[] lhs, byte[] rhs)
		{
			if (lhs.Length != rhs.Length) return false;
			for (int i = 0; i < lhs.Length; i++)
				if (lhs[i] != rhs[i]) return false;

			return true;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SequenceBytesRequired(ulong sequence)
		{
			int sequenceBytes = 0;
			
			while (sequence > 0)
			{
				sequenceBytes++;
				sequence >>= 8;
			}

			if (sequenceBytes == 0)
				sequenceBytes = 1;

			return sequenceBytes;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CompareByteArraysConstantTime(byte[] lhs, byte[] rhs)
		{
			int min = Math.Min(lhs.Length, rhs.Length);

			bool compare = true;

			for (int i = 0; i < min; i++)
				compare &= ((lhs[i] & rhs[i]) == lhs[i]);

			return compare;
		}

#if UNSAFE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool CompareHMACConstantTime(byte[] lhs, byte[] rhs)
		{
			if (lhs.Length != Defines.MAC_SIZE || rhs.Length != Defines.MAC_SIZE)
				throw new InvalidOperationException();

			// fast constant-time comparison for 16-byte MAC which gets away with just two compares (first 8 bytes, then second 8 bytes)
			bool compare = true;
			fixed (byte* lb = &lhs[0])
			{
				fixed (byte* rb = &rhs[0])
				{
					ulong* left = (ulong*)lb;
					ulong* right = (ulong*)rb;

					compare &= (*left == *right);

					left++;
					right++;

					compare &= (*left == *right);
				}
			}

			return compare;
		}
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CompareHMACConstantTime(byte[] lhs, byte[] rhs)
		{
			if (lhs.Length != Defines.MAC_SIZE || rhs.Length != Defines.MAC_SIZE)
				throw new InvalidOperationException();

			bool compare = true;
			for( int i = 0; i < Defines.MAC_SIZE; i++ )
				compare &= (lhs[i] == rhs[i]);

			return compare;
		}
#endif
	}
}
