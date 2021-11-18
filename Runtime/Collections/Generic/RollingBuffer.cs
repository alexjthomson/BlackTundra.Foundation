using System;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// Allows elements to be pushed to a buffer one after the other.
    /// Once the end of the buffer is reached, the next element will be pushed
    /// to the front of the buffer (it will loop around).
    /// </summary>
    [Serializable]
    public sealed class RollingBuffer<T> {

        #region variable

        /// <summary>
        /// Buffer containing every element in the rolling buffer.
        /// </summary>
        private T[] buffer;

        /// <summary>
        /// Next index in the <see cref="buffer"/>.
        /// </summary>
        private int nextIndex;

        /// <summary>
        /// When true, existing values in the rolling buffer can be overridden.
        /// </summary>
        private readonly bool overrideExisting;

        #endregion

        #region property

        /// <summary>
        /// When index is 0, NextValue is returned.
        /// </summary>
        /// <param name="index">Offset index.</param>
        /// <returns></returns>
        public T this[in int index] {
            get {
                if (index < 0 || index >= buffer.Length) throw new ArgumentOutOfRangeException("index");
                int target = index + nextIndex;
                if (target >= buffer.Length) target -= buffer.Length;
                return buffer[target];
            }
            set {
                if (index < 0 || index >= buffer.Length) throw new ArgumentOutOfRangeException("index");
                int target = index + nextIndex;
                if (target >= buffer.Length) target -= buffer.Length;
                buffer[target] = value;
            }
        }

        public int Length => buffer.Length;

        /// <summary>
        /// Next value in the rolling buffer. Using this property doesn't change the next value.
        /// </summary>
        public T NextValue => buffer[nextIndex];

        /// <summary>
        /// Next value in the rolling buffer. Using this property doesn't change the next value.
        /// </summary>
        public T CurrentValue => buffer[(nextIndex > 0 ? nextIndex : buffer.Length) - 1];

        #endregion

        #region constructor

        public RollingBuffer(in int capacity, in bool overrideExisting) {

            if (capacity <= 0) throw new ArgumentOutOfRangeException("capacity must be greater than zero.");
            buffer = new T[capacity];
            nextIndex = 0;
            this.overrideExisting = overrideExisting;

        }

        #endregion

        #region logic

        #region Expand

        public void Expand(in int capacity) {
            int length = buffer.Length;
            if (capacity <= length) throw new ArgumentOutOfRangeException("capacity must be greater than original capacity.");
            int newLength = length + capacity;
            T[] newBuffer = new T[newLength];
            for (int i = 0; i < length; i++) newBuffer[i] = buffer[i];
            for (int i = length; i < newLength; i++) newBuffer[i] = default;
            buffer = newBuffer;
        }

        #endregion

        #region Push

        /// <summary>
        /// Pushes a value to the rolling buffer.
        /// </summary>
        /// <param name="value">Value to push into the buffer.</param>
        /// <param name="oldValue">Value that was overridden as a result of the push operation.</param>
        /// <returns>The index that the value was inserted at.</returns>
        public int Push(in T value, out T oldValue) {

            oldValue = buffer[nextIndex];
            if (oldValue != null && !overrideExisting) { // this entry cannot be overridden, skip until a null entry is found

                for (int i = nextIndex + 1; i < buffer.Length; i++) { // iterate to end of array

                    oldValue = buffer[i];
                    if (oldValue == null) { nextIndex = i; break; } // found a null entry

                }

                if (oldValue != null) {

                    for (int i = 0; i < nextIndex; i++) {

                        oldValue = buffer[i];
                        if (oldValue == null) { nextIndex = i; break; } // found a null entry

                    }

                    if (oldValue != null) // no null entry found
                        throw new BufferException("Buffer full and cannot override existing entries.");

                }

            }

            int insertedIndex = nextIndex;
            buffer[insertedIndex] = value;

            if (++nextIndex >= buffer.Length) nextIndex = 0; // move to next index

            return insertedIndex;

        }

        #endregion

        #region OverrideIndex
        /*
        /// <summary>
        /// Overrides a value in the <see cref="RollingBuffer{T}"/>.
        /// </summary>
        /// <param name="actualIndex">
        /// Actual index in the rolling buffer. This is not the same as the index when using the <see cref="this[in int]"/>
        /// property.
        /// </param>
        /// <param name="newValue">Override value.</param>
        public void OverrideIndex(in int actualIndex, in T newValue) {

            if (actualIndex < 0 || actualIndex >= buffer.Length) throw new ArgumentOutOfRangeException("actualIndex");
            buffer[actualIndex] = newValue;

        }
        */
        #endregion

        #region Replace

        /// <summary>
        /// Replaces a value with a new value.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="newValue">New value.</param>
        /// <returns>Total number of values that have been replaced.</returns>
        public int Replace(in T oldValue, in T newValue) {

            if (oldValue.Equals(newValue)) return 0; // no need to replace the values since they are both equal

            int count = 0;
            for (int i = 0; i < buffer.Length; i++) {
                if (oldValue.Equals(buffer[i])) {
                    buffer[i] = newValue;
                    count++;
                }
            }
            return count;

        }

        #endregion

        #region ToArray

        /// <summary>
        /// Converts the RollingBuffer to an array ordered by oldest entry first.
        /// </summary>
        public T[] ToArray() {

            T[] array = new T[buffer.Length];
            int index = 0;
            for (int i = nextIndex; i < buffer.Length; i++) array[index++] = buffer[i];
            for (int i = 0; i < nextIndex; i++) array[index++] = buffer[i];
            return array;

        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear() {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = default;
            nextIndex = 0;
        }

        /// <summary>
        /// Clears and resizes the buffer.
        /// </summary>
        public void Clear(in int newCapacity) {
            if (newCapacity < 1) throw new ArgumentOutOfRangeException(nameof(newCapacity));
            if (newCapacity == buffer.Length) Clear();
            else buffer = new T[newCapacity];
            nextIndex = 0;
        }

        #endregion

        #region ClearToArray

        /// <summary>
        /// Clears the rolling buffer.
        /// </summary>
        /// <returns>Array containing the contents of the rolling buffer ordered by oldest entry first.</returns>
        public T[] ClearToArray() {

            T[] array = new T[buffer.Length];
            int index = 0;
            for (int i = nextIndex; i < buffer.Length; i++) {
                array[index++] = buffer[i];
                buffer[i] = default;
            }
            for (int i = 0; i < nextIndex; i++) {
                array[index++] = buffer[i];
                buffer[i] = default;
            }
            return array;

        }

        #endregion

        #endregion

    }

}