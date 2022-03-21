using System;

using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace BlackTundra.Foundation.Utility {

    public static class UnityUtility {

        #region logic

        #region HasComponent

        public static bool HasComponent<T>(this GameObject gameObject) => gameObject.GetComponent<T>() != null;

        public static bool HasComponent<T>(this Component component) => component.GetComponent<T>() != null;

        #endregion

        #region ForceGetComponent

        /// <summary>
        /// Gets the existing instance of a component or creates one if no component exists.
        /// </summary>
        /// <typeparam name="T">Type of component to get.</typeparam>
        /// <param name="gameObject">GameObject to get the component from.</param>
        /// <returns>Returns the existing component or newly created component from the gameObject.</returns>
        public static T ForceGetComponent<T>(this GameObject gameObject) where T : Component {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Gets the existing instance of a component or creates one if no component exists.
        /// </summary>
        /// <typeparam name="T">Type of component to get.</typeparam>
        /// <param name="behaviour">MonoBehaviour to get the component from.</param>
        /// <returns>Returns the existing component or newly created component from the behaviour.</returns>
        public static T ForceGetComponent<T>(this MonoBehaviour behaviour) where T : Component {
            if (behaviour == null) throw new ArgumentNullException(nameof(behaviour));
            T component = behaviour.GetComponent<T>();
            return component != null ? component : behaviour.gameObject.AddComponent<T>();
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

        #region GetPoint

        public static Vector3 GetPoint(this Collision collision) {
            if (collision == null) throw new ArgumentNullException(nameof(collision));
            return GetPoint(collision.contacts);
        }

        public static Vector3 GetPoint(this ContactPoint[] contactPoints) {
            if (contactPoints == null) throw new ArgumentNullException(nameof(contactPoints));
            int pointCount = contactPoints.Length;
            if (pointCount == 0) return Vector3.zero;
            ContactPoint point = contactPoints[0];
            Vector3 total = point.point;
            for (int i = 1; i < pointCount; i++) {
                point = contactPoints[i];
                total += point.point;
            }
            return total * (1.0f / pointCount);
        }

        #endregion

        #region GetCollider

        public static Collider GetCollider(this GameObject gameObject) => gameObject != null ? gameObject.GetComponent<Collider>() : null;

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

        #region GetColliders

        public static Collider[] GetColliders(this GameObject gameObject, in bool includeTriggers) {
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            if (includeTriggers) return colliders;
            int colliderCount = colliders.Length;
            Collider[] colliderBuffer = new Collider[colliderCount];
            int index = 0;
            Collider collider;
            for (int i = colliderCount - 1; i >= 0; i--) {
                collider = colliders[i];
                if (!collider.isTrigger) colliderBuffer[index++] = collider;
            }
            colliders = new Collider[index];
            Array.Copy(colliderBuffer, 0, colliders, 0, index);
            return colliders;
        }

        #endregion

        #region GetTriggers

        public static Collider[] GetTriggers(this GameObject gameObject) {
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            int colliderCount = colliders.Length;
            Collider[] colliderBuffer = new Collider[colliderCount];
            int index = 0;
            Collider collider;
            for (int i = colliderCount - 1; i >= 0; i--) {
                collider = colliders[i];
                if (collider.isTrigger) colliderBuffer[index++] = collider;
            }
            colliders = new Collider[index];
            Array.Copy(colliderBuffer, 0, colliders, 0, index);
            return colliders;
        }

        #endregion


        #region CalculateBounds

        /// <summary>
        /// Gets the bounds of a <paramref name="gameObject"/> by combining the bounds of all colliders on the object (including child objects).
        /// </summary>
        public static Bounds CalculateBounds(this GameObject gameObject) {
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            Collider collider;
            Bounds bounds = new Bounds(gameObject.transform.position, Vector3.zero);
            for (int i = colliders.Length - 1; i >= 0; i--) {
                collider = colliders[i];
                bounds.Encapsulate(collider.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Calculates the bounds of multiple <paramref name="colliders"/>.
        /// </summary>
        public static Bounds CalculateBounds(this Collider[] colliders) {
            if (colliders == null) throw new ArgumentNullException(nameof(colliders));
            int length = colliders.Length;
            if (length == 0) return new Bounds();
            else {
                Collider collider = colliders[0];
                Bounds bounds = collider != null ? collider.bounds : new Bounds();
                for (int i = colliders.Length - 1; i > 0; i--) {
                    collider = colliders[i];
                    if (collider == null) continue;
                    bounds.Encapsulate(collider.bounds);
                }
                return bounds;
            }
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

        #region ToTexture2D

        /// <summary>
        /// Converts a <see cref="RenderTexture"/> to a <see cref="Texture2D"/>.
        /// </summary>
        public static Texture2D ToTexture2D(this RenderTexture renderTexture, in TextureFormat format = TextureFormat.RGB24) {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, format, renderTexture.mipmapCount, true);
            RenderTexture activeRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, true);
            texture.Apply();
            RenderTexture.active = activeRenderTexture;
            return texture;
        }

        #endregion

        #region SetMaterialAt

        /// <summary>
        /// Overrides a <see cref="Material"/> at a specified material <paramref name="index"/> for a <paramref name="renderer"/>.
        /// </summary>
        public static void SetMaterialAt(this Renderer renderer, in int index, in Material material) {
            Material[] originalMaterials = renderer.materials;
            int materialCount = originalMaterials.Length;
            Material[] materials = new Material[materialCount];
            for (int i = materialCount - 1; i >= 0; i--) {
                materials[i] = i == index ? material : originalMaterials[i];
            }
            renderer.materials = materials;
        }

        #endregion

        #region PlayRandomTime

        public static void PlayRandomTime(this AudioSource source) {
            if (source == null) return;
            AudioClip clip = source.clip;
            if (clip == null) return;
            source.time = clip.length * Random.value;
            source.Play();
        }

        #endregion

        #region PlayRandomTime

        public static void PlayRandomTime(this AudioSource source, in float minPitch, in float maxPitch) {
            if (source == null) return;
            AudioClip clip = source.clip;
            if (clip == null) return;
            source.time = clip.length * Random.value;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.Play();
        }

        #endregion

        #region PlayRandomPitch

        public static void PlayRandomPitch(this AudioSource source, in float minPitch, in float maxPitch) {
            if (source == null) return;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.Play();
        }

        #endregion

        #region SetRandomTime

        public static void SetRandomTime(this AudioSource source) {
            if (source == null) return;
            AudioClip clip = source.clip;
            if (clip == null) return;
            source.time = clip.length * Random.value;
        }

        #endregion

        #endregion

    }

}