using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to camera offset (position, yaw, uniform scale)
    /// </summary>
    public interface IUsesCameraOffset : IFunctionalitySubscriber<IProvidesCameraOffset>
    {
    }

    public static class IUsesCameraOffsetMethods
    {
        /// <summary>
        /// Get the camera position offset
        /// </summary>
        /// <returns>The camera position offset</returns>
        public static Vector3 GetCameraPositionOffset(this IUsesCameraOffset obj)
        {
#if !FI_AUTOFILL
            return obj.provider.cameraPositionOffset;
#else
            return default;
#endif
        }

        /// <summary>
        /// Set the camera position offset
        /// </summary>
        /// <param name="offset">The camera position offset</param>
        public static void SetCameraPositionOffset(this IUsesCameraOffset obj, Vector3 offset)
        {
#if !FI_AUTOFILL
            obj.provider.cameraPositionOffset = offset;
#endif
        }

        /// <summary>
        /// Get the camera yaw offset
        /// </summary>
        /// <returns>The camera yaw offset</returns>
        public static float GetCameraYawOffset(this IUsesCameraOffset obj)
        {
#if !FI_AUTOFILL
            return obj.provider.cameraYawOffset;
#else
            return default;
#endif
        }

        /// <summary>
        /// Set the camera yaw offset
        /// </summary>
        /// <param name="offset">The yaw offset</param>
        public static void SetCameraYawOffset(this IUsesCameraOffset obj, float offset)
        {
#if !FI_AUTOFILL
            obj.provider.cameraYawOffset = offset;
#endif
        }

        /// <summary>
        /// Get the camera scale
        /// </summary>
        /// <returns>The camera scale</returns>
        public static float GetCameraScale(this IUsesCameraOffset obj)
        {
#if !FI_AUTOFILL
            return obj.provider.cameraScale;
#else
            return default;
#endif
        }

        /// <summary>
        /// Set the camera scale
        /// </summary>
        /// <param name="scale">The camera scale</param>
        public static void SetCameraScale(this IUsesCameraOffset obj, float scale)
        {
#if !FI_AUTOFILL
            obj.provider.cameraScale = scale;
#endif
        }

        /// <summary>
        /// Get the matrix that applies the camera offset transformation
        /// </summary>
        /// <returns>The camera offset matrix</returns>
        public static Matrix4x4 GetCameraOffsetMatrix(this IUsesCameraOffset obj)
        {
#if !FI_AUTOFILL
            return obj.provider.CameraOffsetMatrix;
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the camera offset to a pose and return the modified pose
        /// </summary>
        /// <param name="pose">The pose to which the offset will be applied</param>
        /// <returns>The modified pose</returns>
        public static Pose ApplyOffsetToPose(this IUsesCameraOffset obj, Pose pose)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyOffsetToPose(pose);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a pose and return the modified pose
        /// </summary>
        /// <param name="pose">The pose to which the offset will be applied</param>
        /// <returns>The modified pose</returns>
        public static Pose ApplyInverseOffsetToPose(this IUsesCameraOffset obj, Pose pose)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyInverseOffsetToPose(pose);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the camera offset to a position and return the modified position
        /// </summary>
        /// <param name="position">The position to which the offset will be applied</param>
        /// <returns>The modified position</returns>
        public static Vector3 ApplyOffsetToPosition(this IUsesCameraOffset obj, Vector3 position)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyOffsetToPosition(position);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a pose and return the modified pose
        /// </summary>
        /// <param name="pose">The pose to which the offset will be applied</param>
        /// <returns>The modified pose</returns>
        public static Vector3 ApplyInverseOffsetToPosition(this IUsesCameraOffset obj, Vector3 position)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyInverseOffsetToPosition(position);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the camera offset to a direction and return the modified direction. This is not affected by scale or position.
        /// </summary>
        /// <param name="direction">The direction to which the offset will be applied</param>
        /// <returns>The modified direction</returns>
        public static Vector3 ApplyOffsetToDirection(this IUsesCameraOffset obj, Vector3 direction)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyOffsetToDirection(direction);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a direction and return the modified direction.
        /// This is not affected by scale or position.
        /// </summary>
        /// <param name="direction">The direction to which the offset will be applied</param>
        /// <returns>The modified direction</returns>
        public static Vector3 ApplyInverseOffsetToDirection(this IUsesCameraOffset obj, Vector3 direction)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyInverseOffsetToDirection(direction);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the camera offset to a rotation and return the modified rotation
        /// </summary>
        /// <param name="rotation">The rotation to which the offset will be applied</param>
        /// <returns>The modified rotation</returns>
        public static Quaternion ApplyOffsetToRotation(this IUsesCameraOffset obj, Quaternion rotation)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyOffsetToRotation(rotation);
#else
            return default;
#endif
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a rotation and return the modified rotation
        /// </summary>
        /// <param name="rotation">The rotation to which the offset will be applied</param>
        /// <returns>The modified rotation</returns>
        public static Quaternion ApplyInverseOffsetToRotation(this IUsesCameraOffset obj, Quaternion rotation)
        {
#if !FI_AUTOFILL
            return obj.provider.ApplyInverseOffsetToRotation(rotation);
#else
            return default;
#endif
        }
    }
}
