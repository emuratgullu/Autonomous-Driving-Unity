using UnityEngine;

namespace AutonomousDriving.Core.Vehicle
{
    public enum CarSide
    {
        Driver,
        Passenger
    }

    /// <summary>
    /// Raycast-based wheel sensor that computes lateral clearance distances
    /// to road boundaries, guardrails, or lane dividers.
    /// </summary>
    public class CollidingWheel : MonoBehaviour
    {
        [Header("Sensor Configuration")]
        [Tooltip("Mounting side of the vehicle.")]
        public CarSide side;

        [Tooltip("Current computed distance to detected marker/wall.")]
        public float distanceToMarker = 100f;

        [Tooltip("Maximum Raycast probe distance (meters).")]
        public float raycastMaxDistance = 32.0f;

        [Tooltip("Visualization color for Scene view Raycast.")]
        public Color raycastColor = Color.green;

        [Tooltip("Layer mask for road boundary detection.")]
        public LayerMask detectionLayer = ~0;

        private void Update()
        {
            Vector3 rayDirection = (side == CarSide.Driver) 
                ? transform.TransformDirection(Vector3.left) 
                : transform.TransformDirection(Vector3.right);

            Debug.DrawRay(transform.position, rayDirection * raycastMaxDistance, raycastColor, Time.deltaTime, false);

            if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastMaxDistance, detectionLayer))
            {
                // Verify target marker name or layer
                if (hit.collider.name.Contains("Wall") || hit.collider.name.Contains("Lane") || hit.collider.CompareTag("Wall"))
                {
                    distanceToMarker = Vector3.Distance(hit.point, transform.position);
                }
                else
                {
                    distanceToMarker = hit.distance;
                }
            }
            else
            {
                distanceToMarker = raycastMaxDistance;
            }
        }
    }
}
