using UnityEngine;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Trigger sensor monitoring vehicle and pedestrian presence within the crosswalk zone.
    /// </summary>
    public class CrossingDetector : MonoBehaviour
    {
        private bool pedestrianInCrossing;
        private bool carInCrossing;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Pedestrian")) pedestrianInCrossing = true;
            if (other.CompareTag("Car")) carInCrossing = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Pedestrian")) pedestrianInCrossing = false;
            if (other.CompareTag("Car")) carInCrossing = false;
        }

        public bool IsPedestrianInCrossing() => pedestrianInCrossing;
        public bool IsCarInCrossing() => carInCrossing;

        public void ResetState()
        {
            pedestrianInCrossing = false;
            carInCrossing = false;
        }
    }
}
