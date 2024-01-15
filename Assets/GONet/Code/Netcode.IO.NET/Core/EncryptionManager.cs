using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NetcodeIO.NET.Utils;

namespace NetcodeIO.NET
{
	internal class EncryptionManager
	{
		internal struct encryptionMapEntry
		{
            double expireTime;
			public double ExpiresAtSeconds { get => expireTime; set { expireTime = value; isReset = false; } }

            double lastAccessedAtSeconds;
			public double LastAccessedAtSeconds { get => lastAccessedAtSeconds; set { lastAccessedAtSeconds = value; isReset = false; } }

            int timeoutAfterSeconds;
            public int TimeoutAfterSeconds { get => timeoutAfterSeconds; set { timeoutAfterSeconds = value; isReset = false; } }

            uint clientID;
            public uint ClientID { get => clientID; set { clientID = value; isReset = false; } }

            EndPoint address;
            public EndPoint Address { get => address; set { address = value; isReset = false; } }

            public byte[] SendKey;

            public byte[] ReceiveKey;

            private bool isReset;
            public bool IsReset => isReset;

            public void Reset()
			{
                ExpiresAtSeconds = -1.0;
				LastAccessedAtSeconds = -1000.0;
				Address = null;
				TimeoutAfterSeconds = 0;
				ClientID = 0;

				Array.Clear(SendKey, 0, SendKey.Length);
				Array.Clear(ReceiveKey, 0, ReceiveKey.Length);

                isReset = true;
            }
        }

		internal int encyrptionMappings_usedCount;
        private int encyrptionMappings_totalCount;
		internal encryptionMapEntry[] encryptionMappings;

        /// <summary>
        /// This set will contain the indexes to remove when calling to <see cref="RemoveAllEncryptionMappings"/>. Be sure to clear it before using it in case it is dirty from the previous use.
        /// </summary>
        private readonly HashSet<int> removedIndexes;


        public EncryptionManager(int maxClients)
		{
			encryptionMappings = new encryptionMapEntry[maxClients * 4];
			encyrptionMappings_totalCount = encryptionMappings.Length;

            for (int i = 0; i < encyrptionMappings_totalCount; ++i)
			{
				encryptionMappings[i].SendKey = new byte[32];
				encryptionMappings[i].ReceiveKey = new byte[32];
			}

			Reset();

            removedIndexes = new HashSet<int>();
        }

		public void Reset()
		{
			encyrptionMappings_usedCount = 0;
			for (int i = 0; i < encyrptionMappings_totalCount; ++i)
			{
				encryptionMappings[i].Reset();
			}
        }

		public bool AddEncryptionMapping(EndPoint address, byte[] sendKey, byte[] receiveKey, double currentSeconds, double expiresAtSeconds, int timeoutAfterSeconds, uint clientID)
		{
			for (int i = 0; i < encyrptionMappings_totalCount; i++) // first try to find an expired or timed out, previously used slot for this address and re-use it
			{
                encryptionMapEntry encryptionMapping = encryptionMappings[i];
                if (!encryptionMapping.IsReset
                    && MiscUtils.AreEndPointsEqual(encryptionMapping.Address, address)
					&& (
                            (encryptionMapping.TimeoutAfterSeconds > 0 && (encryptionMapping.LastAccessedAtSeconds + encryptionMapping.TimeoutAfterSeconds) >= currentSeconds)
                            || (encryptionMapping.ExpiresAtSeconds > 0.0 && encryptionMapping.ExpiresAtSeconds < currentSeconds)
                       )
                    )
				{
                    encryptionMappings[i].ExpiresAtSeconds = expiresAtSeconds;
                    encryptionMappings[i].LastAccessedAtSeconds = currentSeconds;
                    encryptionMappings[i].TimeoutAfterSeconds = timeoutAfterSeconds;
                    encryptionMappings[i].ClientID = clientID;

                    Buffer.BlockCopy(sendKey, 0, encryptionMappings[i].SendKey, 0, 32);
                    Buffer.BlockCopy(receiveKey, 0, encryptionMappings[i].ReceiveKey, 0, 32);

                    // NOTE: encyrptionMappings_usedCount stays the same since we are reusing and expired one that was considered in use any way
                    return true;
				}
			}

			for (int i = 0; i < encyrptionMappings_totalCount; i++) // second, if an expired one could not be found/re-used for this address, just find a completely unused/reset slot and use it
			{
                if (encryptionMappings[i].IsReset)
				{
                    encryptionMappings[i].Address = address;
                    encryptionMappings[i].ExpiresAtSeconds = expiresAtSeconds;
                    encryptionMappings[i].LastAccessedAtSeconds = currentSeconds;
                    encryptionMappings[i].TimeoutAfterSeconds = timeoutAfterSeconds;
                    encryptionMappings[i].ClientID = clientID;

                    Buffer.BlockCopy(sendKey, 0, encryptionMappings[i].SendKey, 0, 32);
                    Buffer.BlockCopy(receiveKey, 0, encryptionMappings[i].ReceiveKey, 0, 32);

					++encyrptionMappings_usedCount;

                    return true;
				}
			}

            return false; // third, you are out of luck since we only allocate a specific number of slots...they are all in use and not expired...sorry (TODO: maybe add some new slots!)
		}

        /// <returns>The number of mappings removed (i.e., that match the <paramref name="address"/>).</returns>
		public int RemoveAllEncryptionMappings(EndPoint address)
        {
            int removedCount = 0;
            removedIndexes.Clear();

            for (int i = 0; i < encyrptionMappings_totalCount; i++)
            {
                if (!encryptionMappings[i].IsReset && MiscUtils.AreEndPointsEqual(encryptionMappings[i].Address, address))
                {
                    encryptionMappings[i].Reset();

                    removedIndexes.Add(i);
                    ++removedCount;
                }
            }

            //Move all the following encryption mappings one position to the left in order to fill empty gaps.
            foreach (int index in removedIndexes)
            {
                for (int i = index; i < encyrptionMappings_usedCount; ++i)
                {
                    //If it is the last one, reset it
                    if (i == encyrptionMappings_usedCount - 1)
                    {
                        encryptionMappings[i].Reset();
                    }
                    else
                    {
                        encryptionMappings[i].Address = encryptionMappings[i + 1].Address;
                        encryptionMappings[i].ExpiresAtSeconds = encryptionMappings[i + 1].ExpiresAtSeconds;
                        encryptionMappings[i].LastAccessedAtSeconds = encryptionMappings[i + 1].LastAccessedAtSeconds;
                        encryptionMappings[i].TimeoutAfterSeconds = encryptionMappings[i + 1].TimeoutAfterSeconds;
                        encryptionMappings[i].ClientID = encryptionMappings[i + 1].ClientID;

                        Buffer.BlockCopy(encryptionMappings[i + 1].SendKey, 0, encryptionMappings[i].SendKey, 0, 32);
                        Buffer.BlockCopy(encryptionMappings[i + 1].ReceiveKey, 0, encryptionMappings[i].ReceiveKey, 0, 32);
                    }
                }

                --encyrptionMappings_usedCount;
            }

            return removedCount;
        }

        public byte[] GetSendKey(int index)
		{
			if (index == -1 || index >= encyrptionMappings_totalCount) return null;
			return encryptionMappings[index].SendKey;
		}

		public byte[] GetReceiveKey(int index)
		{
			if (index == -1 || index >= encyrptionMappings_totalCount) return null;
			return encryptionMappings[index].ReceiveKey;
		}

		public int GetTimeoutSeconds(int index)
		{
			if (index == -1 || index >= encyrptionMappings_totalCount) return -1;
			return encryptionMappings[index].TimeoutAfterSeconds;
		}

		public uint GetClientID(int index)
		{
			if (index == -1 || index >= encyrptionMappings_totalCount) return 0;
			return encryptionMappings[index].ClientID;
		}

		public void SetClientID(int index, uint clientID)
		{
			if (index < 0 || index >= encyrptionMappings_usedCount)
				throw new IndexOutOfRangeException(nameof(index));

			encryptionMappings[index].ClientID = clientID;
		}

		public bool Touch(int index, EndPoint address, double currentSeconds)
		{
			if (index < 0 || index >= encyrptionMappings_usedCount)
				throw new IndexOutOfRangeException(nameof(index));

            if (!MiscUtils.AreEndPointsEqual(encryptionMappings[index].Address, address))
				return false;

            encryptionMappings[index].LastAccessedAtSeconds = currentSeconds;
            encryptionMappings[index].ExpiresAtSeconds = currentSeconds + Defines.NETCODE_TIMEOUT_SECONDS;
			return true;
		}

		public void SetExpiresAtSeconds(int index, double expiresAtSeconds)
		{
			if (index < 0 || index >= encyrptionMappings_usedCount)
				throw new IndexOutOfRangeException(nameof(index));

			encryptionMappings[index].ExpiresAtSeconds = expiresAtSeconds;
		}

		public int GetEncryptionMappingIndexForTime(EndPoint address, double currentSeconds)
		{
            for (int i = 0; i < encyrptionMappings_usedCount; ++i)
			{
                encryptionMapEntry encryptionMapping = encryptionMappings[i];
                if (!encryptionMapping.IsReset &&
                    MiscUtils.AreEndPointsEqual(encryptionMapping.Address, address) &&
					((encryptionMapping.LastAccessedAtSeconds + encryptionMapping.TimeoutAfterSeconds) >= currentSeconds || encryptionMapping.TimeoutAfterSeconds <= 0) &&
					(encryptionMapping.ExpiresAtSeconds <= 0.0 || encryptionMapping.ExpiresAtSeconds >= currentSeconds))
				{
                    encryptionMappings[i].LastAccessedAtSeconds = currentSeconds;
					return i;
				}
            }

            return -1;
		}

        public string GetAllEncryptionMappingAddresses()
        {
            StringBuilder stringBuilder = new StringBuilder();
            
            for (int i = 0; i < encyrptionMappings_usedCount; ++i)
            {
                encryptionMapEntry encryptionMapping = encryptionMappings[i];
                stringBuilder.Append('(')
                    .Append(encryptionMapping.Address).Append(',')
                    .Append(encryptionMapping.IsReset).Append(',')
                    .Append(encryptionMapping.ExpiresAtSeconds).Append(',')
                    .Append(encryptionMapping.LastAccessedAtSeconds).Append(',')
                    .Append(encryptionMapping.TimeoutAfterSeconds)
                    .Append(')');
            }

            return stringBuilder.ToString();
        }
    }
}
