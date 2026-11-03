using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using AutonomousDriving.Core.Vehicle;

namespace AutonomousDriving.Parking
{
    /// <summary>
    /// Reinforcement Learning Agent for Autonomous Parallel Parking.
    /// Learns precision maneuver, alignment, and deceleration into designated parking bays.
    /// </summary>
    [RequireComponent(typeof(PhysicsCarController))]
    [RequireComponent(typeof(Rigidbody))]
    public class ParallelParkingAgent : Agent
    {
        [Header("Target & Boundary Settings")]
        [Tooltip("Designated parking bay target GameObject.")]
        public GameObject target;

        [Tooltip("Horizontal boundary limits (X/Z local radius).")]
        public float envRadiusX = 15f;
        public float envRadiusZ = 15f;

        [Header("Episode Parameters")]
        [Tooltip("Maximum allowed physics steps before episode timeout.")]
        public int maxEpisodeSteps = 5000;

        [Header("Reward Tuning")]
        [Tooltip("Small step penalty applied each tick to encourage expeditious parking.")]
        public float stepPenalty = -0.01f;

        [Tooltip("Success bonus awarded upon achieving legal parking pose.")]
        public float successBonus = 100f;

        [Tooltip("Penalty incurred when exceeding environment boundaries.")]
        public float outOfBoundsPenalty = -100f;

        [Header("Fine Tuning Toggles")]
        [Tooltip("Whether to penalize remaining velocity while inside the target bay.")]
        public bool penalizeMotionInTarget = true;

        [Tooltip("Whether to give extra proximity incentive when within 2 meters.")]
        public bool rewardProximityClose = true;

        private PhysicsCarController carController;
        private Rigidbody rb;
        private int stepCounter = 0;
        private bool inTarget = false;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 lastPosition;

        public override void Initialize()
        {
            carController = GetComponent<PhysicsCarController>();
            rb = GetComponent<Rigidbody>();

            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            lastPosition = startPosition;

            if (target == null)
            {
                Debug.LogError($"[{name}] Target parking bay reference is NULL!");
            }
        }

        public override void OnEpisodeBegin()
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;

            carController.ResetPhysics();
            inTarget = false;
            stepCounter = 0;
            lastPosition = startPosition;
        }

        private void FixedUpdate()
        {
            if (target == null) return;

            // Out-of-bounds boundary check
            float deltaX = Mathf.Abs(transform.localPosition.x - target.transform.localPosition.x);
            float deltaZ = Mathf.Abs(transform.localPosition.z - target.transform.localPosition.z);

            if (deltaX > envRadiusX || deltaZ > envRadiusZ)
            {
                AddReward(outOfBoundsPenalty);
                EndEpisode();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (target == null) return;

            // 1. Vehicle dynamics
            sensor.AddObservation(carController.CurrentSpeed);
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(transform.localRotation);

            // 2. Target bay geometry
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation(Vector3.Distance(transform.position, target.transform.position));
            sensor.AddObservation(Vector3.Angle(transform.forward, target.transform.forward));

            // 3. Status flag
            sensor.AddObservation(inTarget ? 1f : 0f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            AddReward(stepPenalty);

            float steering = actions.ContinuousActions[0];
            float rawAccel = actions.ContinuousActions[1];
            float rawReverse = actions.ContinuousActions[2];

            // Normalize continuous inputs from [-1, 1] to [0, 1]
            float accel = (rawAccel + 1f) / 2f;
            float reverse = (rawReverse + 1f) / 2f;
            float netThrottle = accel - reverse;

            carController.Move(steering, netThrottle, 0f, 0f);

            stepCounter++;
            if (stepCounter > maxEpisodeSteps)
            {
                AddReward(-1f);
                EndEpisode();
                return;
            }

            float stepReward = CalculateReward();
            AddReward(stepReward);
        }

        private float CalculateReward()
        {
            float reward = 0f;

            // 1. Directional progress reward
            if (lastPosition != Vector3.zero && target != null)
            {
                float currentDistX = Mathf.Abs(transform.localPosition.x - target.transform.localPosition.x);
                float currentDistZ = Mathf.Abs(transform.localPosition.z - target.transform.localPosition.z);

                float lastDistX = Mathf.Abs(lastPosition.x - target.transform.localPosition.x);
                float lastDistZ = Mathf.Abs(lastPosition.z - target.transform.localPosition.z);

                float deltaProgress = (lastDistX - currentDistX) + (lastDistZ - currentDistZ);
                deltaProgress = Mathf.Clamp(deltaProgress, -1f, 0.75f);
                reward += deltaProgress;

                if (rewardProximityClose)
                {
                    float dist = Vector3.Distance(transform.localPosition, target.transform.localPosition);
                    if (dist < 2f)
                    {
                        reward += (1f / (dist + 1f)) * 0.1f;
                    }
                }
            }

            // 2. Parking target reward
            if (inTarget && target != null)
            {
                if (penalizeMotionInTarget && carController.CurrentSpeed > 0.5f)
                {
                    AddReward(-0.01f); // Incentivize coming to a full stop
                }

                float angleToTarget = Vector3.Angle(transform.forward, target.transform.forward);
                angleToTarget = Mathf.Clamp(angleToTarget, 0f, 45f);
                float angleReward = (-(1f / 45f) * angleToTarget) + 1f;
                reward += angleReward + 1f;

                // Success criteria: Centered, aligned, and stopped
                float localDiffX = Mathf.Abs(transform.localPosition.x - target.transform.localPosition.x);
                float localDiffZ = Mathf.Abs(transform.localPosition.z - target.transform.localPosition.z);

                if (localDiffX < 1.0f && localDiffZ < 1.0f && angleToTarget < 5f && carController.CurrentSpeed < 1.0f)
                {
                    Debug.Log($"[{name}] Successfully parked! Rewarding +{successBonus}");
                    reward += successBonus;
                    EndEpisode();
                }
            }
            else if (target != null)
            {
                Vector3 targetToCar = transform.position - target.transform.position;
                float angleToCar = Vector3.Angle(targetToCar, transform.forward);
                angleToCar = Mathf.Clamp(angleToCar, 0f, 90f);
                float angleProgress = (-(1f / 180f) * angleToCar) + 1f;
                reward += angleProgress;
            }

            lastPosition = transform.localPosition;
            return reward;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            float steering = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            float accel = vertical > 0 ? vertical : 0f;
            float reverse = vertical < 0 ? -vertical : 0f;

            continuousActions[0] = steering;
            continuousActions[1] = accel * 2f - 1f;
            continuousActions[2] = reverse * 2f - 1f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target"))
            {
                inTarget = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Target"))
            {
                inTarget = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                AddReward(-10f);
                EndEpisode();
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("RoadPavement"))
            {
                AddReward(-2f);
                EndEpisode();
            }
            else if (collision.gameObject.CompareTag("ParkingCar"))
            {
                float penalty = -carController.CurrentSpeed * 10f - 5f;
                AddReward(penalty);
                EndEpisode();
            }
        }
    }
}
