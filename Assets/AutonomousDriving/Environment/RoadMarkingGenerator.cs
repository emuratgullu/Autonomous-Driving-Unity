using UnityEngine;

namespace AutonomousDriving.Environment
{
    /// <summary>
    /// Procedurally generates road pavement markings including zebra crosswalks,
    /// dashed center lane dividers, solid edge boundaries, and approach stop lines.
    /// </summary>
    public class RoadMarkingGenerator : MonoBehaviour
    {
        [Header("Road Dimensions")]
        public float roadStartX = 0f;
        public float roadEndX = 100f;
        public float roadCenterZ = 0f;
        public float roadHalfWidth = 3f;
        public float roadY = 0.07f;

        [Header("Crosswalk Specifications")]
        public float crosswalkX = 40f;
        public float crosswalkWidth = 6f;
        public int zebraStripeCount = 8;

        [Header("Lane Markings")]
        public float dashLength = 3f;
        public float dashGap = 4f;
        public float lineWidth = 0.15f;

        [Header("Colors")]
        public Color whiteMarkingColor = Color.white;
        public Color yellowEdgeColor = new Color(1f, 0.85f, 0f);

        private GameObject markingsParent;

        private void Start()
        {
            GenerateMarkings();
        }

        public void GenerateMarkings()
        {
            if (markingsParent != null) Destroy(markingsParent);

            markingsParent = new GameObject("RoadMarkings");
            markingsParent.transform.SetParent(transform);

            GenerateZebraCrossing();
            GenerateCenterLine();
            GenerateEdgeLines();
            GenerateStopLine();
        }

        private void GenerateZebraCrossing()
        {
            var zebraParent = new GameObject("ZebraCrossing");
            zebraParent.transform.SetParent(markingsParent.transform);

            float stripeWidth = (roadHalfWidth * 2f) / zebraStripeCount;
            float stripeLength = crosswalkWidth * 0.8f;

            for (int i = 0; i < zebraStripeCount; i++)
            {
                if (i % 2 != 0) continue; // Alternate stripes

                float z = roadCenterZ - roadHalfWidth + stripeWidth * i + stripeWidth * 0.5f;
                var stripe = CreateQuad($"Stripe_{i}",
                    new Vector3(crosswalkX, roadY, z),
                    new Vector3(stripeLength, 1f, stripeWidth * 0.85f),
                    whiteMarkingColor);
                stripe.transform.SetParent(zebraParent.transform);
            }
        }

        private void GenerateCenterLine()
        {
            var centerParent = new GameObject("CenterLine");
            centerParent.transform.SetParent(markingsParent.transform);

            float x = roadStartX;
            int dashIndex = 0;

            while (x < roadEndX)
            {
                // Skip crosswalk zone
                if (x > crosswalkX - crosswalkWidth / 2f - 1f && x < crosswalkX + crosswalkWidth / 2f + 1f)
                {
                    x += dashLength + dashGap;
                    continue;
                }

                var dash = CreateQuad($"CenterDash_{dashIndex}",
                    new Vector3(x + dashLength / 2f, roadY, roadCenterZ),
                    new Vector3(dashLength, 1f, lineWidth),
                    whiteMarkingColor);
                dash.transform.SetParent(centerParent.transform);

                x += dashLength + dashGap;
                dashIndex++;
            }
        }

        private void GenerateEdgeLines()
        {
            var edgeParent = new GameObject("EdgeLines");
            edgeParent.transform.SetParent(markingsParent.transform);

            float lineLength = roadEndX - roadStartX;
            float centerX = (roadStartX + roadEndX) / 2f;

            var leftEdge = CreateQuad("LeftEdge",
                new Vector3(centerX, roadY, roadCenterZ - roadHalfWidth + lineWidth / 2f),
                new Vector3(lineLength, 1f, lineWidth),
                yellowEdgeColor);
            leftEdge.transform.SetParent(edgeParent.transform);

            var rightEdge = CreateQuad("RightEdge",
                new Vector3(centerX, roadY, roadCenterZ + roadHalfWidth - lineWidth / 2f),
                new Vector3(lineLength, 1f, lineWidth),
                yellowEdgeColor);
            rightEdge.transform.SetParent(edgeParent.transform);
        }

        private void GenerateStopLine()
        {
            float stopLineX = crosswalkX - crosswalkWidth / 2f - 0.5f;
            var stopLine = CreateQuad("StopLine",
                new Vector3(stopLineX, roadY, roadCenterZ),
                new Vector3(0.3f, 1f, roadHalfWidth * 2f),
                whiteMarkingColor);
            stopLine.transform.SetParent(markingsParent.transform);
        }

        private GameObject CreateQuad(string name, Vector3 position, Vector3 scale, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.position = position;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = scale;
            Destroy(quad.GetComponent<Collider>());

            var rend = quad.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { color = color };
            rend.material = mat;

            return quad;
        }
    }
}
