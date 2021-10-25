using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace BlackTundra.Foundation.Serialization {

    /// <summary>
    /// Responsible for serialization and deserialization of objects.
    /// </summary>
    public static class ObjectSerializer {

        #region constant

        /// <summary>
        /// Contains overrides for non-serializable types to serializable implementations of that type.
        /// </summary>
        /// <seealso cref="SerializableImplementationOfAttribute"/>
        private static readonly Dictionary<Type, Type> TypeOverrideDictionary;

        #endregion

        #region constructor

        static ObjectSerializer() {
            TypeOverrideDictionary = new Dictionary<Type, Type>();
            IEnumerable<Type> serializableImplementations = ObjectUtility.GetDecoratedTypes<SerializableImplementationOfAttribute>();
            foreach (Type implementation in serializableImplementations) {
                if (!implementation.HasAttribute<SerializableAttribute>()) {
                    Console.Error($"Invalid serializable implementation \"{implementation.FullName}\": Not decorated with {typeof(SerializableAttribute).FullName}.");
                    continue;
                }
                SerializableImplementationOfAttribute[] attributes = implementation.GetAttributes<SerializableImplementationOfAttribute>();
                SerializableImplementationOfAttribute attribute;
                for (int i = attributes.Length - 1; i >= 0; i--) {
                    attribute = attributes[i];
                    if (TypeOverrideDictionary.TryGetValue(attribute.target, out Type original)) { // already contains implementation
                        Console.Error($"Duplicate serializable implementation of type \"{attribute.target.FullName}\" (original: \"{original.FullName}\", current: \"{implementation.FullName}\").");
                        continue;
                    }
                    ConstructorInfo constructorInfo = implementation.GetConstructor( // get constructor information for target type
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new Type[] { attribute.target },
                        null
                    );
                    if (constructorInfo == null) { // no constructor for target type
                        Console.Error($"Type \"{implementation.FullName}\" contains an invalid serializable implementation of type \"{attribute.target.FullName}\": No constructor with signature \"({attribute.target.FullName})\" found.");
                        continue;
                    }
                    TypeOverrideDictionary.Add(attribute.target, implementation); // register this implementation as a serializable implementation of the target type
                    Console.Info($"[ObjectUtility] Bound \"{attribute.target.FullName}\" -> \"{implementation.FullName}\"");
                }
            }
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
                serializeFromMemory = false; // mark as false so success can be tracked
                if (TypeOverrideDictionary.TryGetValue(type, out Type newType)) { // check if the type if overridden
                    ConstructorInfo constructor = newType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { type }, null); // find the constructor to construct the new type
                    if (constructor != null) { // the constructor was found
                        obj = constructor.Invoke(new object[] { obj }); // construct the new type
                        type = newType;
                        serializeFromMemory = true; // mark as true, the object can now be serialized from memory
                    } else {
                        Console.Error($"Failed to find constructor for object serialization type override {type.FullName} -> {newType.FullName}."); // log failure
                    }
                }
                if (!serializeFromMemory) throw new NotSupportedException($"Cannot serialize object of type \"{type.FullName}\".");
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
                    try {
                        binaryFormatter.Serialize(memoryStream, obj); // try to serialize the object to the memory stream
                    } catch (SerializationException exception) {
                        Console.Error($"Failed to serialize object of type \"{type.FullName}\".", exception);
                        throw exception;
                    }
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

            if (wasSerializable && TypeOverrideDictionary.TryGetValue(type, out Type newType)) // object was marked as serializable from memory
                type = newType; // convert to override type

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

    }

}