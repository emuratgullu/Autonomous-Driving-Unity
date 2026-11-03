using UnityEngine;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Destination trigger detecting safe vehicle arrival past the crosswalk.
    /// </summary>
    public class GoalTrigger : MonoBehaviour
    {
        [Header("References")]
        public EpisodeController episodeController;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Car"))
            {
                if (episodeController != null)
                    episodeController.OnGoalReached();
            }
        }
    }
}
