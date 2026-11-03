using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutonomousDriving.Core.Vehicle;
using AutonomousDriving.Core.Camera;
using AutonomousDriving.PedestrianSafety;

namespace AutonomousDriving.UI
{
    /// <summary>
    /// Runtime procedural UI telemetry HUD. Generates Canvas and TextMeshPro displays
    /// showing live vehicle speed, RL cumulative rewards, collision counts, yield rates,
    /// and pedestrian detection status.
    /// </summary>
    public class TrainingHUD : MonoBehaviour
    {
        [Header("System Dependencies")]
        public SimpleCarController carController;
        public PedestrianController pedestrian;
        public EpisodeController episodeController;
        public RewardManager rewardManager;
        public CameraManager cameraManager;

        private Canvas canvas;
        private TextMeshProUGUI speedText;
        private TextMeshProUGUI episodeText;
        private TextMeshProUGUI rewardText;
        private TextMeshProUGUI agentStateText;
        private TextMeshProUGUI pedStateText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI cameraModeText;

        private int totalEpisodes;
        private int totalCollisions;
        private int totalYields;
        private int totalGoals;
        private float cumulativeReward;
        private bool lastFrameTerminal;

        private static readonly Color PanelColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        private static readonly Color AccentGreen = new Color(0.3f, 0.9f, 0.4f);
        private static readonly Color AccentYellow = new Color(1f, 0.85f, 0.2f);
        private static readonly Color AccentRed = new Color(1f, 0.35f, 0.3f);
        private static readonly Color AccentBlue = new Color(0.4f, 0.7f, 1f);
        private static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f);

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            UpdateHUD();
            TrackStats();
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("TrainingHUD_Canvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Top-left: Vehicle Telemetry Panel
            var vehiclePanel = CreatePanel(canvasObj, "VehiclePanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(280, 120));
            speedText = CreateLabel(vehiclePanel, "SpeedText", "Speed: 0 km/h", new Vector2(15, -10), 22, AccentGreen);
            agentStateText = CreateLabel(vehiclePanel, "AgentState", "Agent: Idle", new Vector2(15, -40), 18, TextWhite);
            rewardText = CreateLabel(vehiclePanel, "RewardText", "Reward: 0.000", new Vector2(15, -65), 18, AccentYellow);
            cameraModeText = CreateLabel(vehiclePanel, "CameraMode", "Camera: Follow [1/2/3]", new Vector2(15, -90), 16, AccentBlue);

            // Top-right: Statistics Panel
            var statsPanel = CreatePanel(canvasObj, "StatsPanel",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-290, -10), new Vector2(280, 160));
            episodeText = CreateLabel(statsPanel, "EpisodeText", "Episode: 0", new Vector2(15, -10), 22, AccentBlue);
            statsText = CreateLabel(statsPanel, "StatsText", "Goals: 0 | Collisions: 0
Yield Rate: --
Collision Rate: --",
                new Vector2(15, -40), 17, TextWhite);

            // Bottom-center: Pedestrian Status Panel
            var pedPanel = CreatePanel(canvasObj, "PedPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 10), new Vector2(240, 50));
            pedStateText = CreateLabel(pedPanel, "PedState", "Pedestrian: --", new Vector2(15, -10), 18, AccentYellow);
        }

        private RectTransform CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = panel.AddComponent<Image>();
            img.color = PanelColor;
            return rt;
        }

        private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string defaultText, Vector2 pos, int fontSize, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-30, 30);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void UpdateHUD()
        {
            if (carController == null) return;

            float displaySpeed = carController.GetSpeed() * 3.6f;
            speedText.text = $"Speed: {displaySpeed:F0} km/h";
            speedText.color = displaySpeed < 5f ? AccentRed : AccentGreen;

            float normSpeed = carController.NormalizedSpeed;
            string actionLabel = normSpeed < 0.01f ? "STOPPED" : (normSpeed > 0.5f ? "ACCELERATING" : "CRUISING");
            Color actionColor = normSpeed < 0.01f ? AccentRed : (normSpeed > 0.5f ? AccentGreen : AccentYellow);
            agentStateText.text = $"Agent: {actionLabel}";
            agentStateText.color = actionColor;

            if (rewardManager != null)
            {
                cumulativeReward += rewardManager.LastStepReward;
                rewardText.text = $"Reward: {cumulativeReward:F3}";
                rewardText.color = cumulativeReward >= 0 ? AccentGreen : AccentRed;
            }

            if (cameraManager != null)
            {
                cameraModeText.text = $"Camera: {cameraManager.CurrentMode} [1/2/3]";
            }

            episodeText.text = $"Episode: {totalEpisodes}";

            float yieldRate = totalEpisodes > 0 ? (float)totalYields / totalEpisodes * 100f : 0f;
            float collisionRate = totalEpisodes > 0 ? (float)totalCollisions / totalEpisodes * 100f : 0f;
            float goalRate = totalEpisodes > 0 ? (float)totalGoals / totalEpisodes * 100f : 0f;

            statsText.text = $"Goals: {totalGoals} | Collisions: {totalCollisions}
" +
                             $"Yield Rate: {yieldRate:F1}%
" +
                             $"Goal Rate: {goalRate:F1}% | Collision Rate: {collisionRate:F1}%";

            if (pedestrian != null)
            {
                string pedLabel = !pedestrian.IsPresent ? "NOT PRESENT" : (pedestrian.HasCrossed ? "CROSSED" : (pedestrian.IsWalking ? "CROSSING" : "WAITING"));
                Color pedColor = !pedestrian.IsPresent ? new Color(0.6f, 0.6f, 0.6f) : (pedestrian.HasCrossed ? AccentGreen : (pedestrian.IsWalking ? AccentRed : AccentYellow));
                pedStateText.text = $"Pedestrian: {pedLabel}";
                pedStateText.color = pedColor;
            }
        }

        private void TrackStats()
        {
            if (episodeController == null || rewardManager == null) return;

            bool isTerminalNow = rewardManager.IsTerminal;

            if (isTerminalNow && !lastFrameTerminal)
            {
                totalEpisodes++;

                if (episodeController.CarHitPedestrian) totalCollisions++;
                else if (episodeController.CarReachedGoal) totalGoals++;

                if (rewardManager.DidYield) totalYields++;

                cumulativeReward = 0f;
            }

            lastFrameTerminal = isTerminalNow;
        }
    }
}
