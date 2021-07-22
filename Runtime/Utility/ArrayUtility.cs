using System;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Utility {

    public static class ArrayUtility {

        #region logic

        #region AddFirst

        public static T[] AddFirst<T>(this T[] original, in T obj) {

            if (original == null) throw new ArgumentNullException("original");

            int length = original.Length;
            T[] newArray = new T[length + 1];
            //for (int i = 0; i < length; i++) newArray[i + 1] = original[i];
            Array.Copy(original, 0, newArray, 1, length);
            newArray[0] = obj;
            return newArray;

        }

        #endregion

        #region AddLast

        public static T[] AddLast<T>(this T[] original, in T obj) {

            if (original == null) throw new ArgumentNullException("original");

            int length = original.Length;
            T[] newArray = new T[length + 1];
            //for (int i = 0; i < length; i++) newArray[i] = original[i];
            Array.Copy(original, 0, newArray, 0, length);
            newArray[length] = obj;
            return newArray;

        }

        #endregion

        #region AddAt

        public static T[] AddAt<T>(this T[] original, in int index, in T obj) {

            if (original == null) throw new ArgumentNullException("original");
            int length = original.Length;
            if (index < 0 || index > length) throw new ArgumentOutOfRangeException("index");

            T[] newArray = new T[length + 1];
            for (int i = 0; i < index; i++) newArray[i] = original[i];
            newArray[index] = obj;
            for (int i = index; i < length; i++) newArray[i + 1] = original[i];
            return newArray;

        }

        #endregion

        #region FindIndexOf

        /// <summary>
        /// Finds the index of the first occurance of an entry.
        /// </summary>
        /// <param name="array">Array to search inside.</param>
        /// <param name="entry">Entry to find.</param>
        /// <param name="startIndex">Index in the array to start searching from.</param>
        /// <returns>Index of the first occurance of the entry or -1 if no entry is found.</returns>
        public static int FindIndexOf<T>(this T[] array, in T entry, in int startIndex = 0) {
            if (array == null) throw new ArgumentNullException("array");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            return Array.IndexOf(array, entry, startIndex);
        }

        #endregion

        #region FindCountOf

        /// <summary>
        /// Finds the total number of occurances in the array.
        /// </summary>
        /// <param name="array">Array to search inside.</param>
        /// <param name="occurance">Occurance to look for inside the array.</param>
        /// <param name="startIndex">Index to start searching from.</param>
        /// <returns>Total number of occurances found, 0 if nothing is found.</returns>
        public static int FindCountOf<T>(this T[] array, in T occurance, in int startIndex = 0) where T : class {

            if (array == null) throw new ArgumentNullException("array");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");

            int length = array.Length; // get the length of the array
            if (length == 0 || startIndex >= length) return 0; // nothing to find

            int count = 0; // track the number of occurances
            for (int i = startIndex; i < length; i++) { if (array[i] == occurance) count++; } // count each occurance
            return count; // return the number of occurances

        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes an entry from an array.
        /// </summary>
        /// <param name="original">Array to remove the entry from.</param>
        /// <param name="entry">Entry to remove.</param>
        /// <param name="removeAll">If true, all references to the entry will be removed. Otherwise only the first occurance will be removed.</param>
        /// <returns>Array without the entry inside it.</returns>
        public static T[] Remove<T>(this T[] original, in T entry, in bool removeAll = false) where T : class {

            if (original == null) throw new ArgumentNullException("original");

            int length = original.Length; // get the length of the original array
            if (length == 0) return original; // no length therefore nothing to remove

            if (!removeAll) { // only remove the first occurence

                int index = original.FindIndexOf(entry);
                if (index == -1) return original; // no occurance found

                return original.RemoveAt(index, out _);

            } else { // remove all occurances

                int occurances = original.FindCountOf(entry); // find the total number of occurances of the entry
                if (occurances == 0) return original; // no occurances found, return the original array

                T[] newArray = new T[length - occurances]; // create a new array
                int index = 0; // track the next index to insert into the newArray at

                T temp; // temporary variable used for storing a reference
                for (int i = 0; i < length; i++) { // iterate through the original array

                    temp = original[i]; // assign temporary variable
                    if (temp != entry) newArray[index++] = temp; // not the entry to remove, add this to the new array

                }

                return newArray; // return the new array

            }

        }

        #endregion

        #region RemoveFirst

        public static T[] RemoveFirst<T>(this T[] original, out T entry) {

            if (original == null) throw new ArgumentNullException("original");

            int length = original.Length - 1;
            if (length == -1) {
                entry = default;
                return new T[0];
            }

            T[] newArray = new T[length];
            for (int i = 0; i < length;) newArray[i] = original[++i];

            entry = original[0];
            return newArray;

        }

        #endregion

        #region RemoveLast

        public static T[] RemoveLast<T>(this T[] original, out T entry) {

            if (original == null) throw new ArgumentNullException("original");

            int length = original.Length - 1;
            if (length == -1) {
                entry = default;
                return new T[0];
            }

            T[] newArray = new T[length];
            for (int i = 0; i < length; i++) newArray[i] = original[i];

            entry = original[length];
            return newArray;

        }

        #endregion

        #region RemoveAt

        public static T[] RemoveAt<T>(this T[] original, in int index, out T entry) {

            if (original == null) throw new ArgumentNullException("original");
            int length = original.Length;
            if (index < 0 || index >= length) throw new ArgumentOutOfRangeException("index");

            if (length == 0) {
                entry = default;
                return new T[0];
            }

            T[] newArray = new T[length - 1];
            for (int i = 0; i < index; i++) newArray[i] = original[i];
            for (int i = index; i < length - 1;) newArray[i] = original[++i];

            entry = original[index];
            return newArray;

        }

        #endregion

        #region Expand

        /// <summary>
        /// Expands an array by a specified amount.
        /// </summary>
        /// <param name="original">Original array.</param>
        /// <param name="size">Size to expand the array by.</param>
        /// <returns>Expanded array.</returns>
        public static T[] Expand<T>(this T[] original, in int size) {

            if (original == null) throw new ArgumentNullException("original");
            if (size < 0) throw new ArgumentOutOfRangeException("size");
            if (size == 0) return original;

            int length = original.Length;
            if (length == 0) return new T[size];

            int newLength = length + size;
            T[] newArray = new T[newLength];

            for (int i = 0; i < length; i++) newArray[i] = original[i];
            return newArray;

        }

        #endregion

        #region FirstNullIndex

        /// <summary>
        /// Finds the index of the first null reference in the array.
        /// </summary>
        /// <param name="array">Array to find the first null index in.</param>
        /// <returns>Index of the first null reference in the array or -1 if no null reference was found.</returns>
        public static int FirstNullIndex<T>(this T[] array) {

            if (array == null) throw new ArgumentNullException("array");

            for (int i = 0; i < array.Length; i++) { if (array[i] == null) return i; } // find first null reference
            return -1; // no null refernce found

        }

        #endregion

        #region Contains

        /// <summary>
        /// Checks if the array contains an entry.
        /// </summary>
        /// <param name="array">Array to search inside.</param>
        /// <param name="entry">Entry to search for.</param>
        /// <returns>Returns true if the entry was found in the array; otherwise returns false.</returns>
        public static bool Contains<T>(this T[] array, in T entry) {
            if (array == null) throw new ArgumentNullException("array");
            return Array.IndexOf(array, entry) != -1;
        }

        #endregion

        #region Sort

        public static void Sort<T>(this T[] array) => array.Sort(0, array.Length, null);

        public static void Sort<T>(this T[] array, in IComparer<T> comparer) => array.Sort(0, array.Length, comparer);

        public static void Sort<T>(this T[] array, in int index, in int count, IComparer<T> comparer) {

            if (index < 0) throw new ArgumentOutOfRangeException("index");
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            int length = array.Length;
            if (length - index < count) throw new ArgumentOutOfRangeException("count");

            Array.Sort(array, index, count, comparer);

        }

        public static void Sort<T>(this T[] array, Comparison<T> comparison) {

            if (comparison == null) throw new ArgumentNullException("comparison");
            Array.Sort(array, comparison);

        }

        #endregion

        #region Shuffle

        public static void Shuffle<T>(this T[] array, int iterations = -1) {

            if (array == null) throw new ArgumentNullException("array");

            if (iterations == 0) return;

            int arrayLength = array.Length;
            if (arrayLength <= 1) return;

            if (iterations < 0) iterations = arrayLength;

            int index;
            T temp;
            for (int i = 0; i < (iterations - 1); i++) {
                index = i + MathsUtility.Random.Next(iterations - i);
                temp = array[index];
                array[index] = array[i];
                array[i] = temp;
            }

        }

        #endregion

        #region Reverse

        public static void Reverse<T>(this T[] array) => Array.Reverse(array);

        #endregion

        #region ShiftRight

        public static void ShiftRight<T>(this T[] original) {

            if (original == null) throw new ArgumentNullException("original");

            int lastIndex = original.Length - 1;

            T lastValue = original[lastIndex];
            for (int i = lastIndex; i > 0; i--) original[i] = original[i - 1];
            original[0] = lastValue;

        }

        public static void ShiftRight<T>(this T[] original, in int distance) {

            if (original == null) throw new ArgumentNullException("original");
            if (distance < 0) { ShiftLeft(original, -distance); return; }
            if (distance == 0) return;

            int count = original.Length;
            if (distance > count) throw new ArgumentOutOfRangeException("distance");
            if (distance == count) return; // no offset required
            
            T[] lastValues = new T[distance]; // store the last values of the array
            Array.Copy(original, count - distance, lastValues, 0, distance);
            //for (int i = count - distance; i < count; i++) lastValues[i] = original[i]; // get the last values of the array
            for (int i = count - 1; i > distance; i--) original[i] = original[i - distance]; // move array along by distance
            Array.Copy(original, 0, lastValues, 0, distance);
            //for (int i = 0; i < distance; i++) original[i] = lastValues[i]; // copy across last values to start of array

        }

        #endregion

        #region ShiftLeft

        public static void ShiftLeft<T>(this T[] original) {

            if (original == null) throw new ArgumentNullException("original");

            int lastIndex = original.Length - 1;

            T firstValue = original[0];
            for (int i = 0; i < lastIndex; i++) original[i] = original[i + 1];
            original[lastIndex] = firstValue;

        }

        public static void ShiftLeft<T>(this T[] original, in int distance) {

            if (original == null) throw new ArgumentNullException("original");
            if (distance < 0) { ShiftRight(original, -distance); return; }
            if (distance == 0) return;

            int count = original.Length;
            if (distance > count) throw new ArgumentOutOfRangeException("distance");
            if (distance == count) return; // no offset required

            T[] lastValues = new T[distance];
            Array.Copy(original, 0, lastValues, 0, distance);
            for (int i = distance; i < count; i++) original[i - distance] = original[i];
            Array.Copy(lastValues, 0, original, count - distance, distance);

        }

        #endregion

        #region Swap

        /// <summary>
        /// Swaps two elements in an array.
        /// </summary>
        /// <param name="array">Array to swap the elements in.</param>
        /// <param name="i0">Unclamped source index. This will be clamped to the range of the array in the method.</param>
        /// <param name="i1">Unclamped target index. This will be clamped to the range of the array in the method.</param>
        public static void Swap<T>(this T[] array, int i0, int i1) {

            if (array == null) throw new ArgumentNullException("array");
            if (array.Length == 0 || i0 == i1) return;

            if (i0 < 0) i0 = 0; else if (i0 >= array.Length) i0 = array.Length - 1;
            if (i1 < 0) i1 = 0; else if (i1 >= array.Length) i1 = array.Length - 1;
            if (i0 == i1) return;

            T temp = array[i1];
            array[i1] = array[i0];
            array[i0] = temp;

        }

        #endregion

        #region ContentEquals

        /// <summary>
        /// Checks if the content of two arrays are equal.
        /// </summary>
        /// <typeparam name="T">Type of array.</typeparam>
        public static bool ContentEquals<T>(this T[] a1, in T[] a2) {

            if (ReferenceEquals(a1, a2)) return true;
            if (a1 == null || a2 == null || a1.Length != a2.Length) return false;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++) { if (!comparer.Equals(a1[i], a2[i])) return false; }
            return true;

        }

        #endregion

        #endregion

    }

}