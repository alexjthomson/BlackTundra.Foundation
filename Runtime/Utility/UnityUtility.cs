using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.Foundation.Utility {

    public static class UnityUtility {

        #region logic

        #region HasComponent

        public static bool HasComponent<T>(this GameObject gameObject) => gameObject.GetComponent<T>() != null;

        #endregion

        #region ForceGetComponent

        /// <summary>
        /// Gets the existing instance of a component or creates one if no component exists.
        /// </summary>
        /// <typeparam name="T">Type of component to get.</typeparam>
        /// <param name="gameObject">GameObject to get the component from.</param>
        /// <returns>Returns the existing component or newly created component from the gameObject.</returns>
        public static T ForceGetComponent<T>(this GameObject gameObject) where T : Component {

            if (gameObject == null) throw new ArgumentNullException("gameObject");

            T component = gameObject.GetComponent<T>();
            return component ?? gameObject.AddComponent<T>();

        }

        /// <summary>
        /// Gets the existing instance of a component or creates one if no component exists.
        /// </summary>
        /// <typeparam name="T">Type of component to get.</typeparam>
        /// <param name="behaviour">MonoBehaviour to get the component from.</param>
        /// <returns>Returns the existing component or newly created component from the behaviour.</returns>
        public static T ForceGetComponent<T>(this MonoBehaviour behaviour) where T : Component {

            if (behaviour == null) throw new ArgumentNullException("behaviour");

            T component = behaviour.GetComponent<T>();
            return component ?? behaviour.gameObject.AddComponent<T>();

        }

        #endregion

        #region AlignTo

        public static void AlignTo(this Transform transform, in Transform target) {

            if (transform == null) throw new ArgumentNullException("transform");
            AlignTo(transform, target, transform.forward);

        }

        public static void AlignTo(this Transform transform, in Transform target, in Vector3 forward) {

            if (transform == null) throw new ArgumentNullException("transform");
            if (target == null) throw new ArgumentNullException("target");
            transform.rotation = Quaternion.LookRotation(forward, target.up);

        }

        #endregion

        #region ContainsLayer

        public static bool ContainsLayer(this LayerMask layerMask, in int layer) => (layerMask & (1 << layer)) != 0;

        #endregion

        #region GetCollider

        public static Collider GetCollider(this GameObject gameObject) => gameObject?.GetComponent<Collider>();

        public static Collider GetCollider(this GameObject gameObject, in LayerMask layerMask) {

            if (gameObject == null) return null;
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0) {
                Collider collider;
                for (int i = 0; i < colliders.Length; i++) {
                    collider = colliders[i];
                    if (layerMask.ContainsLayer(collider.gameObject.layer)) return collider;
                }
            }
            return null;

        }

        #endregion

        #region ToGameObject

        /// <summary>
        /// Attempts to get the <see cref="GameObject"/> associated with the object.
        /// </summary>
        /// <param name="obj">Object to find the <see cref="GameObject"/> for.</param>
        /// <returns>
        /// Returns a reference to the <see cref="GameObject"/> associated with the
        /// <see cref="object"/>, otherwise <c>null</c> is returned.
        /// </returns>
        public static GameObject ToGameObject(this object obj) {
            if (obj == null) return null;
            if (obj is GameObject gameObject) return gameObject;
            else if (obj is Component component) return component.gameObject;
            return null;
        }

        #endregion

        #region SetComponentStates

        public static void SetBehaviourStates<T>(this GameObject gameObject, in bool enabled) where T : MonoBehaviour {
            if (gameObject == null) throw new ArgumentNullException("gameObject");
            T[] components = gameObject.GetComponentsInChildren<T>();
            for (int i = 0; i < components.Length; i++) components[i].enabled = enabled;
        }

        #endregion

        #region SetRendererStates

        public static void SetRendererStates(this GameObject gameObject, in bool enabled) {
            if (gameObject == null) throw new ArgumentNullException("gameObject");
            Renderer[] components = gameObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < components.Length; i++) components[i].enabled = enabled;
        }

        #endregion

        #region SetColliderStates

        public static void SetColliderStates(this GameObject gameObject, in bool enabled) {
            if (gameObject == null) throw new ArgumentNullException("gameObject");
            Collider[] components = gameObject.GetComponentsInChildren<Collider>();
            for (int i = 0; i < components.Length; i++) components[i].enabled = enabled;
        }

        #endregion

        #region ManageSingleton

        /// <summary>
        /// Manages a singleton reference.
        /// </summary>
        /// <typeparam name="T">Type of object being stored as a singleton reference.</typeparam>
        /// <param name="instance">Instance to update the <paramref name="singletonReference"/> with.</param>
        /// <param name="singletonReference">Reference to the primary singleton instance.</param>
        /// <param name="allowInstanceOverride">When <c>true</c>, the <paramref name="singletonReference"/> can be overridden if it holds a reference to a singleton instance.</param>
        /// <returns>Returns the previous <paramref name="singletonReference"/> value.</returns>
        /// <remarks>
        /// An <see cref="InvalidOperationException"/> will be thrown if the <paramref name="singletonReference"/> cannot be overriden for any reason.
        /// </remarks>
        public static T ManageSingleton<T>(this T instance, ref T singletonReference, in bool allowInstanceOverride = false) where T : class {

            if (instance == singletonReference) return singletonReference; // reference already set
            if (singletonReference != null) {
                if (allowInstanceOverride) {
                    T previousValue = singletonReference;
                    singletonReference = instance;
                    return previousValue;
                } else throw new InvalidOperationException("Singleton instance cannot be overridden.");
            } else {
                singletonReference = instance;
                return null;
            }

        }

        #endregion

        #region ManageObjectSingleton

        /// <summary>
        /// Manages an <see cref="Object"/> singleton reference.
        /// This method automates destruction of old singleton references.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Object"/> being stored as a singleton reference.</typeparam>
        /// <param name="instance">Instance to update the <paramref name="singletonReference"/> with.</param>
        /// <param name="singletonReference">Reference to the primary singleton instance.</param>
        /// <param name="allowInstanceOverride">When <c>true</c>, the <paramref name="singletonReference"/> can be overridden if it holds a reference to a singleton instance.</param>
        /// <returns>Returns the previous <paramref name="singletonReference"/> value.</returns>
        /// <remarks>
        /// An <see cref="InvalidOperationException"/> will be thrown if the <paramref name="singletonReference"/> cannot be overriden for any reason.
        /// </remarks>
        public static T ManageObjectSingleton<T>(this T instance, ref T singletonReference, in bool allowInstanceOverride = false) where T : Object {

            if (instance == singletonReference) return singletonReference;
            if (singletonReference != null) {
                if (allowInstanceOverride) {
                    T previousValue = singletonReference;
                    Object.Destroy(previousValue);
                    singletonReference = instance;
                    return previousValue;
                } else {
                    throw new InvalidOperationException("Singleton instance cannot be overridden.");
                }
            } else {
                singletonReference = instance;
                return null;
            }

        }

        #endregion

        #endregion

    }

}