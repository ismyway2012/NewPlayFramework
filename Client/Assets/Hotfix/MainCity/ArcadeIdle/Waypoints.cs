using System.Collections.Generic;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class Waypoints : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField, Tooltip("Color used to visualize waypoint points in the editor.")]
        private Color pointColor = Color.yellow;

        [SerializeField, Tooltip("Color used to visualize the lines connecting waypoints.")]
        private Color lineColor = Color.blue;

        [SerializeField, Tooltip("Size of the sphere used to represent each waypoint.")]
        private float pointSize = 0.3f;

        void OnDrawGizmos()
        {
            // Draw spheres to represent the waypoints
            Gizmos.color = pointColor;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Gizmos.DrawSphere(child.position, pointSize);
            }

            // Draw lines connecting the waypoints
            Gizmos.color = lineColor;
            for (int i = 0; i < transform.childCount - 1; i++)
            {
                Transform startPoint = transform.GetChild(i);
                Transform endPoint = transform.GetChild(i + 1);
                Gizmos.DrawLine(startPoint.position, endPoint.position);
            }
        }
#endif
        /// <summary>
        /// Returns the waypoint at the specified index.
        /// </summary>
        /// <param name="index">The index of the waypoint to retrieve.</param>
        /// <returns>The Transform representing the waypoint.</returns>
        public Transform GetPoint(int index)
        {
            return transform.GetChild(index);
        }

        /// <summary>
        /// Returns a list of all waypoint positions.
        /// </summary>
        /// <returns>A list of Vector3 positions of the waypoints.</returns>
        public List<Vector3> GetPoints()
        {
            var points = new List<Vector3>();

            foreach (Transform child in transform)
            {
                points.Add(child.position);
            }

            return points;
        }
    }
}
