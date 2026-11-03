using UnityEngine;

namespace AutonomousDriving.Core.Camera
{
    /// <summary>
    /// Smooth third-person chase camera for autonomous vehicle tracking.
    /// Supports both local (rotated with car) and world-space offsets.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Vehicle target transform to track.")]
        public Transform target;

        [Header("Offset Settings")]
        [Tooltip("Offset relative to vehicle orientation.")]
        public Vector3 offset = new Vector3(0f, 6f, -10f);

        [Tooltip("If true, offset rotates with the vehicle. If false, offset stays in world coordinates.")]
        public bool useLocalSpace = true;

        [Header("Interpolation Smoothing")]
        [Tooltip("Smooth factor for positional translation.")]
        public float translationSpeed = 8f;

        [Tooltip("Smooth factor for rotational tracking.")]
        public float rotationSpeed = 10f;

        private void LateUpdate()
        {
            if (target == null) return;

            HandleTranslation();
            HandleRotation();
        }

        private void HandleTranslation()
        {
            Vector3 targetPosition = useLocalSpace 
                ? target.TransformPoint(offset) 
                : target.position + offset;

            transform.position = Vector3.Lerp(transform.position, targetPosition, translationSpeed * Time.deltaTime);
        }

        private void HandleRotation()
        {
            Vector3 direction = (target.position + Vector3.up * 1.5f) - transform.position;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
