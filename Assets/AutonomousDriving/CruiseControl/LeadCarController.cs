using UnityEngine;

namespace AutonomousDriving.CruiseControl
{
    /// <summary>
    /// Autonomous lead vehicle with dynamic speed profiles (acceleration, cruising, braking, stopping)
    /// to challenge the Adaptive Cruise Control (ACC) agent.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LeadCarController : MonoBehaviour
    {
        [Header("Orientation Guide")]
        public Transform rightWallTransform;

        [Header("Agent Reference")]
        public ACCAgent blueAgent;

        [Header("Telemetry")]
        public float currentSpeed = 0f;

        private float timer = 0f;
        private Rigidbody rb;
        private Vector3 startPos;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            startPos = transform.position;
        }

        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;

            // Dynamic Speed Stage Schedule
            if (timer <= 10f)
            {
                currentSpeed = 8f;
            }
            else if (timer > 10f && timer <= 30f)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 4f, Time.fixedDeltaTime);
            }
            else if (timer > 30f && timer <= 40f)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.fixedDeltaTime * 2f);
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 8f, Time.fixedDeltaTime);
            }

            if (rightWallTransform != null)
            {
                Vector3 rotatedForward = Quaternion.Euler(0, 45, 0) * rightWallTransform.forward;
                Quaternion targetRotation = Quaternion.LookRotation(rotatedForward);
                rb.MoveRotation(targetRotation);
            }

            Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }

        public void ResetCar()
        {
            timer = 0f;
            currentSpeed = 4f;
            transform.position = startPos;

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
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("EndBox") && blueAgent != null)
            {
                blueAgent.SuccessfulEpisodeEnd();
            }
        }
    }
}
