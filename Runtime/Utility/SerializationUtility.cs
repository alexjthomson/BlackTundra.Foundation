using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace BlackTundra.Foundation.Utility {

    #region SerializableVector2

    [Serializable]
    [SerializableImplementationOf(typeof(Vector2))]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct SerializableVector2 {

        #region variable

        [FieldOffset(0)]
        public float x;

        [FieldOffset(4)]
        public float y;

        #endregion

        #region constructor

        public SerializableVector2(Vector2 v) {
            x = v.x;
            y = v.y;
        }

        public SerializableVector2(in float x, in float y) {
            this.x = x;
            this.y = y;
        }

        #endregion

        #region logic

        public static explicit operator Vector2(in SerializableVector2 v) => new Vector2(v.x, v.y);
        public static explicit operator Vector3(in SerializableVector2 v) => new Vector3(v.x, v.y, 0.0f);
        public static explicit operator Vector4(in SerializableVector2 v) => new Vector4(v.x, v.y, 0.0f, 0.0f);

        public static SerializableVector2 operator +(in SerializableVector2 left, in SerializableVector2 right) => new SerializableVector2(
            left.x + right.x,
            left.y + right.y
        );

        public static SerializableVector2 operator -(in SerializableVector2 left, in SerializableVector2 right) => new SerializableVector2(
            left.x - right.x,
            left.y - right.y
        );

        public static SerializableVector2 operator *(in SerializableVector2 left, in float right) => new SerializableVector2(
            left.x * right,
            left.y * right
        );

        #endregion

    }

    #endregion

    #region SerializableVector3

    [Serializable]
    [SerializableImplementationOf(typeof(Vector3))]
    [StructLayout(LayoutKind.Explicit, Size = 12, Pack = 1)]
    public struct SerializableVector3 {

        #region variable

        [FieldOffset(0)]
        public float x;

        [FieldOffset(4)]
        public float y;

        [FieldOffset(8)]
        public float z;

        #endregion

        #region constructor

        public SerializableVector3(in Vector2 v) {
            x = v.x;
            y = v.y;
            z = 0.0f;
        }

        public SerializableVector3(Vector3 v) {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public SerializableVector3(in float x, in float y) {
            this.x = x;
            this.y = y;
            z = 0.0f;
        }

        public SerializableVector3(in float x, in float y, in float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #endregion

        #region logic

        public static explicit operator Vector2(in SerializableVector3 v) => new Vector2(v.x, v.y);
        public static explicit operator Vector3(in SerializableVector3 v) => new Vector3(v.x, v.y, v.z);
        public static explicit operator Vector4(in SerializableVector3 v) => new Vector4(v.x, v.y, v.z, 0.0f);

        public static SerializableVector3 operator +(in SerializableVector3 left, in SerializableVector3 right) => new SerializableVector3(
            left.x + right.x,
            left.y + right.y,
            left.z + right.z
        );

        public static SerializableVector3 operator -(in SerializableVector3 left, in SerializableVector3 right) => new SerializableVector3(
            left.x - right.x,
            left.y - right.y,
            left.z - right.z
        );

        public static SerializableVector3 operator *(in SerializableVector3 left, in float right) => new SerializableVector3(
            left.x * right,
            left.y * right,
            left.z * right
        );

        #endregion

    }

    #endregion

    #region SerializableVector4

    [Serializable]
    [SerializableImplementationOf(typeof(Vector4))]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
    public struct SerializableVector4 {

        #region variable

        [FieldOffset(0)]
        public float x;

        [FieldOffset(4)]
        public float y;

        [FieldOffset(8)]
        public float z;

        [FieldOffset(12)]
        public float w;

        #endregion

        #region constructor

        public SerializableVector4(in Vector2 v) {
            x = v.x;
            y = v.y;
            z = 0.0f;
            w = 0.0f;
        }

        public SerializableVector4(in Vector3 v) {
            x = v.x;
            y = v.y;
            z = v.z;
            w = 0.0f;
        }

        public SerializableVector4(Vector4 v) {
            x = v.x;
            y = v.y;
            z = v.z;
            w = v.w;
        }

        public SerializableVector4(in float x, in float y) {
            this.x = x;
            this.y = y;
            z = 0.0f;
            w = 0.0f;
        }

        public SerializableVector4(in float x, in float y, in float z) {
            this.x = x;
            this.y = y;
            this.z = z;
            w = 0.0f;
        }

        public SerializableVector4(in float x, in float y, in float z, in float w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        #endregion

        #region logic

        public static explicit operator Vector2(in SerializableVector4 v) => new Vector2(v.x, v.y);
        public static explicit operator Vector3(in SerializableVector4 v) => new Vector3(v.x, v.y, v.z);
        public static explicit operator Vector4(in SerializableVector4 v) => new Vector4(v.x, v.y, v.z, v.w);

        public static SerializableVector4 operator +(in SerializableVector4 left, in SerializableVector4 right) => new SerializableVector4(
            left.x + right.x,
            left.y + right.y,
            left.z + right.z,
            left.w + right.w
        );

        public static SerializableVector4 operator -(in SerializableVector4 left, in SerializableVector4 right) => new SerializableVector4(
            left.x - right.x,
            left.y - right.y,
            left.z - right.z,
            left.w - right.w
        );

        public static SerializableVector4 operator *(in SerializableVector4 left, in float right) => new SerializableVector4(
            left.x * right,
            left.y * right,
            left.z * right,
            left.w * right
        );

        #endregion

    }

    #endregion

    #region SerializableQuanternion

    [Serializable]
    [SerializableImplementationOf(typeof(Quaternion))]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
    public struct SerializableQuaternion {

        #region variable

        [FieldOffset(0)]
        public float x;

        [FieldOffset(4)]
        public float y;

        [FieldOffset(8)]
        public float z;

        [FieldOffset(12)]
        public float w;

        #endregion

        #region constructor

        public SerializableQuaternion(Quaternion q) {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public SerializableQuaternion(in float x, in float y, in float z, in float w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        #endregion

        #region logic

        public static explicit operator Quaternion(in SerializableQuaternion q) => new Quaternion(q.x, q.y, q.z, q.w);
        public static explicit operator Vector4(in SerializableQuaternion q) => new Vector4(q.x, q.y, q.z, q.w);

        #endregion

    }

    #endregion

}