using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace AutonomousDriving.CruiseControl
{
    /// <summary>
    /// Adaptive Cruise Control (ACC) Agent using Reinforcement Learning.
    /// Learns to match lead vehicle speed while maintaining safe following headway.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ACCAgent : Agent
    {
        [Header("Scenario References")]
        [Tooltip("Reference to dynamic lead vehicle.")]
        public LeadCarController leadCar;

        [Tooltip("Transform guide ensuring vehicle remains aligned on road axis.")]
        public Transform rightWallTransform;

        [Header("Vehicle Dynamics")]
        public float maxSpeed = 15f;
        public float accelerationForce = 15f;
        public float brakingForce = 30f;

        [Header("Safety Parameters")]
        [Tooltip("Target distance to maintain behind lead car (meters).")]
        public float safeDistance = 30f;

        [Tooltip("Maximum allowed distance before applying timeout penalty.")]
        public float maxAllowedDistance = 100f;

        [Header("Distance Tracking (Live Telemetry)")]
        public float currentDistanceToLeadCar;
        private bool keptDistanceLessThanSafe;

        private Rigidbody rb;
        private Vector3 startPos;
        private Quaternion startRot;

        public override void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            startPos = transform.position;
            startRot = transform.rotation;
        }

        public override void OnEpisodeBegin()
        {
            transform.position = startPos;
            transform.rotation = startRot;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif

            keptDistanceLessThanSafe = true;
            currentDistanceToLeadCar = 0f;

            if (leadCar != null)
            {
                leadCar.ResetCar();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
#if UNITY_6000_0_OR_NEWER
            float currentSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
#else
            float currentSpeed = transform.InverseTransformDirection(rb.velocity).z;
#endif
            sensor.AddObservation(currentSpeed);

            if (leadCar != null)
            {
                float distance = Vector3.Distance(transform.position, leadCar.transform.position);
                sensor.AddObservation(distance);
                sensor.AddObservation(leadCar.currentSpeed);
            }
            else
            {
                sensor.AddObservation(100f);
                sensor.AddObservation(0f);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float accelerationAction = actions.ContinuousActions[0];

#if UNITY_6000_0_OR_NEWER
            float currentSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
#else
            float currentSpeed = transform.InverseTransformDirection(rb.velocity).z;
#endif

            // Align rotation with roadway orientation
            if (rightWallTransform != null)
            {
                Vector3 rotatedForward = Quaternion.Euler(0, 45, 0) * rightWallTransform.forward;
                Quaternion targetRotation = Quaternion.LookRotation(rotatedForward);
                rb.MoveRotation(targetRotation);
            }

            // Apply propulsion physics
            if (accelerationAction > 0)
            {
                if (currentSpeed < maxSpeed)
                {
                    rb.AddForce(transform.forward * accelerationAction * accelerationForce, ForceMode.Acceleration);
                }
            }
            else if (accelerationAction < 0)
            {
                if (currentSpeed > 0.5f)
                {
                    rb.AddForce(transform.forward * accelerationAction * brakingForce, ForceMode.Acceleration);
                }
                else
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
#else
                    rb.velocity = new Vector3(0, rb.velocity.y, 0);
#endif
                }
            }

            // Reward Calculation
            if (leadCar != null)
            {
                float distance = Vector3.Distance(transform.position, leadCar.transform.position);
                currentDistanceToLeadCar = distance;

                if (distance > maxAllowedDistance)
                {
                    SetReward(-5f);
                    EndEpisode();
                    return;
                }

                if (distance >= safeDistance)
                {
                    keptDistanceLessThanSafe = false;
                }

                // Headway window reward
                if (distance >= safeDistance - 3f && distance <= safeDistance + 3f)
                {
                    AddReward(0.2f);
                }
                else if (distance < safeDistance / 2f)
                {
                    AddReward(-0.2f); // Tailgating hazard
                }

                // Speed synchronization reward
                float speedDifference = Mathf.Abs(currentSpeed - leadCar.currentSpeed);
                if (speedDifference < 1.0f)
                {
                    AddReward(0.01f);
                }
                else if (speedDifference > 5.0f)
                {
                    AddReward(-0.01f);
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxis("Vertical");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("RedCar"))
            {
                SetReward(-1f);
                EndEpisode();
            }
        }

        public void SuccessfulEpisodeEnd()
        {
            SetReward(1f);

            if (keptDistanceLessThanSafe)
            {
                AddReward(10.0f); // Bonus for maintaining strict safe buffer
            }

            EndEpisode();
        }
    }
}
