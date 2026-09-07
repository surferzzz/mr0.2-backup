using System.Collections.Generic;
using System.Linq;
using CPRSimulation.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CPRSimulation.Editor
{
    public static class HospitalEarthquakeSetup
    {
        private const string RootName = "Hospital_Earthquake_Simulation";
        private const string MaterialFolder = "Assets/Materials/Earthquake";

        [MenuItem("Tools/CPR/0.7/Add Hospital Earthquake Simulation", false, -54)]
        public static void AddHospitalEarthquakeSimulation()
        {
            EnsureFolder(MaterialFolder);

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create hospital earthquake simulation");

            EarthquakeController earthquake = root.AddComponent<EarthquakeController>();
            earthquake.targetCamera = FindCameraTarget();
            earthquake.duration = 7.5f;
            // 纯镜头晃动：抖动加在 TrackingSpace（运行时由控制器自动解析），幅度需明显
            earthquake.shakeCamera = true;
            earthquake.amplitudePos = 0.06f;
            earthquake.amplitudeRot = 0.7f;
            earthquake.frequency = 11f;
            // 关闭家具抖动与掉落物，避免出现“意义不明的物体”
            earthquake.shakeVisualTargets = false;
            earthquake.visualShakeTargets = new Transform[0];
            earthquake.enableGravityOnPlay = false;
            earthquake.affectedBodies = new Rigidbody[0];
            earthquake.generateProceduralRumble = true;

            AudioSource rumble = root.AddComponent<AudioSource>();
            rumble.playOnAwake = false;
            rumble.loop = true;
            rumble.volume = 0.35f;
            rumble.spatialBlend = 0.2f;
            earthquake.rumbleLoop = rumble;

            EarthquakeTrainingDirector director = root.AddComponent<EarthquakeTrainingDirector>();
            director.earthquakeController = earthquake;
            director.trainingController = Object.FindObjectOfType<CPRTrainingController>(true);
            director.autoStartCPR = true;
            director.startCPRDelay = 1.5f;

            CreateWorldUi(root.transform, director);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[HospitalEarthquakeSetup] Added earthquake simulation to current CPR scene.");
        }

        private static Transform FindCameraTarget()
        {
            Transform centerEye = Object.FindObjectsOfType<Transform>(true)
                .FirstOrDefault(t => t.name == "CenterEyeAnchor");
            if (centerEye != null)
            {
                return centerEye;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        private static Transform[] FindShakeTargets()
        {
            string[] preferredNames =
            {
                "Hospital_Bed",
                "Wheelchair",
                "IV_Bag_Holder",
                "IV_Bag",
                "Supply_Cabinet_1",
                "Supply_Cabinet_2",
                "Supply_Cabinet_3",
                "Waiting_Table",
                "Waiting_Chair_A",
                "Waiting_Chair_B",
                "Visitor_Bench",
                "Exit_Sign",
                "Ceiling_Light_A",
                "Ceiling_Light_B"
            };

            List<Transform> targets = new List<Transform>();
            foreach (string name in preferredNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    targets.Add(obj.transform);
                }
            }

            return targets.ToArray();
        }

        private static Rigidbody[] CreateFallingProps(Transform parent)
        {
            Material debrisMaterial = CreateMaterial("Earthquake_Debris", new Color(0.62f, 0.66f, 0.65f));
            Material warningMaterial = CreateMaterial("Earthquake_Warning_Yellow", new Color(1f, 0.72f, 0.12f));

            List<Rigidbody> bodies = new List<Rigidbody>();
            bodies.Add(CreateDebrisCube(parent, "Falling_Ceiling_Tile_01", new Vector3(-2.3f, 2.55f, 1.35f), new Vector3(0.95f, 0.045f, 0.55f), debrisMaterial));
            bodies.Add(CreateDebrisCube(parent, "Falling_Ceiling_Tile_02", new Vector3(2.1f, 2.55f, 0.8f), new Vector3(0.85f, 0.045f, 0.48f), debrisMaterial));
            bodies.Add(CreateDebrisCube(parent, "Falling_Wall_Panel_01", new Vector3(-3.35f, 1.95f, 1.1f), new Vector3(0.08f, 0.65f, 0.46f), debrisMaterial));
            bodies.Add(CreateDebrisCube(parent, "Loose_Medical_Box", new Vector3(1.75f, 1.1f, 2.45f), new Vector3(0.32f, 0.22f, 0.24f), warningMaterial));
            bodies.Add(CreateDebrisCube(parent, "Loose_Clipboard", new Vector3(-1.15f, 1.2f, 2.35f), new Vector3(0.36f, 0.025f, 0.26f), debrisMaterial));
            return bodies.ToArray();
        }

        private static Rigidbody CreateDebrisCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(obj, "Create earthquake prop");
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
            obj.transform.localScale = scale;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.45f;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return rb;
        }

        private static ParticleSystem CreateDust(Transform parent)
        {
            GameObject obj = new GameObject("Earthquake_Dust");
            Undo.RegisterCreatedObjectUndo(obj, "Create earthquake dust");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(0f, 0.08f, 0.25f);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new Color(0.55f, 0.54f, 0.50f, 0.28f);
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 75f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6.5f, 0.05f, 5.3f);

            return ps;
        }

        private static void CreateWorldUi(Transform parent, EarthquakeTrainingDirector director)
        {
            // 挂在场景根节点，运行时由 Director 定位到后墙（z=3.5，朝房间 -Z）
            GameObject canvasObj = new GameObject("Earthquake_World_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create earthquake UI");

            const float boardX = 0f;
            const float boardY = 1.69f;
            const float boardZ = 3.44f;
            canvasObj.transform.position = new Vector3(boardX, boardY, boardZ);
            canvasObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1700f, 960f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.17f, 0.96f);

            Text status = CreateText(panel.transform, "StatusText",
                "地震シミュレーション準備完了\nボタンを押して訓練を開始", new Vector2(0f, 120f),
                new Vector2(1500f, 200f), 52, TextAnchor.MiddleCenter);
            status.fontStyle = FontStyle.Bold;
            status.color = new Color(0.95f, 0.98f, 1f, 1f);

            Button button = CreateButton(panel.transform);

            director.earthquakePanel = canvasObj;
            director.statusText = status;
            director.startEarthquakeButton = button;
            director.whiteboardPosition = new Vector3(boardX, boardY, boardZ);
            director.whiteboardRotationY = 0f;
            director.whiteboardCanvasScale = 0.001f;
        }

        private static Button CreateButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("StartEarthquakeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(560f, 150f);

            Image image = buttonObj.GetComponent<Image>();
            image.color = new Color(0.85f, 0.22f, 0.1f, 0.95f);

            Button button = buttonObj.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.35f, 0.18f, 1f);
            colors.pressedColor = new Color(0.65f, 0.12f, 0.08f, 1f);
            button.colors = colors;

            CreateText(buttonObj.transform, "Text", "地震開始", Vector2.zero, rect.sizeDelta, 40, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text textComponent = obj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = anchor;
            textComponent.color = Color.white;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = 18;
            textComponent.resizeTextMaxSize = fontSize;

            return textComponent;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
                return material;
            }

            Shader shader = Shader.Find("Standard");
            material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
