using UnityEngine;
using AutonomousDriving.Core.Vehicle;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Orchestrates pedestrian crossing scenario episodes, manages spawn randomization,
    /// distance-based collision checks, and lifecycle resets.
    /// </summary>
    public class EpisodeController : MonoBehaviour
    {
        [Header("References")]
        public GameObject car;
        public GameObject pedestrian;
        public CrossingDetector crossingDetector;
        public GameObject carSpawnObj;
        public GameObject goalObj;

        [Header("Episode Settings")]
        public int maxStepsPerEpisode = 5000;
        public bool agentDriven = true;

        [Header("Detection Thresholds")]
        public float collisionDistance = 2.5f;
        public float goalDistance = 3.0f;

        [Header("Spawn Randomization")]
        public float carSpawnRandomX = 5f;

        private int currentStep;
        private bool carHitPedestrian;
        private bool carReachedGoal;
        private bool timedOut;
        private int episodeCount;
        private PedestrianController pedController;

        public bool CarHitPedestrian => carHitPedestrian;
        public bool CarReachedGoal => carReachedGoal;
        public bool TimedOut => timedOut;
        public int EpisodeCount => episodeCount;

        private void Start()
        {
            if (pedestrian != null)
                pedController = pedestrian.GetComponent<PedestrianController>();
            ResetEpisode();
        }

        private void FixedUpdate()
        {
            currentStep++;
            if (currentStep >= maxStepsPerEpisode)
                timedOut = true;

            if (!carHitPedestrian && car != null && pedController != null && pedController.IsPresent)
            {
                if (pedController.IsWalking || pedController.IsOnRoad)
                {
                    float dist = Vector3.Distance(car.transform.position, pedestrian.transform.position);
                    if (dist < collisionDistance)
                        carHitPedestrian = true;
                }
            }

            if (!carReachedGoal && car != null && goalObj != null)
            {
                float dist = Vector3.Distance(car.transform.position, goalObj.transform.position);
                if (dist < goalDistance)
                    carReachedGoal = true;
            }

            // In manual evaluation mode, auto-reset upon terminal states
            if (!agentDriven && (carHitPedestrian || carReachedGoal || timedOut))
            {
                episodeCount++;
                ResetEpisode();
            }
        }

        public void ResetEpisode()
        {
            currentStep = 0;
            carHitPedestrian = false;
            carReachedGoal = false;
            timedOut = false;

            if (car != null && carSpawnObj != null)
            {
                Vector3 spawnPos = carSpawnObj.transform.position;
                spawnPos.x += Random.Range(-carSpawnRandomX, carSpawnRandomX);
                car.transform.position = spawnPos;
                car.transform.rotation = carSpawnObj.transform.rotation;

                var sc = car.GetComponent<SimpleCarController>();
                if (sc != null) sc.ResetCar();

                var pc = car.GetComponent<PhysicsCarController>();
                if (pc != null) pc.ResetPhysics();
            }

            if (pedController != null)
                pedController.ResetPedestrian();

            if (crossingDetector != null)
                crossingDetector.ResetState();
        }

        public void OnGoalReached() => carReachedGoal = true;
        public void OnCollisionWithPedestrian() => carHitPedestrian = true;
    }
}
