using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace RediSessionLibrary
{
    /// <summary>
    /// The Raw Serializer for serialzing data in a Redis collection
    /// </summary>
    public sealed class RedisSessionSerializer
    {
        #region Private properties
        private Dictionary<TypeCode, Func<object, byte[]>> serialize_funcs;
        private Dictionary<TypeCode, Func<byte[], object>> deserialize_funcs;
        private readonly byte[] empty;
        #endregion

        /// <summary>
        /// Constructs a Redis Serializer object
        /// </summary>
        public RedisSessionSerializer()
            : base()
        {
            // init a r/o empty array
            empty = new byte[1];
            empty[0] = (byte)TypeCode.Empty;

            // create our lookup table based on types
            // for serialization
            serialize_funcs = new Dictionary<TypeCode, Func<object, byte[]>>();

            serialize_funcs.Add(TypeCode.DBNull, this.SerializeEmpty);
            serialize_funcs.Add(TypeCode.String, this.SerializeString);
            serialize_funcs.Add(TypeCode.Boolean, this.SerializeBoolean);
            serialize_funcs.Add(TypeCode.Int16, this.SerializeInt16);
            serialize_funcs.Add(TypeCode.Int32, this.SerializeInt32);
            serialize_funcs.Add(TypeCode.Int64, this.SerializeInt64);
            serialize_funcs.Add(TypeCode.UInt16, this.SerializeUInt16);
            serialize_funcs.Add(TypeCode.UInt32, this.SerializeUInt32);
            serialize_funcs.Add(TypeCode.UInt64, this.SerializeUInt64);
            serialize_funcs.Add(TypeCode.Char, this.SerializeChar);
            serialize_funcs.Add(TypeCode.DateTime, this.SerializeDateTime);
            serialize_funcs.Add(TypeCode.Double, this.SerializeDouble);
            serialize_funcs.Add(TypeCode.Single, this.SerializeSingle);
            serialize_funcs.Add(TypeCode.Decimal, this.SerializeDecimal);
            serialize_funcs.Add(TypeCode.Empty, this.SerializeEmpty);
            serialize_funcs.Add(TypeCode.Byte, this.SerializeByte);
            serialize_funcs.Add(TypeCode.SByte, this.SerializeSByte);
            serialize_funcs.Add(TypeCode.Object, this.SerializeObject);

            // create our lookup table based on types
            // for de-serialization
            deserialize_funcs = new Dictionary<TypeCode, Func<byte[], object>>();

            deserialize_funcs.Add(TypeCode.DBNull, this.DeSerializeEmpty);
            deserialize_funcs.Add(TypeCode.String, this.DeSerializeString);
            deserialize_funcs.Add(TypeCode.Boolean, this.DeSerializeBoolean);
            deserialize_funcs.Add(TypeCode.Int16, this.DeSerializeInt16);
            deserialize_funcs.Add(TypeCode.Int32, this.DeSerializeInt32);
            deserialize_funcs.Add(TypeCode.Int64, this.DeSerializeInt64);
            deserialize_funcs.Add(TypeCode.UInt16, this.DeSerializeUInt16);
            deserialize_funcs.Add(TypeCode.UInt32, this.DeSerializeUInt32);
            deserialize_funcs.Add(TypeCode.UInt64, this.DeSerializeUInt64);
            deserialize_funcs.Add(TypeCode.Char, this.DeSerializeChar);
            deserialize_funcs.Add(TypeCode.DateTime, this.DeSerializeDateTime);
            deserialize_funcs.Add(TypeCode.Double, this.DeSerializeDouble);
            deserialize_funcs.Add(TypeCode.Single, this.DeSerializeSingle);
            deserialize_funcs.Add(TypeCode.Decimal, this.DeSerializeDecimal);
            deserialize_funcs.Add(TypeCode.Empty, this.DeSerializeEmpty);
            deserialize_funcs.Add(TypeCode.Byte, this.DeSerializeByte);
            deserialize_funcs.Add(TypeCode.SByte, this.DeSerializeSByte);
            deserialize_funcs.Add(TypeCode.Object, this.DeSerializeObject);
        }

        #region helper functions
        /// <summary>
        /// Returns a type encoded byte array
        /// </summary>
        /// <param name="btype">the type</param>
        /// <param name="bufVal">and value</param>
        /// <returns></returns>
        private byte[] TypeEncodeBuffer(byte btype, byte[] bufVal)
        {
            byte[] buf = new byte[bufVal.Length + 1];
            buf[0] = btype;
            int i = 1;
            while (i < buf.Length)
            {
                buf[i] = bufVal[i - 1];
                i++;
            }
            return buf;
        }
        #endregion

        /// <summary>
        /// Called to translate an object to a binary representation
        /// </summary>
        /// <param name="value">the object</param>
        /// <returns>the bytes of the encoded value</returns>
        public byte[] Serialize(object value)
        {
            TypeCode tcode = (value == null ? TypeCode.Empty : Type.GetTypeCode(value.GetType()));
            Func<object, byte[]> serializeFunc = serialize_funcs[tcode];
            return serializeFunc(value);
        }

        /// <summary>
        /// Deserializes an object 
        /// </summary>
        /// <param name="bytes">the type encoded bytes</param>
        /// <returns>the deserialized object</returns>
        public object Deserialize(byte[] bytes)
        {
            TypeCode tcode = (bytes == null || bytes.Length < 1 ? TypeCode.Empty : (TypeCode)bytes[0]);
            Func<byte[], object> deSerializeFunc = deserialize_funcs[tcode];
            return deSerializeFunc(bytes);
        }

        #region Serialization Functions
        #region Objects
        private byte[] SerializeObject(object value)
        {
            byte[] buf = null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)TypeCode.Object);
                bf.Serialize(ms, value);
                buf = ms.GetBuffer();
            }
            return buf;
        }

        private object DeSerializeObject(byte[] arg)
        {
            object retObj = null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(arg))
            {
                if (ms.ReadByte() == (byte)TypeCode.Object)
                    retObj = bf.Deserialize(ms);
            }
            return retObj;
        }
        #endregion

        #region Double Single Decimal
        private byte[] SerializeDecimal(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.Decimal, BitConverter.GetBytes((Double)((Decimal)arg)));
        }
        private object DeSerializeDecimal(byte[] arg)
        {
            return (Decimal)BitConverter.ToDouble(arg, 1);
        }

        private byte[] SerializeSingle(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.Single, BitConverter.GetBytes(((Single)arg)));
        }

        private object DeSerializeSingle(byte[] arg)
        {
            return BitConverter.ToSingle(arg, 1);
        }

        private byte[] SerializeDouble(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.Double, BitConverter.GetBytes(((Double)arg)));
        }

        private object DeSerializeDouble(byte[] arg)
        {
            return BitConverter.ToDouble(arg, 1);
        }
        #endregion

        #region Date Time
        private byte[] SerializeDateTime(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.DateTime, BitConverter.GetBytes(((DateTime)arg).Ticks));
        }

        private object DeSerializeDateTime(byte[] arg)
        {
            return new DateTime(BitConverter.ToInt64(arg, 1));
        }
        #endregion

        #region Char Byte SByte

        private byte[] SerializeSByte(object arg)
        {
            byte[] buf = new byte[2];
            buf[0] = (byte)TypeCode.SByte;
            buf[1] = (byte)arg;
            return buf;
        }

        private object DeSerializeSByte(byte[] arg)
        {
            return (sbyte)arg[1]; // duh
        }

        private byte[] SerializeByte(object arg)
        {
            byte[] buf = new byte[2];
            buf[0] = (byte)TypeCode.Byte;
            buf[1] = (byte)arg;
            return buf;
        }

        private object DeSerializeByte(byte[] arg)
        {
            return arg[1]; // duh
        }

        private byte[] SerializeChar(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.Char, BitConverter.GetBytes((Char)arg));
        }

        private object DeSerializeChar(byte[] arg)
        {
            return BitConverter.ToChar(arg, 1);
        }
        #endregion

        #region IntXX UIntXX
        private byte[] SerializeInt16(object arg)
        {
            return TypeEncodeBuffer((byte)TypeCode.Int16, BitConverter.GetBytes((Int16)arg));
        }

        private object DeSerializeInt16(byte[] arg)
        {
            return BitConverter.ToInt16(arg, 1);
        }

        private byte[] SerializeInt32(object arg)
        {
            Int32 v = (Int32)arg;
            byte[] bufVal = BitConverter.GetBytes(v);
            return TypeEncodeBuffer((byte)TypeCode.Int32, bufVal);
        }

        private object DeSerializeInt32(byte[] arg)
        {
            return BitConverter.ToInt32(arg, 1);
        }

        private byte[] SerializeInt64(object arg)
        {
            Int64 v = (Int64)arg;
            byte[] bufVal = BitConverter.GetBytes(v);
            return TypeEncodeBuffer((byte)TypeCode.Int64, bufVal);
        }

        private object DeSerializeInt64(byte[] arg)
        {
            return BitConverter.ToInt64(arg, 1);
        }

        private byte[] SerializeUInt16(object arg)
        {
            UInt16 v = (UInt16)arg;
            byte[] bufVal = BitConverter.GetBytes(v);
            return TypeEncodeBuffer((byte)TypeCode.UInt16, bufVal);
        }

        private object DeSerializeUInt16(byte[] arg)
        {
            return BitConverter.ToUInt16(arg, 1);
        }

        private byte[] SerializeUInt32(object arg)
        {
            UInt32 v = (UInt32)arg;
            byte[] bufVal = BitConverter.GetBytes(v);
            return TypeEncodeBuffer((byte)TypeCode.UInt32, bufVal);
        }

        private object DeSerializeUInt32(byte[] arg)
        {
            return BitConverter.ToUInt32(arg, 1);
        }

        private byte[] SerializeUInt64(object arg)
        {
            UInt64 v = (UInt64)arg;
            byte[] bufVal = BitConverter.GetBytes(v);
            return TypeEncodeBuffer((byte)TypeCode.String, bufVal);
        }

        private object DeSerializeUInt64(byte[] arg)
        {
            return BitConverter.ToUInt64(arg, 1);
        }
        #endregion

        #region Boolean
        private byte[] SerializeBoolean(object arg)
        {
            Boolean b = (Boolean)arg;
            byte[] buf = new byte[2];
            buf[0] = (byte)TypeCode.Boolean;
            buf[1] = b ? (byte)1 : (byte)0;
            return buf;
        }

        private object DeSerializeBoolean(byte[] arg)
        {
            return arg[1] > 0 ? true : false;
        }
        #endregion

        #region String
        private byte[] SerializeString(object arg)
        {
            String s = arg as String;
            byte[] buf = ASCIIEncoding.UTF8.GetBytes(s);
            return TypeEncodeBuffer((byte)TypeCode.String, buf);
        }

        private object DeSerializeString(byte[] arg)
        {
            return ASCIIEncoding.UTF8.GetString(arg, 1, arg.Length - 1);
        }
        #endregion

        #region Empty / Null
        private byte[] SerializeEmpty(object arg)
        {
            return empty;
        }

        private object DeSerializeEmpty(byte[] arg)
        {
            return null;
        }
        #endregion
        #endregion
    }
}
