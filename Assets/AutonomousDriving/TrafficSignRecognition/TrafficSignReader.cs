using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_SENTIS
using Unity.Sentis;
#else
using Unity.InferenceEngine;
#endif

namespace AutonomousDriving.TrafficSignRecognition
{
    /// <summary>
    /// Real-time German Traffic Sign Recognition Benchmark (GTSRB) 43-Class Neural Classifier.
    /// Operates using Unity Sentis / InferenceEngine with dynamic screen-space ROI extraction.
    /// </summary>
    public class TrafficSignReader : MonoBehaviour
    {
        [Header("Neural Network Asset")]
        public ModelAsset trafficSignModel;
        private Worker engine;

        [Header("User Interface Telemetry")]
        public TextMeshProUGUI resultText;
        public RawImage aiCameraDisplay;

        [Header("Synthetic YOLO / ROI Tracking")]
        public Camera vehicleCamera;
        public string signTag = "Tabela";
        public float detectionDistance = 30f;

        [Range(100f, 5000f)]
        public float boxScaleMultiplier = 500f;

        [Header("Inference Specifications")]
        public int inputResolution = 30; // 30x30 pixels (GTSRB standard)
        public int totalClasses = 43;

        [Range(0.1f, 1f)]
        public float confidenceThreshold = 0.70f;

        private static readonly string[] SignLabels = new string[]
        {
            "Speed Limit (20 km/h)", "Speed Limit (30 km/h)", "Speed Limit (50 km/h)", "Speed Limit (60 km/h)",
            "Speed Limit (70 km/h)", "Speed Limit (80 km/h)", "End of Speed Limit (80 km/h)", "Speed Limit (100 km/h)",
            "Speed Limit (120 km/h)", "No Passing", "No Passing for Trucks", "Right-of-Way at Next Intersection",
            "Priority Road", "Yield", "Stop", "No Vehicles Allowed",
            "Vehicles > 3.5t Prohibited", "No Entry", "General Caution", "Dangerous Curve to the Left",
            "Dangerous Curve to the Right", "Double Curve", "Bumpy Road", "Slippery Road",
            "Road Narrows on the Right", "Road Works", "Traffic Signals", "Pedestrians",
            "Children Crossing", "Bicycles Crossing", "Beware of Ice/Snow", "Wild Animals Crossing",
            "End of All Speed and Passing Limits", "Turn Right Ahead", "Turn Left Ahead",
            "Ahead Only or Turn Right", "Ahead Only or Turn Left", "Keep Right", "Keep Left",
            "Roundabout Mandatory", "End of Minimum Speed", "End of Minimum Speed", "End of All Restrictions"
        };

        private void Start()
        {
            if (trafficSignModel != null)
            {
                Model runtimeModel = ModelLoader.Load(trafficSignModel);
                engine = new Worker(runtimeModel, BackendType.GPUCompute);

                if (resultText != null) resultText.text = "AI SYSTEM INITIALIZING...";
                StartCoroutine(CaptureAndInferRoutine());
            }
            else
            {
                Debug.LogWarning($"[{name}] No Traffic Sign ModelAsset assigned!");
            }
        }

        private IEnumerator CaptureAndInferRoutine()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                if (vehicleCamera == null || engine == null) continue;

                var signs = GameObject.FindGameObjectsWithTag(signTag);
                GameObject closestSign = null;
                float minDistance = detectionDistance;

                foreach (var s in signs)
                {
                    float dist = Vector3.Distance(vehicleCamera.transform.position, s.transform.position);
                    if (dist < minDistance)
                    {
                        Vector3 screenPt = vehicleCamera.WorldToScreenPoint(s.transform.position);
                        if (screenPt.z > 0 && screenPt.x > 0 && screenPt.x < Screen.width && screenPt.y > 0 && screenPt.y < Screen.height)
                        {
                            minDistance = dist;
                            closestSign = s;
                        }
                    }
                }

                if (closestSign == null)
                {
                    if (resultText != null)
                    {
                        resultText.text = "SCANNING...";
                        resultText.color = Color.yellow;
                    }
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // Compute dynamic screen-space bounding box
                Vector3 targetScreenPos = vehicleCamera.WorldToScreenPoint(closestSign.transform.position);
                int dynamicBoxSize = Mathf.RoundToInt(boxScaleMultiplier / targetScreenPos.z);
                dynamicBoxSize = Mathf.Clamp(dynamicBoxSize, 50, Screen.height - 10);

                float x = Mathf.Clamp(targetScreenPos.x - (dynamicBoxSize / 2f), 0, Screen.width - dynamicBoxSize);
                float y = Mathf.Clamp(targetScreenPos.y - (dynamicBoxSize / 2f), 0, Screen.height - dynamicBoxSize);

                Rect roiRect = new Rect(x, y, dynamicBoxSize, dynamicBoxSize);

                // Extract ROI pixel buffer
                Texture2D screenCapture = new Texture2D(dynamicBoxSize, dynamicBoxSize, TextureFormat.RGB24, false);
                screenCapture.ReadPixels(roiRect, 0, 0);
                screenCapture.Apply();

                if (aiCameraDisplay != null)
                {
                    var oldTex = aiCameraDisplay.texture as Texture2D;
                    aiCameraDisplay.texture = screenCapture;
                    if (oldTex != null) Destroy(oldTex);
                }

                // Prepare Tensor input (NHWC layout, 30x30x3)
                TextureTransform transformSettings = new TextureTransform()
                    .SetDimensions(inputResolution, inputResolution, 3)
                    .SetTensorLayout(TensorLayout.NHWC);

                using (Tensor<float> inputTensor = TextureConverter.ToTensor(screenCapture, transformSettings))
                {
                    engine.Schedule(inputTensor);

                    var outputTensor = engine.PeekOutput() as Tensor<float>;
                    float[] outputArray = outputTensor.DownloadToArray();

                    float maxConfidence = 0f;
                    int predictedClass = -1;

                    for (int i = 0; i < Mathf.Min(totalClasses, outputArray.Length); i++)
                    {
                        if (outputArray[i] > maxConfidence)
                        {
                            maxConfidence = outputArray[i];
                            predictedClass = i;
                        }
                    }

                    if (maxConfidence >= confidenceThreshold && predictedClass >= 0 && predictedClass < SignLabels.Length)
                    {
                        if (resultText != null)
                        {
                            resultText.text = $"DETECTED: {SignLabels[predictedClass]} ({maxConfidence * 100f:F0}%)";
                            resultText.color = Color.green;
                        }
                    }
                    else
                    {
                        if (resultText != null)
                        {
                            resultText.text = "ANALYZING TARGET...";
                            resultText.color = new Color(1f, 0.5f, 0f);
                        }
                    }
                }

                if (aiCameraDisplay == null) Destroy(screenCapture);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnDisable()
        {
            engine?.Dispose();
        }
    }
}
