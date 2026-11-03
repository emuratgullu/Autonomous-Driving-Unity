using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using AutonomousDriving.Core.Vehicle;

namespace AutonomousDriving.LaneKeeping
{
    /// <summary>
    /// Discrete Action Space Lane Keeping Agent using 4 CollidingWheel sensor probes
    /// and WheelCollider vehicle substep configuration.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DiscreteLaneAgent : Agent
    {
        [Header("Steering Dynamics")]
        public float maxAngle = 30f;
        public float maxTorque = 50f;
        public float brakeTorque = 30000f;
        public float wheelAngleDelta = 0.25f;

        [Header("Physics Substep Optimization")]
        public float criticalSpeed = 5f;
        public int stepsBelow = 5;
        public int stepsAbove = 1;

        [Header("Lane Geometry")]
        public float constantTorque = 50f;
        public float roadGuideOffset = 15f;
        public float laneWidth = 5f;
        public float maxRPM = 100f;

        [Header("Termination Target")]
        public GameObject endBox;

        private WheelCollider[] wheels;
        private CollidingWheel frontDriver;
        private CollidingWheel frontPassenger;
        private CollidingWheel rearDriver;
        private CollidingWheel rearPassenger;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody rb;

        private float wheelAngle = 0.0f;
        private float frontDistanceToCenter = 0.0f;
        private float backDistanceToCenter = 0.0f;
        private float timer = 0.0f;

        public override void Initialize()
        {
            wheels = GetComponentsInChildren<WheelCollider>();
            rb = GetComponent<Rigidbody>();

            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;

            foreach (var wheel in wheels)
            {
                var sensors = wheel.GetComponentsInChildren<CollidingWheel>();
                foreach (var s in sensors)
                {
                    if (s.name.Contains("frontDriver")) frontDriver = s;
                    else if (s.name.Contains("frontPassenger")) frontPassenger = s;
                    else if (s.name.Contains("rearDriver")) rearDriver = s;
                    else if (s.name.Contains("rearPassenger")) rearPassenger = s;
                }
            }
        }

        public override void OnEpisodeBegin()
        {
            timer = 0.0f;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
            wheelAngle = 0.0f;

            foreach (var w in wheels)
            {
                w.steerAngle = 0f;
                w.brakeTorque = 0f;
                w.motorTorque = 0f;
            }

            frontDistanceToCenter = 0f;
            backDistanceToCenter = 0f;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(wheelAngle / maxAngle);
            sensor.AddObservation(frontDistanceToCenter);
            sensor.AddObservation(backDistanceToCenter);
#if UNITY_6000_0_OR_NEWER
            sensor.AddObservation(rb.linearVelocity.x);
            sensor.AddObservation(rb.linearVelocity.z);
#else
            sensor.AddObservation(rb.velocity.x);
            sensor.AddObservation(rb.velocity.z);
#endif
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (wheels != null && wheels.Length > 0)
            {
                wheels[0].ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);
            }

            int discreteAction = actions.DiscreteActions[0];
            if (discreteAction == 1 && wheelAngle < maxAngle) wheelAngle += wheelAngleDelta;
            if (discreteAction == 2 && wheelAngle > -maxAngle) wheelAngle -= wheelAngleDelta;

            if (frontDriver != null && frontPassenger != null)
                frontDistanceToCenter = ((frontDriver.distanceToMarker - laneWidth) - frontPassenger.distanceToMarker) / roadGuideOffset;

            if (rearDriver != null && rearPassenger != null)
                backDistanceToCenter = ((rearDriver.distanceToMarker - laneWidth) - rearPassenger.distanceToMarker) / roadGuideOffset;

            foreach (var wheel in wheels)
            {
                if (wheel.transform.localPosition.z > 0) wheel.steerAngle = wheelAngle;
                wheel.motorTorque = (wheels[0].rpm < maxRPM) ? constantTorque : 0f;
            }

            float frontDistAbs = Mathf.Abs(frontDistanceToCenter);
            float backDistAbs = Mathf.Abs(backDistanceToCenter);

            if (Mathf.Abs(wheelAngle / maxAngle) < 0.5f) AddReward(0.1f);

            if (frontDistAbs < 0.2f && backDistAbs < 0.2f)
            {
                AddReward((1f - frontDistAbs) * 0.25f + (1f - backDistAbs) * 0.25f);
            }
            else
            {
                float penalty = -(frontDistAbs * 0.25f + backDistAbs * 0.25f);
                if (frontDistanceToCenter < 0f || backDistanceToCenter < 0f) penalty *= 2f;
                AddReward(penalty);
            }

            timer += Time.fixedDeltaTime;

#if UNITY_6000_0_OR_NEWER
            if (rb.linearVelocity.sqrMagnitude < 0.01f && timer > 60f)
#else
            if (rb.velocity.sqrMagnitude < 0.01f && timer > 60f)
#endif
            {
                SetReward(-1.0f);
                EndEpisode();
                return;
            }

            if (frontDistAbs > 0.99f || backDistAbs > 0.99f || transform.position.y > 5.0f || transform.position.y < 0.0f)
            {
                SetReward(-1.0f);
                EndEpisode();
                return;
            }

            if (endBox != null)
            {
                float distanceToEnd = Vector3.Distance(endBox.transform.localPosition, transform.localPosition);
                if (distanceToEnd < 10.0f)
                {
                    SetReward(1.0f);
                    EndEpisode();
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discrete = actionsOut.DiscreteActions;
            float horizontal = Input.GetAxis("Horizontal");
            discrete[0] = horizontal > 0.1f ? 1 : (horizontal < -0.1f ? 2 : 0);
        }
    }
}
