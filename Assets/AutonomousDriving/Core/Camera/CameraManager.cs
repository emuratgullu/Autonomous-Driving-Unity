using UnityEngine;
using UnityEngine.InputSystem;

namespace AutonomousDriving.Core.Camera
{
    public enum CameraMode
    {
        Follow,
        BirdEye,
        FixedSide
    }

    /// <summary>
    /// Multi-perspective camera orchestrator supporting dynamic switching
    /// between Chase Cam, Top-Down Bird's Eye, and Fixed Sidewalk Angle.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("Target Reference")]
        public Transform target;

        [Header("Mode: Follow")]
        public Vector3 followOffset = new Vector3(-10f, 8f, 0f);
        public float followSmooth = 5f;

        [Header("Mode: Bird Eye")]
        public Vector3 birdEyePosition = new Vector3(50f, 40f, 0f);

        [Header("Mode: Fixed Side")]
        public Vector3 fixedSidePosition = new Vector3(40f, 5f, -20f);

        private CameraMode currentMode = CameraMode.Follow;

        public CameraMode CurrentMode => currentMode;

        private void LateUpdate()
        {
            if (target == null) return;

            // Keyboard switching: 1 / 2 / 3
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) currentMode = CameraMode.Follow;
                if (kb.digit2Key.wasPressedThisFrame) currentMode = CameraMode.BirdEye;
                if (kb.digit3Key.wasPressedThisFrame) currentMode = CameraMode.FixedSide;
            }

            switch (currentMode)
            {
                case CameraMode.Follow:
                    Vector3 desired = target.position + followOffset;
                    transform.position = Vector3.Lerp(transform.position, desired, followSmooth * Time.deltaTime);
                    transform.LookAt(target.position + Vector3.up * 1f);
                    break;

                case CameraMode.BirdEye:
                    transform.position = birdEyePosition;
                    transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    break;

                case CameraMode.FixedSide:
                    transform.position = fixedSidePosition;
                    transform.LookAt(target);
                    break;
            }
        }

        public void SetMode(CameraMode mode)
        {
            currentMode = mode;
        }

        public void CycleMode()
        {
            currentMode = (CameraMode)(((int)currentMode + 1) % 3);
        }
    }
}
