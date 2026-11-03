using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

namespace AutonomousDriving.LaneKeeping
{
    /// <summary>
    /// Reinforcement Learning Agent for Lane Centering and Speed Tracking (LKA / Lane Keeping Assist).
    /// Employs lateral Raycasts and exponential reward functions for lane centering.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LaneCenteringAgent : Agent
    {
        [Header("Wheel Colliders")]
        public WheelCollider frontLeftWheelCollider;
        public WheelCollider frontRightWheelCollider;
        public WheelCollider rearLeftWheelCollider;
        public WheelCollider rearRightWheelCollider;

        [Header("Visual Wheel Transforms")]
        public Transform frontLeftWheelTransform;
        public Transform frontRightWheelTransform;
        public Transform rearLeftWheelTransform;
        public Transform rearRightWheelTransform;

        [Header("Vehicle Dynamics")]
        public float maxSteeringAngle = 30f;
        public float motorForce = 4000f;
        public float brakeForce = 3000f;

        [Header("Lane Raycast Sensors")]
        public LayerMask laneLayer;
        public float maxRayDist = 15f;
        public float roadWidth = 8f;

        [Header("Speed & Safety Settings")]
        [Tooltip("Target cruise speed in m/s (16.67 m/s = 60 km/h).")]
        public float targetSpeed = 16.67f;
        public float offTrackPenalty = -0.5f;

        private Rigidbody rb;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private float lastDistLeft = 1f;
        private float lastDistRight = 1f;
        private float steerAngle;

        public override void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            startPosition = transform.position;
            startRotation = transform.rotation;
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
            transform.position = startPosition;
            transform.rotation = startRotation;

            lastDistLeft = 1f;
            lastDistRight = 1f;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
#if UNITY_6000_0_OR_NEWER
            float forwardVelocity = Vector3.Dot(rb.linearVelocity, transform.forward);
#else
            float forwardVelocity = Vector3.Dot(rb.velocity, transform.forward);
#endif
            sensor.AddObservation(forwardVelocity / targetSpeed);
            sensor.AddObservation(lastDistLeft);
            sensor.AddObservation(lastDistRight);
        }

        private void FixedUpdate()
        {
            // Inversion / roll check
            if (Vector3.Dot(transform.up, Vector3.up) < 0.3f)
            {
                SetReward(-1.0f);
                EndEpisode();
                return;
            }

#if UNITY_6000_0_OR_NEWER
            float forwardVelocity = Vector3.Dot(rb.linearVelocity, transform.forward);
#else
            float forwardVelocity = Vector3.Dot(rb.velocity, transform.forward);
#endif

            // Lateral Raycast probes
            lastDistLeft = 1f;
            lastDistRight = 1f;

            if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, maxRayDist, laneLayer, QueryTriggerInteraction.Collide))
            {
                lastDistLeft = hitLeft.distance / maxRayDist;
            }
            if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight, maxRayDist, laneLayer, QueryTriggerInteraction.Collide))
            {
                lastDistRight = hitRight.distance / maxRayDist;
            }

            // Speed Matching Gaussian Reward
            float speedError = Mathf.Abs(forwardVelocity - targetSpeed);
            float speedReward = Mathf.Exp(-0.1f * speedError * speedError);
            AddReward(speedReward * 0.05f);

            // Centering Reward
            bool isOnTrack = (lastDistLeft < 1.0f) || (lastDistRight < 1.0f);
            if (isOnTrack)
            {
                float centerDifference = Mathf.Abs(lastDistLeft - lastDistRight);
                float centeringReward = Mathf.Exp(-10.0f * centerDifference * centerDifference);
                AddReward(centeringReward * 0.03f);
            }
            else
            {
                SetReward(offTrackPenalty);
            }

            // Penalty for reversing
            if (forwardVelocity < -0.1f)
            {
                AddReward(-0.1f);
                EndEpisode();
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float steerInput = actions.ContinuousActions[0];
            float motorInput = actions.ContinuousActions[1];

            HandleMotor(motorInput);
            HandleSteering(steerInput);
            UpdateWheels();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            continuousActions[0] = keyboard.aKey.isPressed ? -1f : (keyboard.dKey.isPressed ? 1f : 0f);
            continuousActions[1] = keyboard.wKey.isPressed ? 1f : (keyboard.sKey.isPressed ? -1f : 0f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Wall"))
            {
                SetReward(-1.0f);
                EndEpisode();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("LaneLine"))
            {
                AddReward(-1.5f); // Outer boundary penalty
            }
            else if (other.CompareTag("YellowLane"))
            {
                AddReward(-1.0f); // Inner boundary penalty
                EndEpisode();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("LaneLine"))
            {
                AddReward(-0.1f);
            }
        }

        private void HandleSteering(float steerInput)
        {
            steerAngle = maxSteeringAngle * steerInput;
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.steerAngle = steerAngle;
            if (frontRightWheelCollider != null) frontRightWheelCollider.steerAngle = steerAngle;
        }

        private void HandleMotor(float motorInput)
        {
            float torque = motorInput * motorForce;
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.motorTorque = torque;
            if (frontRightWheelCollider != null) frontRightWheelCollider.motorTorque = torque;

            float currentBrake = motorInput < 0f ? brakeForce : 0f;
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.brakeTorque = currentBrake;
            if (frontRightWheelCollider != null) frontRightWheelCollider.brakeTorque = currentBrake;
            if (rearLeftWheelCollider != null) rearLeftWheelCollider.brakeTorque = currentBrake;
            if (rearRightWheelCollider != null) rearRightWheelCollider.brakeTorque = currentBrake;
        }

        private void UpdateWheels()
        {
            UpdateWheelTransform(frontLeftWheelCollider, frontLeftWheelTransform);
            UpdateWheelTransform(frontRightWheelCollider, frontRightWheelTransform);
            UpdateWheelTransform(rearLeftWheelCollider, rearLeftWheelTransform);
            UpdateWheelTransform(rearRightWheelCollider, rearRightWheelTransform);
        }

        private void UpdateWheelTransform(WheelCollider col, Transform trans)
        {
            if (col == null || trans == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            trans.position = pos;
            trans.rotation = rot;
        }
    }
}
