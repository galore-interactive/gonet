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
using System.Security;
using System.Threading;

namespace GONet.Utils
{
    [Serializable]
    public sealed class GUID
    {
        #region private const fields
        internal const ulong LAYOUT_GONet_ECO_SYSTEM_ID_BIT = 1UL << 63;
        internal const int FOUR_TIMES_EIGHT = 4 * 8;
        internal const int FIVE_TIMES_EIGHT = 5 * 8;
        internal const int SEVEN_TIMES_EIGHT = 7 * 8;
        #endregion

        public const long UNSET_VALUE = default(long);

        private long _asInt64 = UNSET_VALUE;
        private int _asInt32 = default(int);

        /// <summary>
        /// Check if this instance has a uid set or if it is the default (not set) value.
        /// </summary>
        public bool IsSet => _asInt64 != UNSET_VALUE;

        static GUID()
        {
            asInt32s.Add(default(int)); // make sure 0 is not served up by AsInt32()
        }

        public GUID() { }

        public GUID(int gameServerId, byte sessionServerId, uint id)
        {
            _asInt64 = SessionIdComponentsAsInt64(gameServerId, sessionServerId, id);
        }

        // if the UID is already created then use it
        public GUID(long alreadySetID)
        {
            _asInt64 = alreadySetID;
        }

        public GUID(GUIDInternals internalUid)
        {
            _asInt64 = internalUid.AsInt64();
        }

        /// <summary>
        /// Any time you need an instance out of the blue that <see cref="IsSet"/> will return true, use this!
        /// </summary>
        /// <returns></returns>
        public static GUID Generate()
        {
            // let this implementation do the hard work of building the full id and we will break it down to 64 bits in the UID constructor
            GUIDInternals fullUIDInternals = GUIDInternals.GenerateNewId();
            return new GUID(fullUIDInternals);
        }

        public long AsInt64()
        {
            return _asInt64;
        }


        /// <summary>
        /// This is ALL values used in this process for <see cref="_asInt32"/>;
        /// </summary>
        private static readonly HashSet<int> asInt32s = new HashSet<int>();
        private static readonly Random asInt32s_random = new Random();

        /// <summary>
        /// IMPORTANT: This is NOT thread safe...only call this from one thread!
        /// Returns a unique int32 version for this instance....which will be the same every call to this method for this instance.
        /// Uniqueness is defined by only being unique in the context of this process and only what has been previously returned by this method (on any instance in memory).
        /// This is not guaranteed to return a globally unique int, but it will be process unique.
        /// </summary>
        public int AsInt32_ProcessUniqueOnly()
        {
            if (_asInt32 == default(int))
            {
                _asInt32 = (int)(_asInt64 >> 32); // NOTE: This alone is NOT guaranteed to be unique...which is why we loop below if not.
                while (asInt32s.Contains(_asInt32)) // TODO consider only attempting this some # of times to not allow infinite loop??  But we also need this to return unique!
                {
                    // Having to continue looping to avoid returning duplicate UID as Int32....we will return a unique value
                    int random = asInt32s_random.Next(1, 100);
                    _asInt32 += _asInt32 < 0 ? random : -random;
                }

                asInt32s.Add(_asInt32);
            }

            return _asInt32;
        }

        public static implicit operator long(GUID uid)
        {
            return uid != null ? uid.AsInt64() : UNSET_VALUE;
        }

        public override bool Equals(object obj)
        {
            GUID other = obj as GUID;
            if (other != null)
            {
                return other.AsInt64() == AsInt64();
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _asInt64.GetHashCode();
        }

        private static long SessionIdComponentsAsInt64(int gameServerId, byte sessionServerId, uint id)
        {
            ulong result = 0UL;

            // first bit stays 0, because only the eco system layout has it set (i.e. LAYOUT_ECO_SYSTEM_ID_BIT)
            result |= (ulong)(gameServerId & 0x00efffff) << FIVE_TIMES_EIGHT; // only use low order 3 bytes minus 1 bit for layout type
            result |= (ulong)sessionServerId << FOUR_TIMES_EIGHT;
            result |= id;

            return CRC32.UInt64ToInt64(result);
        }

        internal static GUID FromInt64(long int64)
        {
            // if the UID is already created then use it
            if (int64 > 0)
            {
                return new GUID(int64);
            }

            if (int64 == UNSET_VALUE)
            {
                // assume if all 0's, then this is an unset uid
                return new GUID(); // not set uid
            }
            else
            {
                ulong uint64 = CRC32.Int64ToUInt64(int64);

                bool isLayoutTypeEcoSystemId = (uint64 & LAYOUT_GONet_ECO_SYSTEM_ID_BIT) == LAYOUT_GONet_ECO_SYSTEM_ID_BIT;
                if (isLayoutTypeEcoSystemId)
                {
                    int sevenMostSignificantBitsOfMachine = (int)(uint64 >> SEVEN_TIMES_EIGHT);
                    sevenMostSignificantBitsOfMachine <<= 17;
                    sevenMostSignificantBitsOfMachine = sevenMostSignificantBitsOfMachine & 0x00ffffff;  // this lops off the sign bit (i.e., LAYOUT_ECO_SYSTEM_ID_BIT) or else the UIDInternals constructor below will fail seeing the value for machine being higher than expected/allowed ... and it would be because of that one bit, that we don't care about anyway for this purpose)

                    int increment = (int)((uint64 >> FOUR_TIMES_EIGHT) & 0x00ffffff); // the increment only uses low order 3 bytes
                    int timestamp = (int)uint64;

                    var internalUid = new GUIDInternals(timestamp, sevenMostSignificantBitsOfMachine, 0, increment);
                    return new GUID(internalUid);
                }
                else
                {
                    int gameServerId = (int)((uint64 >> FIVE_TIMES_EIGHT) & 0x00efffff); // only use low order 3 bytes minus 1 bit for layout type
                    byte sessionServerId = (byte)(uint64 >> FOUR_TIMES_EIGHT);
                    uint id = (uint)uint64;

                    return new GUID(gameServerId, sessionServerId, id);
                }
            }
        }
    }

    [Serializable]
    public struct GUIDInternals : IComparable<GUIDInternals>, IEquatable<GUIDInternals>, IConvertible
    {
        #region private static fields
        private static GUIDInternals __emptyInstance = default(GUIDInternals);
        private static int __staticMachine;
        private static short __staticPid;
        private static int __staticIncrement; // high byte will be masked out when generating new UIDInternals
        #endregion

        #region private fields
        // we're using 14 bytes instead of 12 to hold the UIDInternals in memory but unlike a byte[] there is no additional object on the heap
        // the extra two bytes are not visible to anyone outside of this class and they buy us considerable simplification
        // an additional advantage of this representation is that it will serialize to JSON without any 64 bit overflow problems
        private int _timestamp;
        private int _machine;
        private short _pid;
        private int _increment;
        #endregion

        // static constructor
        static GUIDInternals()
        {
            __staticMachine = GetMachineHash();
            __staticIncrement = (new Random()).Next();

            try
            {
                __staticPid = (short)GetCurrentProcessId(); // use low order two bytes only
            }
            catch (SecurityException)
            {
                __staticPid = 0;
            }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the UIDInternals class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public GUIDInternals(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            Unpack(bytes, out _timestamp, out _machine, out _pid, out _increment);
        }

        /// <summary>
        /// Initializes a new instance of the UIDInternals class.
        /// </summary>
        /// <param name="timestamp">The timestamp (expressed as a DateTime).</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public GUIDInternals(DateTime timestamp, int machine, short pid, int increment)
            : this(GetTimestampFromDateTime(timestamp), machine, pid, increment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the UIDInternals class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public GUIDInternals(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            _timestamp = timestamp;
            _machine = machine;
            _pid = pid;
            _increment = increment;
        }

        /// <summary>
        /// Initializes a new instance of the UIDInternals class.
        /// </summary>
        /// <param name="value">The value.</param>
        public GUIDInternals(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            byte[] bytes = StringUtils.ParseHexString(value);
            Unpack(bytes, out _timestamp, out _machine, out _pid, out _increment);
        }

        // public static properties
        /// <summary>
        /// Gets an instance of UIDInternals where the value is empty.
        /// </summary>
        public static GUIDInternals Empty
        {
            get { return __emptyInstance; }
        }

        // public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp
        {
            get { return _timestamp; }
        }

        /// <summary>
        /// Gets the machine.
        /// </summary>
        public int Machine
        {
            get { return _machine; }
        }

        /// <summary>
        /// Gets the PID.
        /// </summary>
        public short Pid
        {
            get { return _pid; }
        }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment
        {
            get { return _increment; }
        }

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime
        {
            get { return DateTimeUtils.UnixEpoch.AddSeconds(_timestamp); }
        }

        public long AsInt64()
        {
            ulong result = 0UL;

            result |= GUID.LAYOUT_GONet_ECO_SYSTEM_ID_BIT;
            int sevenMostSignificantBitsOfMachine = (_machine & 0x00ffffff) >> 17;// use 7 most significant bits from the 3 bytes (i.e 24 bits - 17 == 7 most significant) that make machine
            result |= (ulong)sevenMostSignificantBitsOfMachine << GUID.SEVEN_TIMES_EIGHT;
            result |= (ulong)(_increment & 0x00ffffff) << GUID.FOUR_TIMES_EIGHT; // the increment only uses low order 3 bytes
            result |= (uint)_timestamp;

            return CRC32.UInt64ToInt64(result);
        }

        // public operators
        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals</param>
        /// <returns>True if the first UIDInternals is less than the second UIDInternals.</returns>
        public static bool operator <(GUIDInternals lhs, GUIDInternals rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals</param>
        /// <returns>True if the first UIDInternals is less than or equal to the second UIDInternals.</returns>
        public static bool operator <=(GUIDInternals lhs, GUIDInternals rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals.</param>
        /// <returns>True if the two UIDInternalss are equal.</returns>
        public static bool operator ==(GUIDInternals lhs, GUIDInternals rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals.</param>
        /// <returns>True if the two UIDInternalss are not equal.</returns>
        public static bool operator !=(GUIDInternals lhs, GUIDInternals rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals</param>
        /// <returns>True if the first UIDInternals is greather than or equal to the second UIDInternals.</returns>
        public static bool operator >=(GUIDInternals lhs, GUIDInternals rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two UIDInternalss.
        /// </summary>
        /// <param name="lhs">The first UIDInternals.</param>
        /// <param name="rhs">The other UIDInternals</param>
        /// <returns>True if the first UIDInternals is greather than the second UIDInternals.</returns>
        public static bool operator >(GUIDInternals lhs, GUIDInternals rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        // public static methods
        /// <summary>
        /// Generates a new UIDInternals with a unique value.
        /// </summary>
        /// <returns>An UIDInternals.</returns>
        public static GUIDInternals GenerateNewId()
        {
            return GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));
        }

        /// <summary>
        /// Generates a new UIDInternals with a unique value (with the timestamp component based on a given DateTime).
        /// </summary>
        /// <param name="timestamp">The timestamp component (expressed as a DateTime).</param>
        /// <returns>An UIDInternals.</returns>
        public static GUIDInternals GenerateNewId(DateTime timestamp)
        {
            return GenerateNewId(GetTimestampFromDateTime(timestamp));
        }

        /// <summary>
        /// Generates a new UIDInternals with a unique value (with the given timestamp).
        /// </summary>
        /// <param name="timestamp">The timestamp component.</param>
        /// <returns>An UIDInternals.</returns>
        public static GUIDInternals GenerateNewId(int timestamp)
        {
            int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return new GUIDInternals(timestamp, __staticMachine, __staticPid, increment);
        }

        /// <summary>
        /// Packs the components of an UIDInternals into a byte array.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A byte array.</returns>
        public static byte[] Pack(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            byte[] bytes = new byte[12];
            bytes[0] = (byte)(timestamp >> 24);
            bytes[1] = (byte)(timestamp >> 16);
            bytes[2] = (byte)(timestamp >> 8);
            bytes[3] = (byte)(timestamp);
            bytes[4] = (byte)(machine >> 16);
            bytes[5] = (byte)(machine >> 8);
            bytes[6] = (byte)(machine);
            bytes[7] = (byte)(pid >> 8);
            bytes[8] = (byte)(pid);
            bytes[9] = (byte)(increment >> 16);
            bytes[10] = (byte)(increment >> 8);
            bytes[11] = (byte)(increment);
            return bytes;
        }

        /// <summary>
        /// Parses a string and creates a new UIDInternals.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A UIDInternals.</returns>
        public static GUIDInternals Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            GUIDInternals internalUid;
            if (TryParse(s, out internalUid))
            {
                return internalUid;
            }
            else
            {
                var message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new UIDInternals.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="internalUid">The new UIDInternals.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(string s, out GUIDInternals internalUid)
        {
            // don't throw ArgumentNullException if s is null
            if (s != null && s.Length == 24)
            {
                byte[] bytes;
                if (StringUtils.TryParseHexString(s, out bytes))
                {
                    internalUid = new GUIDInternals(bytes);
                    return true;
                }
            }

            internalUid = default(GUIDInternals);
            return false;
        }

        /// <summary>
        /// Unpacks a byte array into the components of an UIDInternals.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public static void Unpack(byte[] bytes, out int timestamp, out int machine, out short pid, out int increment)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Byte array must be 12 bytes long.");
            }
            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            pid = (short)((bytes[7] << 8) + bytes[8]);
            increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
        }

        // private static methods
        /// <summary>
        /// Gets the current process id.  This method exists because of how CAS operates on the call stack, checking
        /// for permissions before executing the method.  Hence, if we inlined this call, the calling method would not execute
        /// before throwing an exception requiring the try/catch at an even higher level that we don't necessarily control.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        private static int GetMachineHash()
        {
            var hostName = Environment.MachineName; // use instead of Dns.HostName so it will work offline
            return 0x00ffffff & hostName.GetHashCode(); // use first 3 bytes of hash
        }

        private static int GetTimestampFromDateTime(DateTime timestamp)
        {
            return (int)Math.Floor((DateTimeUtils.ToUniversalTime(timestamp) - DateTimeUtils.UnixEpoch).TotalSeconds);
        }

        // public methods
        /// <summary>
        /// Compares this UIDInternals to another UIDInternals.
        /// </summary>
        /// <param name="other">The other UIDInternals.</param>
        /// <returns>A 32-bit signed integer that indicates whether this UIDInternals is less than, equal to, or greather than the other.</returns>
        public int CompareTo(GUIDInternals other)
        {
            int r = _timestamp.CompareTo(other._timestamp);
            if (r != 0) { return r; }
            r = _machine.CompareTo(other._machine);
            if (r != 0) { return r; }
            r = _pid.CompareTo(other._pid);
            if (r != 0) { return r; }
            return _increment.CompareTo(other._increment);
        }

        /// <summary>
        /// Compares this UIDInternals to another UIDInternals.
        /// </summary>
        /// <param name="rhs">The other UIDInternals.</param>
        /// <returns>True if the two UIDInternalss are equal.</returns>
        public bool Equals(GUIDInternals rhs)
        {
            return
                _timestamp == rhs._timestamp &&
                _machine == rhs._machine &&
                _pid == rhs._pid &&
                _increment == rhs._increment;
        }

        /// <summary>
        /// Compares this UIDInternals to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is an UIDInternals and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            if (obj is GUIDInternals)
            {
                return Equals((GUIDInternals)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + _timestamp.GetHashCode();
            hash = 37 * hash + _machine.GetHashCode();
            hash = 37 * hash + _pid.GetHashCode();
            hash = 37 * hash + _increment.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the UIDInternals to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            return Pack(_timestamp, _machine, _pid, _increment);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            byte[] bytes = Pack(_timestamp, _machine, _pid, _increment);
            return StringUtils.ToHexString(bytes);
        }

        // explicit IConvertible implementation
        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.String:
                    return ((IConvertible)this).ToString(provider);
                case TypeCode.Object:
                    if (conversionType == typeof(object) || conversionType == typeof(GUIDInternals))
                    {
                        return this;
                    }
                    break;
            }

            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }
    }
}
