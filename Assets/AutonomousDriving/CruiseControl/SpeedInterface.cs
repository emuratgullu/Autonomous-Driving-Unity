using UnityEngine;
using TMPro;

namespace AutonomousDriving.CruiseControl
{
    /// <summary>
    /// UI telemetry component displaying lead car speed in km/h.
    /// </summary>
    public class SpeedInterface : MonoBehaviour
    {
        [Header("UI References")]
        public LeadCarController leadCarController;
        public TextMeshProUGUI speedText;

        private void Update()
        {
            if (speedText == null) return;

            if (leadCarController != null)
            {
                float speedKmh = leadCarController.currentSpeed * 3.6f;
                speedText.text = $"Lead Car Speed: {speedKmh:F1} km/h";
            }
            else
            {
                speedText.text = "Lead Car Speed: N/A";
            }
        }
    }
}
