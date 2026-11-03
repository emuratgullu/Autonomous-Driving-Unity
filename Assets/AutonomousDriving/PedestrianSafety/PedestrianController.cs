using UnityEngine;

namespace AutonomousDriving.PedestrianSafety
{
    /// <summary>
    /// Autonomous pedestrian with dynamic behavior: curb waiting, approach triggering,
    /// randomized crossing speeds, and edge-case non-crossing / absence simulation.
    /// </summary>
    public class PedestrianController : MonoBehaviour
    {
        [Header("Spawn Points")]
        public GameObject spawnLeft;
        public GameObject spawnRight;
        public float roadWidth = 8f;

        [Header("Speed Settings")]
        public float minWalkSpeed = 1.0f;
        public float maxWalkSpeed = 2.5f;

        [Header("Trigger Distance")]
        public Transform carTransform;
        public float minTriggerDistance = 15f;
        public float maxTriggerDistance = 25f;

        [Header("Road Geometry")]
        public float roadCenterZ = 0f;
        public float roadHalfWidth = 3f;

        [Header("Behavior Randomization")]
        [Tooltip("Probability that pedestrian does not spawn this episode.")]
        [Range(0f, 1f)] public float noPedestrianChance = 0.2f;

        [Tooltip("Probability that pedestrian waits at the curb without crossing.")]
        [Range(0f, 1f)] public float noCrossChance = 0.15f;

        private Vector3 targetPosition;
        private bool isWalking;
        private bool hasCrossed;
        private bool triggered;
        private bool goingRight;
        private bool willCross;
        private bool isPresent;
        private float walkSpeed;
        private float triggerDistance;
        private Vector3 walkDirection;
        private Vector3 crossingCenter;

        public bool IsDetectable => isPresent;
        public bool IsOnRoad => isWalking && Mathf.Abs(transform.position.z - roadCenterZ) < roadHalfWidth;
        public bool HasCrossed => hasCrossed;
        public bool IsWalking => isWalking;
        public bool IsPresent => isPresent;
        public float NormalizedSpeed => isWalking ? walkSpeed / maxWalkSpeed : 0f;
        public Vector3 WalkDirection => walkDirection;

        private void Start()
        {
            if (carTransform == null)
            {
                var carObj = GameObject.FindWithTag("Car");
                if (carObj != null) carTransform = carObj.transform;
            }
        }

        private void FixedUpdate()
        {
            if (!isPresent) return;

            // Wait at curb until vehicle approaches trigger distance
            if (!triggered && !isWalking && !hasCrossed && carTransform != null)
            {
                float carDist = Mathf.Abs(carTransform.position.x - crossingCenter.x);
                if (carDist < triggerDistance)
                {
                    triggered = true;
                    if (willCross) isWalking = true;
                }
                return;
            }

            if (!isWalking) return;

            transform.position = Vector3.MoveTowards(
                transform.position, targetPosition, walkSpeed * Time.fixedDeltaTime);

            Vector3 toTarget = targetPosition - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                walkDirection = toTarget.normalized;
                transform.forward = walkDirection;
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isWalking = false;
                hasCrossed = true;
                walkDirection = Vector3.zero;
            }
        }

        public void ResetPedestrian()
        {
            isWalking = false;
            hasCrossed = false;
            triggered = false;
            walkDirection = Vector3.zero;

            isPresent = Random.value > noPedestrianChance;
            if (!isPresent)
            {
                transform.position = new Vector3(-100f, -100f, -100f);
                return;
            }

            willCross = Random.value > noCrossChance;
            walkSpeed = Random.Range(minWalkSpeed, maxWalkSpeed);
            triggerDistance = Random.Range(minTriggerDistance, maxTriggerDistance);

            if (spawnLeft != null && spawnRight != null)
                crossingCenter = (spawnLeft.transform.position + spawnRight.transform.position) / 2f;

            goingRight = Random.value > 0.5f;
            if (goingRight && spawnLeft != null)
            {
                transform.position = spawnLeft.transform.position;
                targetPosition = spawnRight != null ? spawnRight.transform.position : transform.position + Vector3.right * roadWidth;
            }
            else if (spawnRight != null)
            {
                transform.position = spawnRight.transform.position;
                targetPosition = spawnLeft != null ? spawnLeft.transform.position : transform.position - Vector3.right * roadWidth;
            }
        }
    }
}
