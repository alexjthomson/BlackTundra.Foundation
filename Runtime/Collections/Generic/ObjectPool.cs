using System;
using System.Collections.Concurrent;

namespace BlackTundra.Foundation.Collections.Generic {

    /// <summary>
    /// Class used for handing an object pool of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of object to pool.</typeparam>
    public sealed class ObjectPool<T> where T : class {

        #region delegate

        /// <summary>
        /// Delegate used for creating a poolable object.
        /// </summary>
        /// <param name="index">Index of the poolable object in the <see cref="ObjectPool{T}"/>.</param>
        /// <returns>Returns a reference to the newly created object.</returns>
        public delegate T CreatePoolObjectDelegate(in int index);

        /// <summary>
        /// Delegate used for returning an object to the <see cref="ObjectPool{T}"/>.
        /// </summary>
        /// <param name="obj">Reference to the object to return to the <see cref="ObjectPool{T}"/>.</param>
        /// <param name="index">Index of the object to return to the <see cref="ObjectPool{T}"/>.</param>
        public delegate void ReturnToPoolDelegate(in T obj);

        /// <summary>
        /// Delegate used for setting up an object that has left the <see cref="ObjectPool{T}"/>. This is not the
        /// same as the object being disassociated with the object pool. This simply means the object has been
        /// requested and is now an active object in the object pool.
        /// </summary>
        /// <param name="obj">Reference to the object in the <see cref="ObjectPool{T}"/>.</param>
        /// <param name="index">Index of the object in the <see cref="ObjectPool{T}"/>.</param>
        public delegate void RemoveFromPoolCallback(in T obj);

        #endregion

        #region variable

        /// <summary>
        /// Collection that tracks available objects in the pool.
        /// </summary>
        private readonly ConcurrentBag<T> availableObjects;

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> that contains every object in the <see cref="ObjectPool{T}"/>.
        /// </summary>
        private readonly PackedBuffer<T> objectBuffer;

        /// <summary>
        /// Size to expand the internal object pool by when additional space is required.
        /// </summary>
        public readonly int expandSize;

        /// <summary>
        /// Maximum capacity of the internal object pool.
        /// </summary>
        public readonly int capacity;

        /// <inheritdoc cref="CreatePoolObjectDelegate"/>
        private readonly CreatePoolObjectDelegate createPoolObjectCallback;

        /// <inheritdoc cref="ReturnToPoolDelegate"/>
        private readonly ReturnToPoolDelegate returnToPoolCallback;

        /// <inheritdoc cref="RemoveFromPoolCallback"/>
        private readonly RemoveFromPoolCallback removeFromPoolCallback;

        #endregion

        #region property

        /// <summary>
        /// Number of objects in the internal object pool.
        /// </summary>
        public int ObjectCount => objectBuffer.Count;

        /// <summary>
        /// Number of active objects in the object pool.
        /// </summary>
        public int ActiveObjectCount => objectBuffer.Count - availableObjects.Count;

        /// <summary>
        /// Number of available objects in the object pool.
        /// </summary>
        public int AvailableObjectCount => availableObjects.Count;

        /// <summary>
        /// Remaining space in the internal object pool before the pool reaches its maximum <see cref="capacity"/>.
        /// </summary>
        public int RemainingSpace => capacity - objectBuffer.Count;

        /// <returns>
        /// Returns the object in the internal object pool at the requested <paramref name="index"/>.
        /// </returns>
        public T this[in int index] => objectBuffer[index];

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a new <see cref="ObjectPool{T}"/>.
        /// </summary>
        /// <param name="initialCapacity">
        /// Initial capacity of objects in the pool.
        /// </param>
        /// <param name="maximumCapacity">
        /// Maximum capacity of objects in the pool.
        /// </param>
        /// <param name="expandSize">
        /// Size to expand the object pool buffer by when space is required.
        /// </param>
        /// <param name="initialObjectCount">
        /// Initial number of objects add to the pool.
        /// </param>
        /// <param name="createPoolObjectCallback">
        /// Callback invoked when a new poolable object of type <typeparamref name="T"/> should be created.
        /// </param>
        /// <param name="returnToPoolCallback">
        /// Callback invoked to set the object back up to re-enter the pool after being used.
        /// </param>
        /// <param name="exitPoolCallback">
        /// Callback invoked when an object is removed from the pool because it has been requested to be used.
        /// </param>
        public ObjectPool(
            in int initialCapacity,
            in int maximumCapacity,
            in int expandSize,
            in int initialObjectCount,
            in CreatePoolObjectDelegate createPoolObjectCallback,
            in ReturnToPoolDelegate returnToPoolCallback = null,
            in RemoveFromPoolCallback removeFromPoolCallback = null
        ) {
            if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            if (maximumCapacity < initialCapacity || maximumCapacity == 0) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
            if (expandSize <= 0) throw new ArgumentOutOfRangeException(nameof(expandSize));
            if (initialObjectCount < 0 || initialObjectCount > maximumCapacity) throw new ArgumentOutOfRangeException(nameof(initialObjectCount));
            if (createPoolObjectCallback == null) throw new ArgumentNullException(nameof(createPoolObjectCallback));
            availableObjects = new ConcurrentBag<T>();
            this.expandSize = expandSize;
            capacity = maximumCapacity;
            this.createPoolObjectCallback = createPoolObjectCallback;
            this.returnToPoolCallback = returnToPoolCallback;
            this.removeFromPoolCallback = removeFromPoolCallback;
            if (initialObjectCount > 0) {
                int initialPoolSize = initialObjectCount > initialCapacity ? initialObjectCount : initialCapacity;
                T[] buffer = new T[initialPoolSize];
                T obj;
                for (int i = 0; i < initialObjectCount; i++) {
                    obj = createPoolObjectCallback(i);
                    if (obj == null) throw new NullReferenceException();
                    buffer[i] = obj;
                }
                objectBuffer = new PackedBuffer<T>(buffer);
            } else {
                objectBuffer = new PackedBuffer<T>(initialCapacity);
            }
        }

        #endregion

        #region logic

        #region GetObject

        /// <summary>
        /// Gets an available object and removes it from the object pool so it can be used and later
        /// returned to the object pool.
        /// </summary>
        /// <returns>Returns the available poolable object.</returns>
        /// <seealso cref="TryGetObject(out T)"/>
        public T GetObject() {
            if (availableObjects.TryTake(out T obj)) {
                removeFromPoolCallback?.Invoke(obj);
                return obj;
            } else { // failed to take object from available objects
                int currentCount = objectBuffer.Count;
                obj = createPoolObjectCallback(currentCount); // create a new poolable object
                if (obj == null) throw new NullReferenceException(); // created object is null
                if (objectBuffer.IsFull) { // the object buffer is full, therefore currentCount is equal to the current capacity of the buffer
                    if (currentCount >= capacity) throw new OutOfMemoryException(); // pool reached capacity
                    int newCapacity = currentCount + expandSize;
                    if (newCapacity > capacity) // new capacity is greater than the maximum capacity
                        objectBuffer.Expand(newCapacity - capacity); // expand by only the space left
                    else // new capacity is not greater than the maximum capacity
                        objectBuffer.Expand(expandSize);
                }
                objectBuffer.AddLast(obj);
                if (returnToPoolCallback != null) {
                    try {
                        returnToPoolCallback.Invoke(obj);
                    } finally {
                        availableObjects.Add(obj);
                    }
                } else {
                    availableObjects.Add(obj);
                }
                return obj;
            }
        }

        #endregion

        #region TryGetObject

        /// <summary>
        /// Attempts to get an available object and remove it from the object pool so it can be used and
        /// later returned to the object pool.
        /// </summary>
        /// <param name="obj">
        /// Available object removed from the object pool or <c>null</c> if the operation was not successful.
        /// </param>
        /// <returns>Returns <c>true</c> if the operation was successful.</returns>
        public bool TryGetObject(out T obj) {
            try {
                obj = GetObject();
            } catch (Exception) {
                obj = null;
                return false;
            }
            return true;
        }

        #endregion

        #region ReturnToPool

        /// <summary>
        /// Returns an object from the object pool.
        /// </summary>
        public void ReturnToPool(in T obj) {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (objectBuffer[obj] == -1) throw new ArgumentException(string.Concat(nameof(obj), " was not created via this object pool instance."));
            if (returnToPoolCallback != null) {
                try {
                    returnToPoolCallback.Invoke(obj);
                } finally {
                    availableObjects.Add(obj);
                }
            } else {
                availableObjects.Add(obj);
            }
        }

        #endregion

        #endregion

    }

}