using UnityEngine;
using AutonomousDriving.Core.Vehicle;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Computes reinforcement learning rewards and terminal signals for pedestrian crosswalk yielding.
    /// Rewards smooth deceleration when pedestrians enter the road, penalizes hazardous close passes,
    /// and grants bonuses for safe goal arrivals.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        [Header("Controller Dependencies")]
        public EpisodeController episodeController;
        public SimpleCarController carController;
        public PedestrianController pedestrian;
        public CrossingDetector crossingDetector;

        [Header("Reward Hyperparameters")]
        public float goalReward = 10.0f;
        public float collisionPenalty = -20.0f;
        public float timePenalty = -0.001f;
        public float yieldBonus = 5.0f;
        public float failureToYieldPenalty = -10.0f;

        private float lastStepReward = 0f;
        private bool isTerminal = false;
        private bool didYield = false;
        private bool yieldRewarded = false;

        public float LastStepReward => lastStepReward;
        public bool IsTerminal => isTerminal;
        public bool DidYield => didYield;

        private void FixedUpdate()
        {
            if (episodeController == null) return;

            lastStepReward = timePenalty;
            isTerminal = false;

            // Check if pedestrian is in hazardous road zone
            bool pedCrossing = pedestrian != null && pedestrian.IsPresent && (pedestrian.IsWalking || pedestrian.IsOnRoad);
            float speed = carController != null ? carController.GetSpeed() : 0f;

            // Yield condition: Car decelerates or stops while pedestrian is actively crossing
            if (pedCrossing)
            {
                if (speed < 1.0f)
                {
                    didYield = true;
                    if (!yieldRewarded)
                    {
                        lastStepReward += yieldBonus;
                        yieldRewarded = true;
                    }
                }
                else if (speed > 5.0f && crossingDetector != null && crossingDetector.IsCarInCrossing())
                {
                    // Severe penalty for blasting through while pedestrian is crossing
                    lastStepReward += failureToYieldPenalty * Time.fixedDeltaTime;
                }
            }

            // Terminal evaluation
            if (episodeController.CarHitPedestrian)
            {
                lastStepReward += collisionPenalty;
                isTerminal = true;
            }
            else if (episodeController.CarReachedGoal)
            {
                lastStepReward += goalReward;
                if (didYield || (pedestrian != null && !pedestrian.IsPresent))
                {
                    lastStepReward += 2.0f; // Extra clean run bonus
                }
                isTerminal = true;
            }
            else if (episodeController.TimedOut)
            {
                lastStepReward += -1.0f;
                isTerminal = true;
            }

            if (isTerminal)
            {
                yieldRewarded = false;
                didYield = false;
            }
        }
    }
}
