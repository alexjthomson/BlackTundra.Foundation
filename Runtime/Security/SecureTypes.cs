#define SECURITY_USE_DECOY_MEMVALS

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace BlackTundra.Foundation.Security {

    #region sshort

    /// <summary>
    /// Secure <see cref="short"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sshort : IEquatable<sshort>, IEquatable<short>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private short decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private short protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private short protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sshort"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public short value {
#pragma warning restore IDE1006 // naming styles
            get => (short)(protectedValue ^ protectedOffset);
            set {
                byte[] newOffset = new byte[sizeof(short)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToInt16(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = (short)(value ^ protectedOffset);
            }
        }

        #endregion

        #region constructor

        public sshort(in short value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(short)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt16(newOffset, 0);
            protectedValue = (short)(value ^ protectedOffset);
        }

        public sshort(SerializationInfo info, StreamingContext context) {
            short value = info.GetInt16("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(short)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt16(newOffset, 0);
            protectedValue = (short)(value ^ protectedOffset);
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sshort i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(short i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator int(in sshort i) => i.value;
        public static implicit operator long(in sshort i) => i.value;
        public static implicit operator short(in sshort i) => i.value;
        public static implicit operator sshort(in short i) => new sshort(i);
        public static implicit operator sint(in sshort i) => new sint(i.value);
        public static implicit operator slong(in sshort i) => new slong(i.value);

        public static sshort operator +(sshort i1, sshort i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator -(sshort i1, sshort i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator *(sshort i1, sshort i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator /(sshort i1, sshort i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator ^(sshort i1, sshort i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset)));

        public static sshort operator +(sshort i1, short i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) + i2));
        public static sshort operator -(sshort i1, short i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) - i2));
        public static sshort operator *(sshort i1, short i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) * i2));
        public static sshort operator /(sshort i1, short i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) / i2));
        public static sshort operator ^(sshort i1, short i2) => new sshort((short)((i1.protectedValue ^ i1.protectedOffset) ^ i2));

        public static sshort operator +(short i1, sshort i2) => new sshort((short)(i1 + (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator -(short i1, sshort i2) => new sshort((short)(i1 - (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator *(short i1, sshort i2) => new sshort((short)(i1 * (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator /(short i1, sshort i2) => new sshort((short)(i1 / (i2.protectedValue ^ i2.protectedOffset)));
        public static sshort operator ^(short i1, sshort i2) => new sshort((short)(i1 ^ (i2.protectedValue ^ i2.protectedOffset)));

        public static sshort operator ++(sshort i) => new sshort((short)((i.protectedValue ^ i.protectedOffset) + 1));
        public static sshort operator --(sshort i) => new sshort((short)((i.protectedValue ^ i.protectedOffset) - 1));

        #endregion

    }

    #endregion

    #region sushort

    /// <summary>
    /// Secure <see cref="ushort"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sushort : IEquatable<sushort>, IEquatable<ushort>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private ushort decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private ushort protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private ushort protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sushort"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public ushort value {
#pragma warning restore IDE1006 // naming styles
            get => (ushort)(protectedValue ^ protectedOffset);
            set {
                byte[] newOffset = new byte[sizeof(ushort)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToUInt16(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = (ushort)(value ^ protectedOffset);
            }
        }

        #endregion

        #region constructor

        public sushort(in ushort value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(ushort)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt16(newOffset, 0);
            protectedValue = (ushort)(value ^ protectedOffset);
        }

        public sushort(SerializationInfo info, StreamingContext context) {
            ushort value = info.GetUInt16("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(ushort)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt16(newOffset, 0);
            protectedValue = (ushort)(value ^ protectedOffset);
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sushort i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(ushort i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator int(in sushort i) => i.value;
        public static implicit operator uint(in sushort i) => i.value;
        public static implicit operator long(in sushort i) => i.value;
        public static implicit operator ulong(in sushort i) => i.value;
        public static implicit operator ushort(in sushort i) => i.value;
        public static implicit operator sushort(in ushort i) => new sushort(i);
        public static implicit operator sint(in sushort i) => new sint(i.value);
        public static implicit operator slong(in sushort i) => new slong(i.value);
        public static implicit operator suint(in sushort i) => new suint(i.value);
        public static implicit operator sulong(in sushort i) => new sulong(i.value);

        public static sushort operator +(sushort i1, sushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator -(sushort i1, sushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator *(sushort i1, sushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator /(sushort i1, sushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator ^(sushort i1, sushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset)));

        public static sushort operator +(sushort i1, ushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) + i2));
        public static sushort operator -(sushort i1, ushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) - i2));
        public static sushort operator *(sushort i1, ushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) * i2));
        public static sushort operator /(sushort i1, ushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) / i2));
        public static sushort operator ^(sushort i1, ushort i2) => new sushort((ushort)((i1.protectedValue ^ i1.protectedOffset) ^ i2));

        public static sushort operator +(ushort i1, sushort i2) => new sushort((ushort)(i1 + (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator -(ushort i1, sushort i2) => new sushort((ushort)(i1 - (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator *(ushort i1, sushort i2) => new sushort((ushort)(i1 * (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator /(ushort i1, sushort i2) => new sushort((ushort)(i1 / (i2.protectedValue ^ i2.protectedOffset)));
        public static sushort operator ^(ushort i1, sushort i2) => new sushort((ushort)(i1 ^ (i2.protectedValue ^ i2.protectedOffset)));

        public static sushort operator ++(sushort i) => new sushort((ushort)((i.protectedValue ^ i.protectedOffset) + 1));
        public static sushort operator --(sushort i) => new sushort((ushort)((i.protectedValue ^ i.protectedOffset) - 1));

        #endregion

    }

    #endregion

    #region sint

    /// <summary>
    /// Secure <see cref="int"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sint : IEquatable<sint>, IEquatable<int>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private int decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private int protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private int protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sint"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public int value {
#pragma warning restore IDE1006 // naming styles
            get => protectedValue ^ protectedOffset;
            set {
                byte[] newOffset = new byte[sizeof(int)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToInt32(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = value ^ protectedOffset;
            }
        }

        #endregion

        #region constructor

        public sint(in int value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(int)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt32(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        public sint(SerializationInfo info, StreamingContext context) {
            int value = info.GetInt32("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(int)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt32(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sint i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(int i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator int(in sint i) => i.value;
        public static implicit operator long(in sint i) => i.value;
        public static implicit operator sint(in int i) => new sint(i);
        public static implicit operator slong(in sint i) => new slong(i.value);

        public static sint operator +(sint i1, sint i2) => new sint((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator -(sint i1, sint i2) => new sint((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator *(sint i1, sint i2) => new sint((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator /(sint i1, sint i2) => new sint((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator ^(sint i1, sint i2) => new sint((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset));

        public static sint operator +(sint i1, int i2) => new sint((i1.protectedValue ^ i1.protectedOffset) + i2);
        public static sint operator -(sint i1, int i2) => new sint((i1.protectedValue ^ i1.protectedOffset) - i2);
        public static sint operator *(sint i1, int i2) => new sint((i1.protectedValue ^ i1.protectedOffset) * i2);
        public static sint operator /(sint i1, int i2) => new sint((i1.protectedValue ^ i1.protectedOffset) / i2);
        public static sint operator ^(sint i1, int i2) => new sint((i1.protectedValue ^ i1.protectedOffset) ^ i2);

        public static sint operator +(int i1, sint i2) => new sint(i1 + (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator -(int i1, sint i2) => new sint(i1 - (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator *(int i1, sint i2) => new sint(i1 * (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator /(int i1, sint i2) => new sint(i1 / (i2.protectedValue ^ i2.protectedOffset));
        public static sint operator ^(int i1, sint i2) => new sint(i1 ^ (i2.protectedValue ^ i2.protectedOffset));

        public static sint operator ++(sint i) => new sint((i.protectedValue ^ i.protectedOffset) + 1);
        public static sint operator --(sint i) => new sint((i.protectedValue ^ i.protectedOffset) - 1);

        #endregion

    }

    #endregion

    #region suint

    /// <summary>
    /// Secure <see cref="uint"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct suint : IEquatable<suint>, IEquatable<uint>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private uint decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private uint protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private uint protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="suint"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public uint value {
#pragma warning restore IDE1006 // naming styles
            get => protectedValue ^ protectedOffset;
            set {
                byte[] newOffset = new byte[sizeof(uint)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToUInt32(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = value ^ protectedOffset;
            }
        }

        #endregion

        #region constructor

        public suint(in uint value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(uint)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt32(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        public suint(SerializationInfo info, StreamingContext context) {
            uint value = info.GetUInt32("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(uint)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt32(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(suint i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(uint i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator uint(in suint i) => i.value;
        public static implicit operator long(in suint i) => i.value;
        public static implicit operator ulong(in suint i) => i.value;
        public static implicit operator suint(in uint i) => new suint(i);
        public static implicit operator slong(in suint i) => new slong(i.value);
        public static implicit operator sulong(in suint i) => new sulong(i.value);

        public static suint operator +(suint i1, suint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator -(suint i1, suint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator *(suint i1, suint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator /(suint i1, suint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator ^(suint i1, suint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset));

        public static suint operator +(suint i1, uint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) + i2);
        public static suint operator -(suint i1, uint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) - i2);
        public static suint operator *(suint i1, uint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) * i2);
        public static suint operator /(suint i1, uint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) / i2);
        public static suint operator ^(suint i1, uint i2) => new suint((i1.protectedValue ^ i1.protectedOffset) ^ i2);

        public static suint operator +(uint i1, suint i2) => new suint(i1 + (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator -(uint i1, suint i2) => new suint(i1 - (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator *(uint i1, suint i2) => new suint(i1 * (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator /(uint i1, suint i2) => new suint(i1 / (i2.protectedValue ^ i2.protectedOffset));
        public static suint operator ^(uint i1, suint i2) => new suint(i1 ^ (i2.protectedValue ^ i2.protectedOffset));

        public static suint operator ++(suint i) => new suint((i.protectedValue ^ i.protectedOffset) + 1);
        public static suint operator --(suint i) => new suint((i.protectedValue ^ i.protectedOffset) - 1);

        #endregion

    }

    #endregion

    #region slong

    /// <summary>
    /// Secure <see cref="long"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct slong : IEquatable<slong>, IEquatable<long>, IEquatable<sint>, IEquatable<int>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private long decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private long protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private long protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="slong"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public long value {
#pragma warning restore IDE1006 // naming styles
            get => protectedValue ^ protectedOffset;
            set {
                byte[] newOffset = new byte[sizeof(long)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToInt64(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = value ^ protectedOffset;
            }
        }

        #endregion

        #region constructor

        public slong(in long value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(long)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt64(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        public slong(SerializationInfo info, StreamingContext context) {
            long value = info.GetInt64("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(long)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToInt32(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(slong i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(long i) => (protectedValue ^ protectedOffset) == i;
        public bool Equals(sint i) => (protectedValue ^ protectedOffset) == i.value;
        public bool Equals(int i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator long(in slong i) => i.value;
        public static implicit operator slong(in long i) => new slong(i);
        public static implicit operator slong(in int i) => new slong(i);

        public static slong operator +(slong i1, slong i2) => new slong((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator -(slong i1, slong i2) => new slong((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator *(slong i1, slong i2) => new slong((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator /(slong i1, slong i2) => new slong((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator ^(slong i1, slong i2) => new slong((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset));

        public static slong operator +(slong i1, long i2) => new slong((i1.protectedValue ^ i1.protectedOffset) + i2);
        public static slong operator -(slong i1, long i2) => new slong((i1.protectedValue ^ i1.protectedOffset) - i2);
        public static slong operator *(slong i1, long i2) => new slong((i1.protectedValue ^ i1.protectedOffset) * i2);
        public static slong operator /(slong i1, long i2) => new slong((i1.protectedValue ^ i1.protectedOffset) / i2);
        public static slong operator ^(slong i1, long i2) => new slong((i1.protectedValue ^ i1.protectedOffset) ^ i2);

        public static slong operator +(long i1, slong i2) => new slong(i1 + (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator -(long i1, slong i2) => new slong(i1 - (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator *(long i1, slong i2) => new slong(i1 * (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator /(long i1, slong i2) => new slong(i1 / (i2.protectedValue ^ i2.protectedOffset));
        public static slong operator ^(long i1, slong i2) => new slong(i1 ^ (i2.protectedValue ^ i2.protectedOffset));

        public static slong operator ++(slong i) => new slong((i.protectedValue ^ i.protectedOffset) + 1);
        public static slong operator --(slong i) => new slong((i.protectedValue ^ i.protectedOffset) - 1);

        #endregion

    }

    #endregion

    #region sulong

    /// <summary>
    /// Secure <see cref="ulong"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sulong : IEquatable<sulong>, IEquatable<ulong>, IEquatable<suint>, IEquatable<uint>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private ulong decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private ulong protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private ulong protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sulong"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public ulong value {
#pragma warning restore IDE1006 // naming styles
            get => protectedValue ^ protectedOffset;
            set {
                byte[] newOffset = new byte[sizeof(ulong)];
                CryptoUtility.RNG.GetBytes(newOffset);
                protectedOffset = BitConverter.ToUInt64(newOffset, 0);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                protectedValue = value ^ protectedOffset;
            }
        }

        #endregion

        #region constructor

        public sulong(in ulong value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(ulong)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt64(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        public sulong(SerializationInfo info, StreamingContext context) {
            ulong value = info.GetUInt64("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            byte[] newOffset = new byte[sizeof(ulong)];
            CryptoUtility.RNG.GetBytes(newOffset);
            protectedOffset = BitConverter.ToUInt64(newOffset, 0);
            protectedValue = value ^ protectedOffset;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sulong i) => (protectedValue ^ protectedOffset) == (i.protectedValue ^ i.protectedOffset);
        public bool Equals(ulong i) => (protectedValue ^ protectedOffset) == i;
        public bool Equals(suint i) => (protectedValue ^ protectedOffset) == i.value;
        public bool Equals(uint i) => (protectedValue ^ protectedOffset) == i;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", protectedValue ^ protectedOffset);

        #endregion

        #region ToString

        public override string ToString() => (protectedValue ^ protectedOffset).ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator ulong(in sulong i) => i.value;
        public static implicit operator sulong(in ulong i) => new sulong(i);
        public static implicit operator sulong(in uint i) => new sulong(i);

        public static sulong operator +(sulong i1, sulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) + (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator -(sulong i1, sulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) - (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator *(sulong i1, sulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) * (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator /(sulong i1, sulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) / (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator ^(sulong i1, sulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) ^ (i2.protectedValue ^ i2.protectedOffset));

        public static sulong operator +(sulong i1, ulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) + i2);
        public static sulong operator -(sulong i1, ulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) - i2);
        public static sulong operator *(sulong i1, ulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) * i2);
        public static sulong operator /(sulong i1, ulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) / i2);
        public static sulong operator ^(sulong i1, ulong i2) => new sulong((i1.protectedValue ^ i1.protectedOffset) ^ i2);

        public static sulong operator +(ulong i1, sulong i2) => new sulong(i1 + (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator -(ulong i1, sulong i2) => new sulong(i1 - (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator *(ulong i1, sulong i2) => new sulong(i1 * (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator /(ulong i1, sulong i2) => new sulong(i1 / (i2.protectedValue ^ i2.protectedOffset));
        public static sulong operator ^(ulong i1, sulong i2) => new sulong(i1 ^ (i2.protectedValue ^ i2.protectedOffset));

        public static sulong operator ++(sulong i) => new sulong((i.protectedValue ^ i.protectedOffset) + 1);
        public static sulong operator --(sulong i) => new sulong((i.protectedValue ^ i.protectedOffset) - 1);

        #endregion

    }

    #endregion

    #region sfloat

    /// <summary>
    /// Secure <see cref="float"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sfloat : IEquatable<sfloat>, IEquatable<float>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private float decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private byte[] protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private byte[] protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sfloat"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float value {
#pragma warning restore IDE1006 // naming styles
            get {
                byte[] outputBuffer = new byte[sizeof(float)];
                for (int i = sizeof(float) - 1; i > -1; i--) outputBuffer[i] = (byte)(protectedValue[i] ^ protectedOffset[i]);
                return BitConverter.ToSingle(outputBuffer, 0);
            }
            set {
                byte[] inputBuffer = BitConverter.GetBytes(value);
                CryptoUtility.RNG.GetBytes(protectedOffset);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                for (int i = sizeof(float) - 1; i > -1; i--) protectedValue[i] = (byte)(inputBuffer[i] ^ protectedOffset[i]);
            }
        }

        #endregion

        #region constructor

        public sfloat(in float value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            protectedOffset = new byte[sizeof(float)];
            protectedValue = new byte[sizeof(float)];
            this.value = value;
        }

        public sfloat(SerializationInfo info, StreamingContext context) {
            float value = info.GetSingle("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            protectedOffset = new byte[sizeof(float)];
            protectedValue = new byte[sizeof(float)];
            this.value = value;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sfloat f) {
            for (int i = sizeof(float) - 1; i > -1; i--) {
                if ((protectedValue[i] ^ protectedOffset[i]) != (f.protectedValue[i] ^ f.protectedOffset[i])) return false;
            }
            return true;
        }
        public bool Equals(float f) => value == f;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", value);

        #endregion

        #region ToString

        public override string ToString() => value.ToString();

        #endregion

        #endregion

        #region operators

        public static implicit operator float(in sfloat f) => f.value;
        public static implicit operator double(in sfloat f) => f.value;
        public static implicit operator sfloat(in float f) => new sfloat(f);
        public static implicit operator sdouble(in sfloat f) => new sdouble(f);

        public static sfloat operator +(sfloat f1, sfloat f2) => new sfloat(f1.value + f2.value);
        public static sfloat operator -(sfloat f1, sfloat f2) => new sfloat(f1.value - f2.value);
        public static sfloat operator *(sfloat f1, sfloat f2) => new sfloat(f1.value * f2.value);
        public static sfloat operator /(sfloat f1, sfloat f2) => new sfloat(f1.value / f2.value);

        public static sfloat operator +(sfloat f1, float f2) => new sfloat(f1.value + f2);
        public static sfloat operator -(sfloat f1, float f2) => new sfloat(f1.value - f2);
        public static sfloat operator *(sfloat f1, float f2) => new sfloat(f1.value * f2);
        public static sfloat operator /(sfloat f1, float f2) => new sfloat(f1.value / f2);

        public static sfloat operator +(float f1, sfloat f2) => new sfloat(f1 + f2.value);
        public static sfloat operator -(float f1, sfloat f2) => new sfloat(f1 - f2.value);
        public static sfloat operator *(float f1, sfloat f2) => new sfloat(f1 * f2.value);
        public static sfloat operator /(float f1, sfloat f2) => new sfloat(f1 / f2.value);

        #endregion

    }

    #endregion

    #region sdouble

    /// <summary>
    /// Secure <see cref="double"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sdouble : IEquatable<sdouble>, IEquatable<double>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private double decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Protected value, this will contain an obfuscated version of the real value.
        /// </summary>
        private byte[] protectedValue;

        /// <summary>
        /// Random number XORed against the <see cref="protectedValue"/> to get the actual value.
        /// </summary>
        private byte[] protectedOffset;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sdouble"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public double value {
#pragma warning restore IDE1006 // naming styles
            get {
                byte[] outputBuffer = new byte[sizeof(double)];
                for (int i = sizeof(double) - 1; i > -1; i--) outputBuffer[i] = (byte)(protectedValue[i] ^ protectedOffset[i]);
                return BitConverter.ToDouble(outputBuffer, 0);
            }
            set {
                byte[] inputBuffer = BitConverter.GetBytes(value);
                CryptoUtility.RNG.GetBytes(protectedOffset);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                for (int i = sizeof(double) - 1; i > -1; i--) protectedValue[i] = (byte)(inputBuffer[i] ^ protectedOffset[i]);
            }
        }

        #endregion

        #region constructor

        public sdouble(in double value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            protectedOffset = new byte[sizeof(double)];
            protectedValue = new byte[sizeof(double)];
            this.value = value;
        }

        public sdouble(SerializationInfo info, StreamingContext context) {
            double value = info.GetDouble("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            protectedOffset = new byte[sizeof(double)];
            protectedValue = new byte[sizeof(double)];
            this.value = value;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sdouble f) {
            for (int i = sizeof(double) - 1; i > -1; i--) {
                if ((protectedValue[i] ^ protectedOffset[i]) != (f.protectedValue[i] ^ f.protectedOffset[i])) return false;
            }
            return true;
        }
        public bool Equals(double f) => value == f;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", value);

        #endregion

        #region ToString

        public override string ToString() => value.ToString();

        #endregion

        #endregion

        #region operators

        public static explicit operator float(in sdouble f) => (float)f.value;
        public static implicit operator double(in sdouble f) => f.value;
        public static explicit operator sfloat(in sdouble f) => new sfloat((float)f.value);
        public static implicit operator sdouble(in double f) => new sdouble(f);

        public static sdouble operator +(sdouble f1, sdouble f2) => new sdouble(f1.value + f2.value);
        public static sdouble operator -(sdouble f1, sdouble f2) => new sdouble(f1.value - f2.value);
        public static sdouble operator *(sdouble f1, sdouble f2) => new sdouble(f1.value * f2.value);
        public static sdouble operator /(sdouble f1, sdouble f2) => new sdouble(f1.value / f2.value);

        public static sdouble operator +(sdouble f1, double f2) => new sdouble(f1.value + f2);
        public static sdouble operator -(sdouble f1, double f2) => new sdouble(f1.value - f2);
        public static sdouble operator *(sdouble f1, double f2) => new sdouble(f1.value * f2);
        public static sdouble operator /(sdouble f1, double f2) => new sdouble(f1.value / f2);

        public static sdouble operator +(double f1, sdouble f2) => new sdouble(f1 + f2.value);
        public static sdouble operator -(double f1, sdouble f2) => new sdouble(f1 - f2.value);
        public static sdouble operator *(double f1, sdouble f2) => new sdouble(f1 * f2.value);
        public static sdouble operator /(double f1, sdouble f2) => new sdouble(f1 / f2.value);

        #endregion

    }

    #endregion

    #region sbool

    /// <summary>
    /// Secure <see cref="bool"/> that's protected against memory modification software.
    /// </summary>
    [Serializable]
#pragma warning disable IDE1006 // naming styles
    public struct sbool : IEquatable<sbool>, IEquatable<bool>, ISerializable {
#pragma warning restore IDE1006 // naming styles

        #region constant

        /// <summary>
        /// Maximum index value.
        /// </summary>
        private const int MaxIndex = 12;

        #endregion

        #region variable

#if SECURITY_USE_DECOY_MEMVALS
        /// <summary>
        /// Decoy value used to fool memory modification software.
        /// </summary>
#pragma warning disable IDE0052 // remove unread private members
        private bool decoyValue;
#pragma warning restore IDE0052 // remove unread private members
#endif

        /// <summary>
        /// Encoded <see cref="bool"/> value.
        /// The last 4 bits describe the position in the encoded value that the boolean value is contained.
        /// For instance, if the value of the last 4 bits is 0001, if the 1st bit (index 0) is set, the value
        /// is <c>true</c>. If the last 4 bits reads 1001, then the 9th bit (index 0) is checked.
        /// </summary>
        public ushort encodedValue;

        #endregion

        #region property

        /// <summary>
        /// Used to modify the value of the <see cref="sbool"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public bool value {
#pragma warning restore IDE1006 // naming styles
            get {
                int index = (encodedValue & 0b11110000_00000000) >> 12;
                return (encodedValue & (1 << index)) != 0;
            }
            set {
                byte[] random = new byte[sizeof(ushort)];
                CryptoUtility.RNG.GetBytes(random);
#if SECURITY_USE_DECOY_MEMVALS
                decoyValue = value;
#endif
                encodedValue = BitConverter.ToUInt16(random, 0);
                int index = (encodedValue & 0b11110000_00000000) >> 12;
                if (index >= MaxIndex) index -= MaxIndex;
                encodedValue = (ushort)(
                    0b00001111_11111111 & (
                        value
                            ? encodedValue | (1 << index)
                            : encodedValue & ~(1 << index)
                    ) | (index << 12)
                );
            }
        }

        #endregion

        #region constructor

        public sbool(in bool value) {
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            encodedValue = 0;
            this.value = value;
        }

        public sbool(SerializationInfo info, StreamingContext context) {
            bool value = info.GetBoolean("v");
#if SECURITY_USE_DECOY_MEMVALS
            decoyValue = value;
#endif
            encodedValue = 0;
            this.value = value;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(sbool v) => this.value == v.value;
        public bool Equals(bool v) => value == v;

        #endregion

        #region GetObjectData

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context) => info.AddValue("v", value);

        #endregion

        #region ToString

        public override string ToString() => value ? "true" : "false";

        #endregion

        #endregion

        #region operators

        public static implicit operator bool(in sbool v) => v.value;
        public static implicit operator sbool(in bool v) => new sbool(v);

        #endregion

    }

    #endregion

}