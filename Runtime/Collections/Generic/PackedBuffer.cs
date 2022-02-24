/*
 * TODO:
 * - Use Array.Copy and other built-in C# methods for changing the buffer array.
 * - Implement any remaining built-in C# interfaces so the buffer can be used like LinkedList<T> and Queue<T>.
 */

using BlackTundra.Foundation.Utility;

using System;
using System.Collections;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// Buffer that packs references towards the start of the buffer so its fast to iterate and add and remove elements.
    /// </summary>
    [Serializable]
    public sealed class PackedBuffer<T> : IEquatable<PackedBuffer<T>>, IEnumerable<T> {

        #region constant

        /// <summary>
        /// <see cref="BufferException"/> message when the buffer is full.
        /// </summary>
        private const string BufferFullMessage = "PackedBuffer reached maximum capacity.";

        #endregion

        #region nested

        public sealed class Enumerator : IEnumerator<T>, IEnumerator {

            #region variable

            private readonly PackedBuffer<T> packedBuffer;
            private readonly int count;
            private int index;

            #endregion

            #region property

            public T Current => packedBuffer[index];

            object IEnumerator.Current => packedBuffer[index];

            #endregion

            #region constructor

            internal Enumerator(in PackedBuffer<T> packedBuffer) {
                this.packedBuffer = packedBuffer;
                count = packedBuffer.Count;
                index = -1;
            }

            #endregion

            #region logic

            public void Dispose() { }

            public bool MoveNext() => ++index < count;

            public void Reset() => index = -1;

            #endregion

        }

        #endregion

        #region variable

        /*
         * Since the class is serializable, no variables can be readonly since they won't be able to be
         * serialized/deserialized correctly.
         */

        /// <summary>
        /// Buffer containing every element.
        /// </summary>
        private T[] buffer;

        /// <summary>
        /// First index that is a null reference.
        /// <c>-1</c> if there is no null reference (buffer is full).
        /// </summary>
        private int lastIndex;

        /// <summary>
        /// Value used to track if a value is valueless/empty.
        /// </summary>
#pragma warning disable IDE0044 // add readonly modifier
        private T emptyValue;
#pragma warning restore IDE0044 // add readonly modifier

        #endregion

        #region property

        /// <summary>
        /// Length / capacity of the packed buffer.
        /// </summary>
        public int Capacity => buffer.Length;

        /// <summary>
        /// Number of entries in the packed buffer.
        /// </summary>
        public int Count => lastIndex == -1 ? buffer.Length : lastIndex;

        /// <summary>
        /// True if the buffer is full.
        /// </summary>
        public bool IsFull => lastIndex == -1 || lastIndex == buffer.Length;

        /// <summary>
        /// <c>true</c> if the buffer is empty.
        /// </summary>
        /// <seealso cref="HasElements"/>
        public bool IsEmpty => lastIndex == 0;

        /// <summary>
        /// <c>true</c> if the buffer has elements.
        /// </summary>
        /// <seealso cref="IsEmpty"/>
        public bool HasElements => lastIndex != 0;

        /// <summary>
        /// Number of spaces available in the buffer.
        /// </summary>
        public int RemainingSpace => lastIndex == -1 ? 0 : (buffer.Length - lastIndex);

        /// <summary>
        /// Gets an element at an index.
        /// </summary>
        public T this[in int index] => buffer[index];

        /// <summary>
        /// Gets the index of an object in the buffer.
        /// </summary>
        /// <returns>Lowest index of the object in the game or -1 if the object wasn't found.</returns>
        public int this[in T obj] {
            get {
                if (obj == null) throw new ArgumentNullException("obj");
                T temp;
                int length = lastIndex == -1 ? buffer.Length : lastIndex;
                for (int i = 0; i < length; i++) {
                    temp = buffer[i];
                    if (obj.Equals(temp)) return i;
                }
                return -1;
            }
        }

        /// <summary>
        /// First element in the <see cref="PackedBuffer{T}"/>.
        /// </summary>
        public T First => Count == 0 ? default : buffer[0];

        /// <summary>
        /// Last element in the <see cref="PackedBuffer{T}"/>.
        /// </summary>
        public T Last {
            get {
                int index = (lastIndex == -1 ? buffer.Length : lastIndex) - 1; // calculate the index of the last element
                return index == -1 ? default : buffer[index]; // return the last element
            }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a packed buffer with an initial capacity.
        /// </summary>
        public PackedBuffer(in int capacity) {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            buffer = new T[capacity];
            lastIndex = 0;
            emptyValue = default;
        }

        /// <inheritdoc cref="PackedBuffer{T}.PackedBuffer(in int, in T)"/>
        public PackedBuffer(in T[] array, in int startIndex, in int length, in int capacity) {
            if (array == null) throw new ArgumentNullException(nameof(array));
            int count = array.Length;
            if (startIndex < 0 || startIndex >= count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0 || length > count - startIndex) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (length > capacity) throw new ArgumentException($"{nameof(length)} cannot be greater than {nameof(capacity)}");
            buffer = new T[capacity];
            if (length > 0) {
                Array.Copy(array, 0, buffer, startIndex, length);
                Pack();
            }
            emptyValue = default;
        }

        #endregion

        #region logic

        #region Expand

        /// <summary>
        /// Expands the packed buffer by <paramref name="capacity"/> elements.
        /// </summary>
        /// <param name="capacity">Number of new indicies to add.</param>
        public void Expand(in int capacity) {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");
            if (capacity > 0) {
                int length = buffer.Length;
                int newLength = length + capacity;
                T[] newBuffer = new T[newLength];
                for (int i = 0; i < length; i++) newBuffer[i] = buffer[i];
                for (int i = length; i < newLength; i++) newBuffer[i] = default;
                buffer = newBuffer;
            }
        }

        #endregion

        #region EmptyBuffer

        /// <summary>
        /// Empties the entire buffer.
        /// </summary>
        private void EmptyBuffer() { for (int i = 0; i < buffer.Length; i++) buffer[i] = emptyValue; }

        #endregion

        #region Shrink

        /// <summary>
        /// Shrinks the packed buffer by shaving off unused values at the end of the buffer.
        /// This can be used if a buffer gets too large and needs to be shrank to save memory.
        /// </summary>
        /// <param name="amount">Amount of cells to shave from the end of the buffer. If this is -1 the maximum number of (non-null) elements will be removed from the end.</param>
        /// <param name="removeExisting">When true, existing elements can be removed from the end of the buffer.</param>
        /// <returns>Number of cells removed from the buffer.</returns>
        public int Shrink(int amount = -1, in bool removeExisting = false) {

            #region process amount

            if (amount < -1) throw new ArgumentOutOfRangeException(string.Concat(nameof(amount), " must be a positive integer or -1."));
            if (amount == 0) return 0; // nothing to shave
            int length = buffer.Length; // get the current length of the buffer
            int count = Count;
            int maxAmount = length - count; // find the maximum amount of cells that can be removed
            if (amount == -1 || (!removeExisting && amount > maxAmount)) amount = maxAmount; // apply an upper clamp to the amount
            else if (removeExisting && amount >= count) { Clear(); return count; } // remove all elements from buffer

            #endregion

            int newSize = length - amount; // find the new size of the buffer
            T[] newBuffer = new T[newSize]; // create a new buffer
            Array.Copy(buffer, newBuffer, newSize);
            buffer = newBuffer; // assign the new buffer
            return amount; // return the amount of cells removed

        }

        #endregion

        #region TryShrink

        /// <summary>
        /// Try to shrink the buffer by a set <paramref name="amount"/>. The buffer will only be shrank if the
        /// <paramref name="amount"/> specified is available as free space (or the buffer is completely empty).
        /// </summary>
        /// <param name="amount">Number of elements to remove in the shrink operation.</param>
        /// <returns>Returns <c>true</c> if the buffer was shrank successfully.</returns>
        public bool TryShrink(int amount) {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); // out of range
            if (amount > buffer.Length) amount = buffer.Length; // apply max clamp
            if (lastIndex == -1) return false; // last index is -1, meaning the buffer is full
            int newSize = buffer.Length - amount; // calculate the size of the buffer after the shrink operation
            if (newSize < lastIndex) return false; // check if the buffer can be shrank by the set amount
            T[] newBuffer = new T[newSize]; // construct a new buffer
            Array.Copy(buffer, newBuffer, newSize); // copy the contents of the current buffer to the new one
            buffer = newBuffer; // re-assign the buffer
            return true; // return true
        }

        #endregion

        #region Pack

        private void Pack() {
            if (buffer.Length == 0) return;
            lastIndex = -1; // last index that was null
            for (int i = 0; i < buffer.Length; i++) { // iterate buffer
                if (buffer[i] != null) { // not null
                    if (lastIndex != -1) {
                        buffer[lastIndex] = buffer[i]; // move to last empty index
                    }
                } else if (lastIndex == -1) { // null and there is no recorded last empty index
                    lastIndex = i;
                }
            }
            if (lastIndex != -1) { // the buffer has some null references somewhere (as if this was still -1 it means the buffer is full)
                for (int i = lastIndex; i < buffer.Length; i++) buffer[i] = emptyValue; // remove remaining buffer references
            }
        }

        #endregion

        #region +

        public static PackedBuffer<T> operator +(PackedBuffer<T> buffer, T obj) { buffer.AddLast(obj); return buffer; }

        #endregion

        #region -

        public static PackedBuffer<T> operator -(PackedBuffer<T> buffer, T obj) { buffer.Remove(obj); return buffer; }

        #endregion

        #region AddFirst

        /// <summary>
        /// Adds an element to the front of the buffer.
        /// </summary>
        /// <param name="obj">Object to add to the buffer.</param>
        /// <returns>Returns the index that the object was added at or if the object is already added and allowOneInstance is true the lowest index </returns>
        public int AddFirst(in T obj) {
            if (obj == null || obj.Equals(emptyValue)) throw new ArgumentException("obj");
            if (lastIndex >= buffer.Length) throw new BufferException(BufferFullMessage, null);
            buffer.ShiftRight();
            buffer[0] = obj;
            return 0;
        }

        /// <summary>
        /// Adds an element to the front of the buffer.
        /// </summary>
        /// <param name="obj">Object to add to the buffer.</param>
        /// <param name="allowOneInstance">When true, only one instance of the object is allowed into the buffer. This first repeat instance will be returned or if none is found, the object will be added to the end of the buffer.</param>
        /// <returns>Returns the index that the object was added at or if the object is already added and allowOneInstance is true the lowest index </returns>
        public int AddFirst(in T obj, in bool allowOneInstance) {
            if (obj == null || obj.Equals(emptyValue)) throw new ArgumentException("obj");
            if (allowOneInstance) {
                int index = this[obj];
                if (index != -1) return index;
            }
            return AddFirst(obj);
        }

        public void AddFirst(in T[] objs, in bool allowOneInstance) {

            if (objs == null) throw new ArgumentNullException("objs");

            int count = objs.Length;
            if (count == 0) return;
            if (count > RemainingSpace) throw new BufferException(BufferFullMessage, null);

            buffer.ShiftRight(count);
            bool requiresPack = false;
            for (int i = 0; i < count; i++) {
                T obj = objs[i];
                if (obj == null || obj.Equals(emptyValue) || (allowOneInstance && this[obj] != -1)) {
                    requiresPack = true;
                    buffer[i] = emptyValue;
                } else {
                    buffer[i] = obj;
                    lastIndex++;
                }
            }
            if (requiresPack) Pack();

        }

        #endregion

        #region AddLast

        /// <summary>
        /// Adds an element to the end of the buffer.
        /// </summary>
        /// <param name="obj">Object to add to the buffer.</param>
        /// <returns>Returns the index that the object was added at.</returns>
        public int AddLast(in T obj) {
            if (obj == null) throw new ArgumentNullException("obj");
            if (lastIndex >= buffer.Length) throw new BufferException(BufferFullMessage, null);
            buffer[lastIndex] = obj;
            return lastIndex++;
        }

        /// <summary>
        /// Adds an element to the end of the buffer.
        /// </summary>
        /// <param name="obj">Object to add to the buffer.</param>
        /// <param name="allowOneInstance">
        /// When <c>true</c>, only one instance of the object will be allowed in the buffer.
        /// <returns>
        /// Returns the index that the object was added or the first (lowest) index where an instance of
        /// the object already exists in the array provided <c>allowOneInstance</c> is <c>true</c>.
        /// </returns>
        public int AddLast(in T obj, in bool allowOneInstance) {
            if (obj == null) throw new ArgumentNullException("obj");
            if (allowOneInstance) {
                int index = this[obj];
                if (index != -1) return index;
            }
            return AddLast(obj);
        }

        /// <summary>
        /// Adds an element to the end of the buffer.
        /// </summary>
        /// <param name="objs">Array of objects to insert into the buffer.</param>
        /// <param name="allowOneInstance">
        /// When <c>true</c>, only one instance of the object will be allowed in the buffer.
        /// <returns>
        /// Returns the index that the object was added or the first (lowest) index where an instance of
        /// the object already exists in the array provided <c>allowOneInstance</c> is <c>true</c>.
        /// </returns>
        public void AddLast(in T[] objs, in bool allowOneInstance) {
            if (objs == null) throw new ArgumentNullException("objs");
            int count = objs.Length;
            if (count == 0) return;
            if (count > RemainingSpace) throw new BufferException(BufferFullMessage, null);
            for (int i = 0; i < count; i++) {
                T obj = objs[i];
                if ((obj != null || (emptyValue == null || emptyValue.Equals(obj))) && (!allowOneInstance || this[obj] == -1)) // check instance can be added to the buffer
                    buffer[lastIndex++] = obj; // add to buffer
            }
        }

        #endregion

        #region AddAt

        /// <summary>
        /// Adds an object to the buffer at a specified position.
        /// </summary>
        /// <param name="obj">Object to insert into the buffer.</param>
        /// <param name="index">
        /// Index to insert the object into the buffer at. If the index is greater or equal to the number of
        /// elements in the buffer, the object will be inserted at the end of the buffer.
        /// </param>
        public void AddAt(in T obj, in int index) {

            #region argument checks
            if (obj == null) throw new ArgumentNullException("objs");
            int bufferSize = buffer.Length; // get the size of the buffer
            if (index < 0 || index > bufferSize) throw new ArgumentOutOfRangeException("index");
            #endregion

            if (lastIndex == -1) throw new BufferException(BufferFullMessage, null); // buffer is full
            int remainingSpace = bufferSize - lastIndex; // calculate the space remaining inside the buffer
            if (remainingSpace < 1) throw new BufferException(BufferFullMessage, null); // not enough space to insert into the buffer
            if (index < lastIndex) { // object needs to be inserted before the last element of the buffer
                Array.Copy(buffer, index, buffer, index + 1, buffer.Length - index); // shift all elements to the right
                buffer[index] = obj; // insert at target index
                lastIndex += 1;
            } else {
                buffer[lastIndex++] = obj; // insert at the end of the buffer since this is a packed buffer so all data is packed to the start of the buffer
            }

        }

        /// <summary>
        /// Adds an object to the buffer at a specified position.
        /// </summary>
        /// <param name="obj">Object to insert into the buffer.</param>
        /// <param name="index">
        /// Index to insert the object into the buffer at. If the index is greater or equal to the number of
        /// elements in the buffer, the object will be inserted at the end of the buffer.
        /// </param>
        /// <param name="allowOneInstance">
        /// When <c>true</c>, only one instance of the object will be allowed in the buffer.
        /// <returns>
        /// Returns the index that the object was added or the first (lowest) index where an instance of
        /// the object already exists in the array provided <c>allowOneInstance</c> is <c>true</c>.
        /// </returns>
        public int AddAt(in T obj, in int index, in bool allowOneInstance) {

            #region argument checks
            if (obj == null) throw new ArgumentNullException("objs");
            int bufferSize = buffer.Length; // get the size of the buffer
            if (index < 0 || index > bufferSize) throw new ArgumentOutOfRangeException("index");
            #endregion

            #region allow one instance
            if (allowOneInstance) { // only allow one instance of the object in the buffer
                int firstIndex = this[obj]; // find the first occurance of the obj in the buffer
                if (firstIndex != -1) return firstIndex; // an instance was found, return the index
            }
            #endregion

            if (lastIndex == -1) throw new BufferException(BufferFullMessage, null); // buffer is full
            int remainingSpace = bufferSize - lastIndex; // calculate the space remaining inside the buffer
            if (remainingSpace < 1) throw new BufferException(BufferFullMessage, null); // not enough space to insert into the buffer
            if (index < lastIndex) {
                Array.Copy(buffer, index, buffer, index + 1, buffer.Length - index); // shift all elements to the right
                buffer[index] = obj; // insert at target index
                lastIndex += 1;
            } else {
                buffer[lastIndex++] = obj; // insert at the end of the buffer since this is a packed buffer so all data is packed to the start of the buffer
            }
            return index;

        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes an object from the buffer.
        /// </summary>
        /// <param name="obj">Object to remove from the buffer.</param>
        /// <returns>Number of times the object was removed from the buffer.</returns>
        public int Remove(in T obj) {

            int count = 0; // counts the number of times the object was removed from the buffer

            int target; // temporary variable
            while ((target = this[obj]) != -1) { // find each instance of the object and remove it
                RemoveAt(target); // remove at the index the object was found at
                count++;
            }

            return count; // return the number of times the object was removed from the buffer

        }

        #endregion

        #region RemoveAll

        public int[] RemoveAll(in T[] objs) {

            if (objs == null) throw new ArgumentNullException("objs");
            if (objs.Length == 0) return new int[0];

            int count = objs.Length;
            for (int i = 0; i < count; i++) { if (objs[i] == null) throw new ArgumentException($"objs contains a null reference at index {i}."); }

            int[] counts = new int[count];
            for (int i = 0; i < count; i++) counts[i] = Remove(objs[i]);
            return counts;

        }

        #endregion

        #region RemoveAt

        /// <summary>
        /// Removes an element at a certain index.
        /// </summary>
        /// <param name="index">Index to remove at.</param>
        /// <returns>Returns the element that was removed.</returns>
        public T RemoveAt(in int index) {

            if (index < 0 || index >= lastIndex) throw new ArgumentOutOfRangeException("index");

            T temp = buffer[index]; // store the value of the target element to remove
            if (temp == null || temp.Equals(emptyValue)) return temp; // the item doesn't exist

            if (index == lastIndex - 1) buffer[index] = emptyValue; // the last element is the one being removed
            else { // move the end back to the start (pack the buffer)
                Array.Copy(buffer, index + 1, buffer, index, lastIndex - index - 1);
                //for (int i = index + 1; i < lastIndex; i++) buffer[i - 1] = buffer[i];
            }
            lastIndex--; // reduce the last index by one

            return temp; // return the removed element

        }

        /// <summary>
        /// Removes a range of values by their index.
        /// </summary>
        /// <param name="indexes">Indexes to remove items from.</param>
        /// <returns>Returns a <see cref="List{T}"/> of elements that were removed from the buffer. Only non-empty/non-null values are added to the list.</returns>
        public IEnumerable<T> RemoveAt(in IEnumerable<int> indexes) {
            List<T> removalList = new List<T>();
            T temp;
            foreach (int index in indexes) {
                temp = buffer[index];
                if (temp != null && !temp.Equals(emptyValue)) removalList.Add(temp);
                buffer[index] = emptyValue;
            }
            Pack();
            return removalList;
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear() {
            EmptyBuffer();
            lastIndex = 0;
        }

        public void Clear(in int newSize) {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");
            if (newSize == buffer.Length) EmptyBuffer();
            else buffer = new T[newSize];
            lastIndex = 0;
        }

        /// <summary>
        /// Clears the buffer and sets the buffer to a new set of values.
        /// </summary>
        /// <param name="newValues">New values to assign to the buffer.</param>
        public void Clear(in T[] newValues) {
            int expandSize = newValues.Length - buffer.Length;
            if (expandSize > 0) buffer = new T[newValues.Length];
            Array.Copy(newValues, 0, buffer, 0, newValues.Length);
            for (int i = newValues.Length; i < buffer.Length; i++) buffer[i] = emptyValue;
            Pack();
        }

        #endregion

        #region Contains

        /// <returns>Returns <c>true</c> if the <paramref name="value"/> is contained within the <see cref="PackedBuffer{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Contains(in T value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return IndexOf(value) != -1;
        }

        #endregion

        #region IndexOf

        public int IndexOf(in T obj) => buffer.FindIndexOf(obj);

        #endregion

        #region Equals

        public bool Equals(PackedBuffer<T> packedBuffer) {
            if (packedBuffer == null) return false; // the provided buffer is null
            int count = Count; // get the number of elements in this buffer
            if (count != packedBuffer.Count) return false; // different number of elements
            T[] otherBuffer = packedBuffer.buffer; // find a reference to the other buffer
            for (int i = 0; i < count; i++) { if (!buffer[i].Equals(otherBuffer[i])) return false; } // find elements that don't match
            return true; // no non-matching elements found
        }

        #endregion

        #region GetEnumerator

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        #endregion

        #region ToArray

        /// <summary>
        /// Converts the packed buffer to a packed array.
        /// </summary>
        /// <returns>Array containing no null references.</returns>
        public T[] ToArray() {
            int count = Count;
            T[] array = new T[count];
            if (count == 0) return array;
            Array.Copy(buffer, 0, array, 0, count);
            return array;
        }

        #endregion

        #region Swap

        /// <summary>
        /// Swaps two elements by index.
        /// </summary>
        public void Swap(in int i0, in int i1) {
            int count = lastIndex == -1 ? buffer.Length : lastIndex;
            if (i0 < 0 || i0 >= count) throw new ArgumentOutOfRangeException("i0");
            if (i1 < 0 || i1 >= count) throw new ArgumentOutOfRangeException("i1");
            if (i0 == i1) return;
            T temp = buffer[i0];
            buffer[i0] = buffer[i1];
            buffer[i1] = temp;
        }

        #endregion

        #region Remap

        /// <summary>
        /// Remaps (rearranges) the <see cref="PackedBuffer{T}"/> elements based off of an <paramref name="indexMap"/>.
        /// </summary>
        /// <param name="indexMap">
        /// Maps the current index in the <see cref="PackedBuffer{T}"/> to a new index. This array must be the same length as the
        /// current <see cref="Count">number of elements in the <see cref="PackedBuffer{T}"/></see>.
        /// </param>
        /// <remarks>
        /// No checks are performed to ensure that every element is remapped. It is up to the provider of the <paramref name="indexMap"/>
        /// to ensure the map remaps every element.
        /// </remarks>
        public void Remap(in int[] indexMap) {
            if (indexMap == null) throw new ArgumentNullException(nameof(indexMap));
            int bufferLength = buffer.Length;
            int count = lastIndex == -1 ? bufferLength : lastIndex;
            if (count != indexMap.Length) throw new ArgumentException($"{nameof(indexMap)} must have same length as {nameof(Count)}.");
            if (count < 2) return;
            T[] newBuffer = new T[bufferLength];
            int index;
            for (int i = count - 1; i >= 0; i--) {
                index = indexMap[i];
                if (index < 0 || index >= count) throw new ArgumentException($"{nameof(indexMap)} has invalid mapping at index {i}: `{index}`. Must be in range `0` (inclusive) -> `{count}` (exclusive).");
                newBuffer[i] = buffer[index];
            }
            Array.Copy(newBuffer, 0, buffer, 0, count);
            Pack(); // pack, this ensures that if any elements are missed, the empty gaps are removed
        }

        #endregion

        #endregion

    }

}