using UnityEngine;
using UnityEngine.InputSystem;

namespace AutonomousDriving.Core.Vehicle
{
    /// <summary>
    /// Simplified longitudinal vehicle controller ideal for crosswalk yielding,
    /// pedestrian collision avoidance, and discrete forward/braking scenarios.
    /// </summary>
    public class SimpleCarController : MonoBehaviour
    {
        [Header("Dynamics Settings")]
        [Tooltip("Top speed achievable by the vehicle in m/s.")]
        public float maxSpeed = 20f;

        [Tooltip("Forward acceleration in m/s^2.")]
        public float acceleration = 8f;

        [Tooltip("Active braking deceleration in m/s^2.")]
        public float brakeDeceleration = 12f;

        [Tooltip("Natural friction/drag deceleration when coasting in m/s^2.")]
        public float dragDeceleration = 0.5f;

        [Header("Operation Mode")]
        [Tooltip("Set to true when controlled by an autonomous agent script.")]
        public bool agentDriven = false;

        private float currentSpeed;
        private int discreteAction = 1; // 0: Brake, 1: Coast, 2: Accelerate

        /// <summary>
        /// Returns normalized speed in range [0, 1].
        /// </summary>
        public float NormalizedSpeed => Mathf.Clamp01(currentSpeed / maxSpeed);

        private void Update()
        {
            if (agentDriven) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                discreteAction = 2; // Accelerate
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                discreteAction = 0; // Brake
            else
                discreteAction = 1; // Coast
        }

        private void FixedUpdate()
        {
            switch (discreteAction)
            {
                case 2: // Accelerate
                    currentSpeed += acceleration * Time.fixedDeltaTime;
                    break;
                case 0: // Brake
                    currentSpeed -= brakeDeceleration * Time.fixedDeltaTime;
                    break;
                default: // Coast / friction
                    currentSpeed -= dragDeceleration * Time.fixedDeltaTime;
                    break;
            }

            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
            transform.position += transform.forward * currentSpeed * Time.fixedDeltaTime;
        }

        /// <summary>
        /// Sets the discrete driving action (0: Brake, 1: Coast, 2: Accelerate).
        /// </summary>
        public void SetDiscreteAction(int action)
        {
            discreteAction = Mathf.Clamp(action, 0, 2);
        }

        /// <summary>
        /// Returns the current speed in m/s.
        /// </summary>
        public float GetSpeed() => currentSpeed;

        /// <summary>
        /// Resets vehicle speed to zero and state to idle.
        /// </summary>
        public void ResetCar()
        {
            currentSpeed = 0f;
            discreteAction = 1;
        }
    }
}
