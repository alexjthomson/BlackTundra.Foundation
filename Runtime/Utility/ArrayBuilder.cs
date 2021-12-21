using System;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Utility {

    /// <summary>
    /// Utility used to build an array when you don't know the number of elements expected to be in the array.
    /// </summary>
    /// <typeparam name="T">Type of array to build.</typeparam>
    public sealed class ArrayBuilder<T> {

        #region variable

        /// <summary>
        /// LinkedList used to build the array.
        /// </summary>
        private readonly LinkedList<T> linkedList;

        /// <summary>
        /// Array being constructed.
        /// </summary>
        private T[] array;

        /// <summary>
        /// Stores if the ArrayBuilder has been modified since the array was calculated.
        /// </summary>
        private bool modified;

        #endregion

        #region property

        public T this[in int index] {

            get {

                if (index < 0) throw new IndexOutOfRangeException("index");
                if (modified || array == null) BuildArray();
                if (index >= array.Length) throw new IndexOutOfRangeException("index");
                return array[index];

            }

        }

        public int Length => linkedList.Count;

#pragma warning disable IDE1006 // naming styles

        /// <summary>
        /// First element in the array.
        /// </summary>
        public T first {

            get {

                LinkedListNode<T> node = linkedList.First;
                return node == null ? default : node.Value;

            }

        }

        /// <summary>
        /// Last element in the array.
        /// </summary>
        public T last {

            get {

                LinkedListNode<T> node = linkedList.Last;
                return node == null ? default : node.Value;

            }

        }

#pragma warning restore IDE1006 // naming styles

        #endregion

        #region constructor

        public ArrayBuilder() {
            linkedList = new LinkedList<T>();
            array = null;
            modified = true;
        }

        #endregion

        #region logic

        #region AddFirst

        public void AddFirst(in T element) {

            linkedList.AddFirst(element);
            modified = true;

        }

        #endregion

        #region AddLast

        public void AddLast(in T element) {

            linkedList.AddLast(element);
            modified = true;

        }

        #endregion

        #region RemoveFirst

        public void RemoveFirst() {

            linkedList.RemoveFirst();
            modified = true;

        }

        #endregion

        #region RemoveLast

        public void RemoveLast() {

            linkedList.RemoveLast();
            modified = true;

        }

        #endregion

        #region Clear

        public void Clear() {

            linkedList.Clear();
            modified = true;

        }

        #endregion

        #region BuildArray

        /// <summary>
        /// Builds an array based off of the linkedList variable.
        /// </summary>
        private void BuildArray() {

            int length = linkedList.Count;
            array = new T[length];
            if (length == 0) return; // nothing more required

            LinkedListNode<T> node = linkedList.First; // get the first node in the linked list
            for (int i = 0; i < length; i++) { // iterate through the array

                array[i] = node.Value; // assign a value to this point in the array
                node = node.Next; // move to the next element in the linked list

            }

            modified = false; // unset modified flag

        }

        #endregion

        #region ToArray

        public T[] ToArray() {
            if (modified) BuildArray();
            return array;
        }

        #endregion

        #endregion

    }

}