using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField, Tooltip("The target object the camera follows.")]
        private Transform target;

        // The offset distance between the camera and the target.
        private Vector3 offset;

        void Start()
        {
            // Calculate the initial offset between the camera and the target.
            offset = transform.position - target.position;
        }

        void LateUpdate()
        {
            // Update the camera's position by maintaining the initial offset relative to the target.
            transform.position = offset + target.position;
        }
    }
}
