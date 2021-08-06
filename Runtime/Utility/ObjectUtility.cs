using System;

using System.IO;

using System.Collections.Generic;

using System.Reflection.Emit;

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace BlackTundra.Foundation.Utility {

    public static class ObjectUtility {

        #region constant

        private static readonly Dictionary<Type, int> SizeOfDictionary;

        #endregion

        #region constructor

        static ObjectUtility() {

            SizeOfDictionary = new Dictionary<Type, int>();

        }

        #endregion

        #region logic

        #region Size

        public static int SizeOf<T>() where T : struct => Marshal.SizeOf(default(T));

        public static int SizeOf(this Type type) {

            if (type == null) throw new ArgumentNullException(nameof(type));

            if (SizeOfDictionary.TryGetValue(type, out int size)) return size;
            size = SizeOfType(type);
            SizeOfDictionary.Add(type, size);
            return size;

        }

        #endregion

        #region SizeOfType

        private static int SizeOfType(in Type type) {

            DynamicMethod dynamicMethod = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            return (int)dynamicMethod.Invoke(null, null);

        }

        #endregion

        #region SerializeToBytes

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="includeType">
        /// When true, the object will be serialized with the object type. This is useful for deserializing an object you don't know
        /// the type of.
        /// </param>
        public static byte[] SerializeToBytes(this object obj, in bool includeType = false) {

            if (obj == null) throw new ArgumentNullException(nameof(obj));

            Type type = obj.GetType(); // get the type of the object

            bool serializable = type.IsSerializable; // check if the type can be serialized
            bool serializeFromMemory; // create a boolean to track if the object can be serialized directly from its value in memory

            byte flag = (byte)(serializable ? 0x01 : 0x00); // create a flag that helps the deserializer identify how the object has been serialized

            if (!serializable) { // the object is not by default serializable but could be translated into a serializable format
                if (obj is Vector2 vector2) {
                    obj = new SerializableVector2(vector2);
                } else if (obj is Vector3 vector3) {
                    obj = new SerializableVector3(vector3);
                } else if (obj is Vector4 vector4) {
                    obj = new SerializableVector4(vector4);
                } else if (obj is Quaternion quaternion) {
                    obj = new SerializableQuaternion(quaternion);
                } else throw new NotSupportedException($"Cannot serialize object of type \"{type.FullName}\"."); // there was no serializable format for this object type
                serializeFromMemory = true; // the object can now be serialized directly from memory
            } else { // the object is by default serializable
                serializeFromMemory = type.IsValueType && (type.IsPrimitive || type.IsEnum); // a type is serializable from memory if it is a value type and is primitive or an enum
            }

            if (serializeFromMemory && !includeType) { // can be taken directly from memory

                int size = Marshal.SizeOf(obj); // find the size in memory that the object will take up, all value-types should take the same amount of memory as eachother
                byte[] buffer = new byte[size + 1]; // create a buffer with enough space for the flag and serialized bytes
                IntPtr bufferPtr = Marshal.AllocHGlobal(size); // create a pointer to use in the buffer
                try {
                    Marshal.StructureToPtr(obj, bufferPtr, false); // convert the stucture in memory to a pointer (get a pointer to the object)
                    Marshal.Copy(bufferPtr, buffer, 1, size); // copy the contents of the memory structure to the buffer using the pointer
                } finally {
                    Marshal.FreeHGlobal(bufferPtr); // free the pointer
                }

                flag |= 0x02; // assign a flag that tells the deserializer that the object was serialized from memory
                buffer[0] = flag; // write the flag to the buffer
                return buffer; // return the serialized bytes that have now been written to the buffer

            } else { // requires binary formatter for serialization

                BinaryFormatter binaryFormatter = new BinaryFormatter(); // create a binary formatter to format the object (which is likely a class)
                using (MemoryStream memoryStream = new MemoryStream()) { // create a memory stream
#if UNITY_EDITOR
                    try {
                        binaryFormatter.Serialize(memoryStream, obj); // try to serialize the object to the memory stream
                    } catch (SerializationException exception) {
                        Debug.LogWarning("Check the object attempting to be serialized and its members are serializable.");
                        throw exception;
                    }
#else
                    binaryFormatter.Serialize(memoryStream, obj); // try to serialize the object to the memory stream
#endif
                    return memoryStream.ToArray().AddFirst(flag); // convert the contents of the memory stream to a byte array and make sure to add the flag to the start
                }

            }

        }

        #endregion

        #region ToObject

        public static T ToObject<T>(in byte[] bytes) {

            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            int size = bytes.Length - 1;
            if (size < 1) return default;

            byte flag = bytes[0]; // get the flag
            bool wasSerializable = (flag & 0x01) != 0x00; // the data was serializable in its original format
            bool wasSerializedFromMemory = (flag & 0x02) != 0x00; // the data was serialized directly from memory

            Type type = typeof(T);

            if (wasSerializable) {
                if (type == typeof(Vector3)) type = typeof(SerializableVector3);
                else if (type == typeof(Quaternion)) type = typeof(SerializableQuaternion);
                else if (type == typeof(Vector2)) type = typeof(SerializableVector2);
                else if (type == typeof(Vector4)) type = typeof(SerializableVector4);
            }

            object obj;

            if (wasSerializedFromMemory) { // convert bytes directly to type in memory

                int typeSize;
                try {
                    typeSize = Marshal.SizeOf(type);
                } catch (ArgumentException exception) { // an ArgumentException is likely to be thrown if a type cannot be marshaled
                    throw new ArgumentException(
                        $"Failed to find size of \"{type.AssemblyQualifiedName}\".",
                        exception
                    );
                }
                if (size != typeSize) throw new ArgumentException($"Type size mismatch (type: \"{type.AssemblyQualifiedName}\", size: {size}, type size: {typeSize}).");

                IntPtr bufferPtr = Marshal.AllocHGlobal(size);
                try {
                    Marshal.Copy(bytes, 1, bufferPtr, size);
                    obj = Marshal.PtrToStructure(bufferPtr, type);
                } finally {
                    Marshal.FreeHGlobal(bufferPtr);
                }

            } else { // deserialize the data using a binary formatter

                using (MemoryStream memoryStream = new MemoryStream()) {

                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    memoryStream.Write(bytes, 1, bytes.Length - 1);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    obj = binaryFormatter.Deserialize(memoryStream);

                }

            }

            return (T)obj;

        }

        #endregion

        #region ToGUID

        public static int ToGUID(this string value) {

            if (value == null) return -1;

            unchecked {

                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < value.Length; i += 2) {
                    hash1 = ((hash1 << 5) + hash1) ^ value[i];
                    if (i == value.Length - 1) break;
                    hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
                }

                return hash1 + (hash2 * 1566083941);

            }

        }

        public static int ToGUID(this int value) => value;

        public static int ToGUID(this long value) {

            unchecked {

                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                hash1 = ((hash1 << 5) + hash1) ^ (int)(value & uint.MaxValue);
                hash2 = ((hash2 << 5) + hash2) ^ (int)((value >> 32) & uint.MaxValue);

                return hash1 + (hash2 * 1566083941);

            }

        }

        public static int ToGUID(this ulong value) {

            unchecked {

                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                hash1 = ((hash1 << 5) + hash1) ^ (int)(value & uint.MaxValue);
                hash2 = ((hash2 << 5) + hash2) ^ (int)((value >> 32) & uint.MaxValue);

                return hash1 + (hash2 * 1566083941);

            }

        }

        public static int ToGUID(this float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);

        public static int ToGUID(this byte[] buffer) {

            unchecked {

                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < buffer.Length; i++)
                    hash = (hash ^ buffer[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;

                return hash;

            }

            /*
            bool alt = false;
            unchecked {

                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < buffer.Length; i += 4) {
                    int value = 0;
                    for (int j = i; j < i + 4 && j < buffer.Length; j++)
                        value |= buffer[j] >> (j * 8);
                    if (alt) hash2 = ((hash2 << 5) + hash2) ^ value;
                    else hash1 = ((hash1 << 5) + hash1) ^ value;
                }

                return hash1 + (hash2 * 1566083941);

            }
            */
        }

        #endregion

        #endregion

    }

}