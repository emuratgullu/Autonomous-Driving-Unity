using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AutonomousDriving.Core.Vehicle
{
    /// <summary>
    /// Realistic 4-wheel vehicle physics controller using Unity WheelColliders.
    /// Supports both autonomous agent inputs and manual keyboard controls.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsCarController : MonoBehaviour
    {
        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheelCollider;
        [SerializeField] private WheelCollider frontRightWheelCollider;
        [SerializeField] private WheelCollider rearLeftWheelCollider;
        [SerializeField] private WheelCollider rearRightWheelCollider;

        [Header("Wheel Transforms")]
        [SerializeField] private Transform frontLeftWheelTransform;
        [SerializeField] private Transform frontRightWheelTransform;
        [SerializeField] private Transform rearLeftWheelTransform;
        [SerializeField] private Transform rearRightWheelTransform;

        [Header("Vehicle Dynamics")]
        [Tooltip("Maximum steering angle for the front wheels in degrees.")]
        [SerializeField] private float maxSteeringAngle = 35f;

        [Tooltip("Motor torque applied to the drive wheels (Nm).")]
        [SerializeField] private float motorForce = 1500f;

        [Tooltip("Braking torque applied to all wheels (Nm).")]
        [SerializeField] private float brakeForce = 3000f;

        [Header("Control Settings")]
        [Tooltip("When enabled, manual keyboard input is ignored and vehicle is driven by autonomous agents.")]
        [SerializeField] private bool agentDriven = true;

        private Rigidbody rb;
        private float horizontalInput;
        private float verticalInput;
        private float currentSteerAngle;
        private bool isBraking;

        /// <summary>
        /// Current speed of the vehicle in m/s (based on Rigidbody velocity).
        /// </summary>
        public float CurrentSpeed
        {
            get
            {
                if (rb == null) rb = GetComponent<Rigidbody>();
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity.magnitude;
#else
                return rb.velocity.magnitude;
#endif
            }
        }

        /// <summary>
        /// Current speed of the vehicle in km/h.
        /// </summary>
        public float SpeedKmh => CurrentSpeed * 3.6f;

        public bool AgentDriven
        {
            get => agentDriven;
            set => agentDriven = value;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!agentDriven)
            {
                HandleManualInput();
            }

            ApplyMotor();
            ApplySteering();
            UpdateWheelPoses();
        }

        /// <summary>
        /// Direct control method invoked by Reinforcement Learning agents (e.g., ParallelParkingAgent).
        /// </summary>
        /// <param name="steering">Steering input in range [-1, 1].</param>
        /// <param name="accel">Acceleration input in range [-1, 1].</param>
        /// <param name="footbrake">Footbrake pressure in range [0, 1].</param>
        /// <param name="handbrake">Handbrake pressure in range [0, 1].</param>
        public void Move(float steering, float accel, float footbrake, float handbrake)
        {
            horizontalInput = Mathf.Clamp(steering, -1f, 1f);
            verticalInput = Mathf.Clamp(accel, -1f, 1f);
            isBraking = (handbrake > 0.5f) || (footbrake > 0.1f && Mathf.Abs(verticalInput) < 0.1f);

            ApplyMotor();
            ApplySteering();
            UpdateWheelPoses();
        }

        private void HandleManualInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Steering input (A/D or Left/Right Arrow)
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontalInput = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontalInput = 1f;
            else
                horizontalInput = 0f;

            // Acceleration & Reversing input (W/S or Up/Down Arrow)
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                verticalInput = 1f;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                verticalInput = -1f;
            else
                verticalInput = 0f;

            isBraking = keyboard.spaceKey.isPressed;
        }

        private void ApplySteering()
        {
            currentSteerAngle = maxSteeringAngle * horizontalInput;
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.steerAngle = currentSteerAngle;
            if (frontRightWheelCollider != null) frontRightWheelCollider.steerAngle = currentSteerAngle;
        }

        private void ApplyMotor()
        {
            float targetTorque = verticalInput * motorForce;

            if (frontLeftWheelCollider != null) frontLeftWheelCollider.motorTorque = targetTorque;
            if (frontRightWheelCollider != null) frontRightWheelCollider.motorTorque = targetTorque;

            float currentBrakeTorque = isBraking ? brakeForce : 0f;
            ApplyBrakeTorque(currentBrakeTorque);
        }

        private void ApplyBrakeTorque(float torque)
        {
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.brakeTorque = torque;
            if (frontRightWheelCollider != null) frontRightWheelCollider.brakeTorque = torque;
            if (rearLeftWheelCollider != null) rearLeftWheelCollider.brakeTorque = torque;
            if (rearRightWheelCollider != null) rearRightWheelCollider.brakeTorque = torque;
        }

        private void UpdateWheelPoses()
        {
            UpdateSingleWheelPose(frontLeftWheelCollider, frontLeftWheelTransform);
            UpdateSingleWheelPose(frontRightWheelCollider, frontRightWheelTransform);
            UpdateSingleWheelPose(rearLeftWheelCollider, rearLeftWheelTransform);
            UpdateSingleWheelPose(rearRightWheelCollider, rearRightWheelTransform);
        }

        private void UpdateSingleWheelPose(WheelCollider col, Transform trans)
        {
            if (col == null || trans == null) return;

            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            trans.position = pos;
            trans.rotation = rot;
        }

        /// <summary>
        /// Resets vehicle momentum and wheel forces to initial resting state.
        /// </summary>
        public void ResetPhysics()
        {
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#endif
            }

            ApplyBrakeTorque(0f);
            horizontalInput = 0f;
            verticalInput = 0f;
            isBraking = false;
        }
    }
}
