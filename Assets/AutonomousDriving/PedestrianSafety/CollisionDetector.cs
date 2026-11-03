using UnityEngine;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Collision listener detecting impact between vehicle and pedestrians.
    /// </summary>
    public class CollisionDetector : MonoBehaviour
    {
        [Header("References")]
        public EpisodeController episodeController;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Pedestrian"))
            {
                if (episodeController != null)
                    episodeController.OnCollisionWithPedestrian();
            }
        }
    }
}
