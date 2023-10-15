using System;

using NetcodeIO.NET.Utils;
using NetcodeIO.NET.Utils.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace NetcodeIO.NET.Internal
{
	/// <summary>
	/// Helper class for reading/writing packets
	/// </summary>
	internal static class PacketIO
	{
        private static readonly byte[] Zeroes = new byte[15];
        private static IMac _mac = new Poly1305();

        private static ChaCha7539Engine cipherEngine;
        private static ParametersWithIV _temp_Params;
        private static KeyParameter _encryptKey;
        private static KeyParameter _decryptKey;
        private static KeyParameter _macKey;

        private static readonly object encryptionMutex = new object();

        /// <summary>
        /// Read and decrypt packet data into an output buffer
        /// </summary>
        public static int ReadPacketData(NetcodePacketHeader header, ByteArrayReaderWriter stream, int length, ulong protocolID, byte[] key, byte[] outputBuffer)
		{
			byte[] encryptedBuffer = BufferPool.GetBuffer(2048);
			stream.ReadBytesIntoBuffer(encryptedBuffer, length);
			
			try
			{
				return DecryptPacketData(header, protocolID, encryptedBuffer, length, key, outputBuffer);
            }
            catch (Exception e)
			{
				throw e;
			}
            finally
            {
                BufferPool.ReturnBuffer(encryptedBuffer);
            }
		}

		/// <summary>
		/// Encrypt a packet's data
		/// </summary>
		public static int EncryptPacketData(NetcodePacketHeader header, ulong protocolID, byte[] packetData, int packetDataLen, byte[] key, byte[] outBuffer)
        {
            return ProcessEncryptionForPacketData(true, header, protocolID, packetData, packetDataLen, key, outBuffer);
        }

        private static KeyParameter GenerateRecordMacKey(IStreamCipher cipher, byte[] firstBlock)
        {
            cipher.ProcessBytes(firstBlock, 0, firstBlock.Length, firstBlock, 0);

            if (_macKey == null)
                _macKey = new KeyParameter(firstBlock, 0, 32);
            else
            {
                _macKey.Reset();
                _macKey.SetKey(firstBlock, 0, 32);
            }

            Arrays.Fill(firstBlock, (byte)0);
            return _macKey;
        }

        private static int CalculateRecordMac(KeyParameter macKey, byte[] additionalData, byte[] buf, int off, int len, byte[] outMac)
        {
            _mac.Reset();
            _mac.Init(macKey);

            UpdateRecordMacText(_mac, additionalData, 0, additionalData.Length);
            UpdateRecordMacText(_mac, buf, off, len);
            UpdateRecordMacLength(_mac, additionalData.Length);
            UpdateRecordMacLength(_mac, len);

            int macSize = _mac.GetMacSize();
            byte[] finalMac = BufferPool.GetBuffer(macSize);
            MacUtilities.DoFinal(_mac, finalMac, macSize);
            int finalMacLength = finalMac.Length;
            Buffer.BlockCopy(finalMac, 0, outMac, 0, finalMacLength);
            BufferPool.ReturnBuffer(finalMac);
            return finalMacLength;
        }

        private static void UpdateRecordMacLength(IMac mac, int len)
        {
            byte[] longLen = BufferPool.GetBuffer(8);
            Pack.UInt64_To_LE((ulong)len, longLen);
            mac.BlockUpdate(longLen, 0, longLen.Length);
            BufferPool.ReturnBuffer(longLen);
        }

        private static void UpdateRecordMacText(IMac mac, byte[] buf, int off, int len)
        {
            mac.BlockUpdate(buf, off, len);

            int partial = len % 16;
            if (partial != 0)
            {
                mac.BlockUpdate(Zeroes, 0, 16 - partial);
            }
        }

        /// <summary>
        /// Decrypt a packet's data
        /// </summary>
        public static int DecryptPacketData(NetcodePacketHeader header, ulong protocolID, byte[] packetData, int packetDataLen, byte[] key, byte[] outBuffer)
		{
            return ProcessEncryptionForPacketData(false, header, protocolID, packetData, packetDataLen, key, outBuffer);
        }

        private static int ProcessEncryptionForPacketData(bool isEncrypting, NetcodePacketHeader header, ulong protocolID, byte[] packetData, int packetDataLen, byte[] key, byte[] outBuffer)
        {
            byte[] additionalData = BufferPool.GetBuffer(Defines.NETCODE_VERSION_INFO_BYTES + 8 + 1);
            using (var writer = ByteArrayReaderWriter.Get(additionalData))
            {
                writer.WriteASCII(Defines.NETCODE_VERSION_INFO_STR);
                writer.Write(protocolID);
                writer.Write(header.GetPrefixByte());
            }

            return ProcessEncryptionForPacketData(isEncrypting, header.SequenceNumber, additionalData, packetData, packetDataLen, key, outBuffer);
        }

        private static int ProcessEncryptionForPacketData(bool isEncrypting, ulong sequenceNumber, byte[] additionalData, byte[] packetData, int packetDataLen, byte[] key, byte[] outBuffer)
        {
            lock (encryptionMutex)
            {
                byte[] nonce = BufferPool.GetBuffer(12);
                using (var writer = ByteArrayReaderWriter.Get(nonce))
                {
                    writer.Write((UInt32)0);
                    writer.Write(sequenceNumber);
                }

                if (cipherEngine == null)
                {
                    cipherEngine = new ChaCha7539Engine();
                }
                else
                {
                    cipherEngine.Reset();
                }

                KeyParameter keyParameter;
                if (isEncrypting)
                {
                    if (_encryptKey == null)
                        _encryptKey = new KeyParameter(key);
                    else
                    {
                        _encryptKey.Reset();
                        _encryptKey.SetKey(key);
                    }

                    keyParameter = _encryptKey;
                }
                else
                {
                    if (_decryptKey == null)
                        _decryptKey = new KeyParameter(key);
                    else
                    {
                        _decryptKey.Reset();
                        _decryptKey.SetKey(key);
                    }

                    keyParameter = _decryptKey;
                }

                if (_temp_Params == null)
                {
                    _temp_Params = new ParametersWithIV(keyParameter, nonce);
                }
                else
                {
                    _temp_Params.Reset();
                    _temp_Params.Set(keyParameter, nonce);
                }

                cipherEngine.Init(isEncrypting, _temp_Params);

                byte[] firstBlock = BufferPool.GetBuffer(64);
                KeyParameter macKey = GenerateRecordMacKey(cipherEngine, firstBlock);

                byte[] mac = BufferPool.GetBuffer(16);
                byte[] receivedMac = null;

                int lengthilSoup = packetDataLen;

                try
                {
                    if (isEncrypting)
                    {
                        lengthilSoup += 16;

                        cipherEngine.ProcessBytes(packetData, 0, packetDataLen, outBuffer, 0);

                        int macSize = CalculateRecordMac(macKey, additionalData, outBuffer, 0, packetDataLen, mac);
                        Array.Copy(mac, 0, outBuffer, packetDataLen, macSize);
                    }
                    else
                    {
                        lengthilSoup -= 16;

                        int macSize = CalculateRecordMac(macKey, additionalData, packetData, 0, lengthilSoup, mac);

                        receivedMac = BufferPool.GetBuffer(16);
                        Array.Copy(packetData, lengthilSoup, receivedMac, 0, receivedMac.Length);

                        if (!Arrays.ConstantTimeAreEqual(mac, receivedMac))
                        {
                            throw new TlsFatalAlert(AlertDescription.bad_record_mac);
                        }

                        cipherEngine.ProcessBytes(packetData, 0, packetDataLen, outBuffer, 0);
                    }

                    return lengthilSoup;
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    BufferPool.ReturnBuffer(additionalData);
                    BufferPool.ReturnBuffer(nonce);
                    BufferPool.ReturnBuffer(mac);
                    BufferPool.ReturnBuffer(firstBlock);
                    if (!isEncrypting)
                    {
                        BufferPool.ReturnBuffer(receivedMac);
                    }
                }
            }
        }

        /// <summary>
        /// Encrypt a challenge token
        /// </summary>
        public static int EncryptChallengeToken(ulong sequenceNum, byte[] packetData, byte[] key, byte[] outBuffer)
		{
			byte[] additionalData = BufferPool.GetBuffer(0);
            return ProcessEncryptionForPacketData(true, sequenceNum, additionalData, packetData, 300 - Defines.MAC_SIZE, key, outBuffer);
		}

		/// <summary>
		/// Decrypt a challenge token
		/// </summary>
		public static int DecryptChallengeToken(ulong sequenceNum, byte[] packetData, byte[] key, byte[] outBuffer)
		{
			byte[] additionalData = BufferPool.GetBuffer(0);
            return ProcessEncryptionForPacketData(false, sequenceNum, additionalData, packetData, 300, key, outBuffer);
		}

		// Encrypt a private connect token
		public static int EncryptPrivateConnectToken(byte[] privateConnectToken, ulong protocolID, ulong expireTimestamp, ulong sequence, byte[] key, byte[] outBuffer)
		{
			int len = privateConnectToken.Length;

			byte[] additionalData = BufferPool.GetBuffer(Defines.NETCODE_VERSION_INFO_BYTES + 8 + 8);
			using (var writer = ByteArrayReaderWriter.Get(additionalData))
			{
				writer.WriteASCII(Defines.NETCODE_VERSION_INFO_STR);
				writer.Write(protocolID);
				writer.Write(expireTimestamp);
			}

            return ProcessEncryptionForPacketData(true, sequence, additionalData, privateConnectToken, len - Defines.MAC_SIZE, key, outBuffer);
		}

		// Decrypt a private connect token
		public static int DecryptPrivateConnectToken(byte[] encryptedConnectToken, ulong protocolID, ulong expireTimestamp, ulong sequence, byte[] key, byte[] outBuffer)
		{
            int len = encryptedConnectToken.Length;

            byte[] additionalData = BufferPool.GetBuffer(Defines.NETCODE_VERSION_INFO_BYTES + 8 + 8);
            using (var writer = ByteArrayReaderWriter.Get(additionalData))
            {
                writer.WriteASCII(Defines.NETCODE_VERSION_INFO_STR);
                writer.Write(protocolID);
                writer.Write(expireTimestamp);
            }

            return ProcessEncryptionForPacketData(false, sequence, additionalData, encryptedConnectToken, len, key, outBuffer);
        }
    }
}
