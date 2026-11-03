using UnityEngine;

namespace AutonomousDriving.Environment
{
    /// <summary>
    /// Procedurally builds realistic simulation environments including grass planes,
    /// randomized 3D trees, roadway traffic signs, and emissive street light poles.
    /// </summary>
    public class EnvironmentBuilder : MonoBehaviour
    {
        [Header("Road Layout")]
        public float roadStartX = 0f;
        public float roadEndX = 100f;
        public float roadCenterZ = 0f;
        public float roadHalfWidth = 3f;

        [Header("Crosswalk Position")]
        public float crosswalkX = 40f;

        [Header("Vegetation")]
        public int treeCount = 12;

        [Header("Color Palette")]
        public Color trunkColor = new Color(0.45f, 0.28f, 0.12f);
        public Color foliageColor = new Color(0.2f, 0.6f, 0.15f);
        public Color signColor = new Color(0.85f, 0.85f, 0.85f);
        public Color poleColor = new Color(0.5f, 0.5f, 0.5f);
        public Color grassColor = new Color(0.3f, 0.55f, 0.2f);
        public Color yieldSignColor = new Color(1f, 0.2f, 0.2f);

        private GameObject envParent;

        private void Start()
        {
            BuildEnvironment();
        }

        public void BuildEnvironment()
        {
            if (envParent != null) Destroy(envParent);

            envParent = new GameObject("ProceduralEnvironment");
            envParent.transform.SetParent(transform);

            BuildGrassPlanes();
            BuildTrees();
            BuildTrafficSigns();
            BuildLightPoles();
        }

        private void BuildGrassPlanes()
        {
            var grassParent = new GameObject("GrassPlanes");
            grassParent.transform.SetParent(envParent.transform);

            float roadLength = roadEndX - roadStartX;
            float centerX = (roadStartX + roadEndX) / 2f;
            float grassWidth = 15f;

            CreateFlatQuad(grassParent, "GrassLeft",
                new Vector3(centerX, 0.01f, roadCenterZ - roadHalfWidth - 1f - grassWidth / 2f),
                new Vector3(roadLength + 20f, 1f, grassWidth), grassColor);

            CreateFlatQuad(grassParent, "GrassRight",
                new Vector3(centerX, 0.01f, roadCenterZ + roadHalfWidth + 1f + grassWidth / 2f),
                new Vector3(roadLength + 20f, 1f, grassWidth), grassColor);
        }

        private void BuildTrees()
        {
            var treeParent = new GameObject("Trees");
            treeParent.transform.SetParent(envParent.transform);

            float sidewalkEdge = roadHalfWidth + 2.5f;

            for (int i = 0; i < treeCount; i++)
            {
                float x = roadStartX + 5f + i * ((roadEndX - roadStartX - 10f) / (treeCount - 1));
                if (Mathf.Abs(x - crosswalkX) < 8f) continue; // Clear crosswalk sightline

                float z = (i % 2 == 0)
                    ? roadCenterZ - sidewalkEdge - Random.Range(1f, 3f)
                    : roadCenterZ + sidewalkEdge + Random.Range(1f, 3f);

                float trunkHeight = Random.Range(2.5f, 4f);
                float foliageRadius = Random.Range(1.2f, 2f);

                CreateTree(treeParent, $"Tree_{i}", new Vector3(x, 0f, z), trunkHeight, foliageRadius);
            }
        }

        private void CreateTree(GameObject parent, string name, Vector3 basePos, float trunkHeight, float foliageRadius)
        {
            var tree = new GameObject(name);
            tree.transform.SetParent(parent.transform);
            tree.transform.position = basePos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, trunkHeight / 2f, 0);
            trunk.transform.localScale = new Vector3(0.3f, trunkHeight / 2f, 0.3f);
            Destroy(trunk.GetComponent<Collider>());
            SetMaterial(trunk, trunkColor);

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform);
            foliage.transform.localPosition = new Vector3(0, trunkHeight + foliageRadius * 0.6f, 0);
            foliage.transform.localScale = Vector3.one * foliageRadius * 2f;
            Destroy(foliage.GetComponent<Collider>());
            SetMaterial(foliage, foliageColor);
        }

        private void BuildTrafficSigns()
        {
            var signsParent = new GameObject("TrafficSigns");
            signsParent.transform.SetParent(envParent.transform);

            float signZ = roadCenterZ - roadHalfWidth - 1.5f;

            CreateSign(signsParent, "YieldSign", new Vector3(crosswalkX - 8f, 0f, signZ), yieldSignColor, SignShape.Triangle);
            CreateSign(signsParent, "CrosswalkSign", new Vector3(crosswalkX, 0f, signZ), signColor, SignShape.Diamond);
            CreateSign(signsParent, "SpeedSign", new Vector3(roadStartX + 10f, 0f, signZ), signColor, SignShape.Square);
        }

        private enum SignShape { Square, Diamond, Triangle }

        private void CreateSign(GameObject parent, string name, Vector3 basePos, Color color, SignShape shape)
        {
            var sign = new GameObject(name);
            sign.transform.SetParent(parent.transform);
            sign.transform.position = basePos;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(sign.transform);
            pole.transform.localPosition = new Vector3(0, 1.5f, 0);
            pole.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
            Destroy(pole.GetComponent<Collider>());
            SetMaterial(pole, poleColor);

            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            face.transform.SetParent(sign.transform);
            face.transform.localPosition = new Vector3(0, 3.2f, 0);

            switch (shape)
            {
                case SignShape.Diamond:
                    face.transform.localScale = new Vector3(0.8f, 0.8f, 0.08f);
                    face.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                    break;
                case SignShape.Triangle:
                    face.transform.localScale = new Vector3(0.9f, 0.8f, 0.08f);
                    break;
                default:
                    face.transform.localScale = new Vector3(0.7f, 0.7f, 0.08f);
                    break;
            }

            Destroy(face.GetComponent<Collider>());
            SetMaterial(face, color);
        }

        private void BuildLightPoles()
        {
            var polesParent = new GameObject("LightPoles");
            polesParent.transform.SetParent(envParent.transform);

            float poleZ = roadCenterZ + roadHalfWidth + 1.5f;
            float[] polePositions = { 15f, 40f, 65f, 90f };

            for (int i = 0; i < polePositions.Length; i++)
            {
                CreateLightPole(polesParent, $"LightPole_{i}", new Vector3(polePositions[i], 0f, poleZ));
            }
        }

        private void CreateLightPole(GameObject parent, string name, Vector3 basePos)
        {
            var lp = new GameObject(name);
            lp.transform.SetParent(parent.transform);
            lp.transform.position = basePos;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lp.transform);
            pole.transform.localPosition = new Vector3(0, 3f, 0);
            pole.transform.localScale = new Vector3(0.12f, 3f, 0.12f);
            Destroy(pole.GetComponent<Collider>());
            SetMaterial(pole, poleColor);

            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "Arm";
            arm.transform.SetParent(lp.transform);
            arm.transform.localPosition = new Vector3(0, 6f, -1f);
            arm.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            arm.transform.localScale = new Vector3(0.08f, 1f, 0.08f);
            Destroy(arm.GetComponent<Collider>());
            SetMaterial(arm, poleColor);

            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lp.transform);
            bulb.transform.localPosition = new Vector3(0, 5.9f, -2f);
            bulb.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
            Destroy(bulb.GetComponent<Collider>());

            var rend = bulb.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var bulbMat = new Material(shader);
            bulbMat.color = new Color(1f, 0.95f, 0.8f);
            bulbMat.EnableKeyword("_EMISSION");
            bulbMat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.8f) * 2f);
            rend.material = bulbMat;
        }

        private void SetMaterial(GameObject obj, Color color)
        {
            var rend = obj.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            rend.material = mat;
        }

        private void CreateFlatQuad(GameObject parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent.transform);
            quad.transform.position = pos;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = scale;
            Destroy(quad.GetComponent<Collider>());

            SetMaterial(quad, color);
        }
    }
}
