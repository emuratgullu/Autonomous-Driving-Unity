using UnityEngine;

namespace AutonomousDriving.CruiseControl
{
    /// <summary>
    /// Interactive obstacle vehicle simulating sudden cut-in traffic maneuvers
    /// when the ego agent reaches a designated proximity threshold.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RedCarController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 15f;

        [Header("Activation Triggers")]
        public Transform blueCarTransform;
        public float activationDistance = 25f;

        [Header("Alignment")]
        public Transform rightWallTransform;

        [Header("Reset Parameters")]
        public GameObject endBox;
        public string endBoxTag = "EndBox";

        private Rigidbody rb;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool hasStartedMoving = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
        }

        private void FixedUpdate()
        {
            if (!hasStartedMoving && blueCarTransform != null)
            {
                float distance = Vector3.Distance(transform.position, blueCarTransform.position);
                if (distance <= activationDistance)
                {
                    hasStartedMoving = true;
                }
            }

            if (rightWallTransform != null)
            {
                Vector3 rotatedForward = Quaternion.Euler(0, 45, 0) * rightWallTransform.forward;
                Quaternion targetRotation = Quaternion.LookRotation(rotatedForward);
                rb.MoveRotation(targetRotation);
            }

            if (hasStartedMoving)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = transform.forward * forwardSpeed;
#else
                rb.velocity = transform.forward * forwardSpeed;
#endif
            }
            else
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject == endBox || collision.gameObject.CompareTag(endBoxTag))
            {
                ResetCar();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == endBox || other.CompareTag(endBoxTag))
            {
                ResetCar();
            }
        }

        public void ResetCar()
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
            hasStartedMoving = false;
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
        }
    }
}
