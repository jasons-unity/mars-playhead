using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class HandleUtils
    {
        const float k_ConstantHandleSizeFactor = 0.2f;
        const float k_WorldSpaceHandleScale = 0.2f;
        const float k_WorldSpaceCircleHandleSize = 0.3f;
        const float k_HandleSnapScalar = 0.1f;
        const float k_LineWidth = 5f;
        const float k_DottedLineScreenSpaceWidth = 2f;
        static readonly Vector3 k_HandleSnap = Vector3.one * k_HandleSnapScalar;

        /// <summary>
        /// Draw a wire-frame rectangle on the XZ plane in the space of Handles.matrix.
        /// </summary>
        /// <param name="center">The center of the rectangle in the space of Handles.matrix</param>
        /// <param name="extents">The width and height of the rectangle.</param>
        public static void DrawXZPlaneRect(Vector3 center, Vector2 extents)
        {
            var frontLeftCorner = new Vector3(center.x - extents.x, center.y, center.z + extents.y);
            var backLeftCorner = new Vector3(center.x - extents.x, center.y, center.z - extents.y);
            var frontRightCorner = new Vector3(center.x + extents.x, center.y, center.z + extents.y);
            var backRightCorner = new Vector3(center.x + extents.x, center.y, center.z - extents.y);
            Handles.DrawLine(frontLeftCorner, backLeftCorner);
            Handles.DrawLine(backLeftCorner, backRightCorner);
            Handles.DrawLine(backRightCorner, frontRightCorner);
            Handles.DrawLine(frontRightCorner, frontLeftCorner);
        }

        /// <summary>
        /// Draw an unconstrained movement handle.
        /// </summary>
        /// <param name="position">The position of the handle in the space of Handles.matrix.</param>
        /// <returns>The new value modified by the user's interaction with the handle.</returns>
        public static Vector3 FreeMoveHandle(Vector3 position)
        {
            return Handles.FreeMoveHandle(
                position, Quaternion.identity, HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor,
                k_HandleSnap, Handles.SphereHandleCap);
        }

        /// <summary>
        /// Draw a 3D slider that moves along one axis.
        /// </summary>
        /// <param name="position">The position of the current point in the space of Handles.matrix.</param>
        /// <param name="direction">The direction axis of the slider in the space of Handles.matrix.</param>
        /// <returns>The new value modified by the user's interaction with the handle. If the user has not moved the handle, it will return the position value passed into the function.</returns>
        public static Vector3 Slider(Vector3 position, Vector3 direction)
        {
            return Handles.Slider(
                position, direction, HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor,
                Handles.SphereHandleCap, k_HandleSnapScalar);
        }

        /// <summary>
        /// Draw a 3D slider that moves along two axes.
        /// </summary>
        /// <param name="position">The position of the current point in the space of Handles.matrix.</param>
        /// <param name="slideDirection1">The first axis of the slider's plane of movement in the space of Handles.matrix.</param>
        /// <param name="sliderDirection2">The second axis of the slider's plane of movement in the space of Handles.matrix.</param>
        /// <param name="id">A control id to be used for the slider instead of allowing it to be auto generated. </param>
        /// <returns>The new value modified by the user's interaction with the handle. If the user has not moved the handle, it will return the position value passed into the function.</returns>
        public static Vector3 Slider2D(Vector3 position, Vector3 slideDirection1, Vector3 sliderDirection2, int id)
        {
            return Handles.Slider2D(
                id, position, Vector3.Cross(slideDirection1, sliderDirection2), slideDirection1, sliderDirection2,
                HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor, Handles.SphereHandleCap, k_HandleSnap);
        }

        /// <summary>
        /// Draw a 3D slider that moves along two axes.
        /// </summary>
        /// <param name="position">The position of the current point in the space of Handles.matrix.</param>
        /// <param name="slideDirection1">The first axis of the slider's plane of movement in the space of Handles.matrix.</param>
        /// <param name="sliderDirection2">The second axis of the slider's plane of movement in the space of Handles.matrix.</param>
        /// <returns>The new value modified by the user's interaction with the handle. If the user has not moved the handle, it will return the position value passed into the function.</returns>
        public static Vector3 Slider2D(Vector3 position, Vector3 slideDirection1, Vector3 sliderDirection2)
        {
            return Handles.Slider2D(
                position, Vector3.Cross(slideDirection1, sliderDirection2), slideDirection1, sliderDirection2,
                HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor, Handles.SphereHandleCap, k_HandleSnap);
        }

        /// <summary>
        /// Make a text label positioned in 3D space.
        /// </summary>
        /// <param name="position">Position in 3D space as seen from the current handle camera.</param>
        /// <param name="text">Text to display on the label.</param>
        public static void Label(Vector3 position, string text)
        {
            Handles.Label(position, text);
        }

        /// <summary>
        /// Draw a noninteractive sphere handle.
        /// </summary>
        /// <param name="position">The position of the handle in the space of Handles.matrix.</param>
        /// <param name="constantSize">If true, draw this handle at constant screen size.</param>
        public static void SphereHandleCap(Vector3 position, bool constantSize = true)
        {
            Handles.SphereHandleCap(
                0, position, Quaternion.identity,
                constantSize ? HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor : k_WorldSpaceHandleScale,
                EventType.Repaint);
        }

        /// <summary>
        /// Draw a noninteractive cube handle.
        /// </summary>
        /// <param name="position">The position of the handle in the space of Handles.matrix.</param>
        /// <param name="rotation">The rotation of the handle in the space of Handles.matrix.</param>
        /// <param name="constantSize">If true, draw this handle at constant screen size.</param>
        public static void CubeHandleCap(Vector3 position, Quaternion rotation, bool constantSize = true)
        {
            Handles.CubeHandleCap(
                0, position, rotation,
                constantSize ? HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor : k_WorldSpaceHandleScale,
                EventType.Repaint);
        }

        /// <summary>
        /// Draw a noninteractive cone handle.
        /// </summary>
        /// <param name="position">The position of the handle in the space of Handles.matrix.</param>
        /// <param name="direction">The direction of the cone in the space of Handles.matrix.</param>
        /// <param name="constantSize">If true, draw this handle at constant screen size.</param>
        public static void ConeHandleCap(Vector3 position, Quaternion direction, bool constantSize = true)
        {
            Handles.ConeHandleCap(
                0, position, direction,
                constantSize ? HandleUtility.GetHandleSize(position) * k_ConstantHandleSizeFactor : k_WorldSpaceHandleScale,
                EventType.Repaint);
        }

        /// <summary>
        /// Draw a noninteractive circle handle.
        /// </summary>
        /// <param name="position">The position of the handle in the space of Handles.matrix.</param>
        /// <param name="rotation">The rotation of the handle in the space of Handles.matrix.</param>
        /// <param name="constantSize">If true, draw this handle at constant screen size.</param>
        public static void CircleHandleCap(Vector3 position, Quaternion rotation, bool constantSize = true)
        {
            Handles.CircleHandleCap(
                0, position, rotation,
                constantSize ? HandleUtility.GetHandleSize(position) * k_WorldSpaceCircleHandleSize : k_WorldSpaceCircleHandleSize,
                EventType.Repaint);
        }

        /// <summary>
        /// Draw a thick (anti-aliased) line between two points.
        /// </summary>
        /// <param name="startPosition">Start position of the line.</param>
        /// <param name="endPosition">End position of the line.</param>
        public static void DrawLine(Vector3 startPosition, Vector3 endPosition)
        {
            Handles.DrawAAPolyLine(k_LineWidth, startPosition, endPosition);
        }

        /// <summary>
        /// Draw a dotted line from startPosition to endPosition.
        /// </summary>
        /// <param name="startPosition">Start position of the line.</param>
        /// <param name="endPosition">End position of the line.</param>
        /// <param name="screenSpaceSize">The size in pixels for the lengths of the line segments and the gaps between them.</param>
        public static void DrawDottedLine(Vector3 startPosition, Vector3 endPosition, float screenSpaceSize = k_DottedLineScreenSpaceWidth)
        {
            Handles.DrawDottedLine(startPosition, endPosition, screenSpaceSize);
        }
    }
}
