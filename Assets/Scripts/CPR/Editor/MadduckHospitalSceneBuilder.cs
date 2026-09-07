using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CPRSimulation.Editor
{
    public static class MadduckHospitalSceneBuilder
    {
        private const string RootName = "Madduck_Hospital_Training_Room";
        private const string AssetRoot = "Assets/ThirdParty/MadduckHospital";
        private const string FbxRoot = AssetRoot + "/FBX";
        private const string TextureRoot = AssetRoot + "/Hospital_assets/textures";
        private const string MaterialFolder = AssetRoot + "/Materials";
        private const string PrimaryScenePath = "Assets/0.7/mr0.7.unity";

        [MenuItem("Tools/CPR/0.7/Open 0.7 And Build Madduck Hospital Room", false, -58)]
        public static void OpenPrimarySceneAndBuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!File.Exists(PrimaryScenePath))
            {
                EditorUtility.DisplayDialog("Madduck hospital room", "Cannot find " + PrimaryScenePath, "OK");
                return;
            }

            EditorSceneManager.OpenScene(PrimaryScenePath, OpenSceneMode.Single);
            BuildInCurrentScene();
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("Tools/CPR/0.7/Build Madduck Hospital Room In Current Scene", false, -57)]
        public static void BuildInCurrentScene()
        {
            if (!Directory.Exists(FbxRoot) || !Directory.Exists(TextureRoot))
            {
                EditorUtility.DisplayDialog(
                    "Madduck hospital room",
                    "Missing Madduck assets. Expected:\n" + FbxRoot + "\n" + TextureRoot,
                    "OK");
                return;
            }

            AssetDatabase.Refresh();
            PrepareImportSettings();
            EnsureAssetFolder(MaterialFolder);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Madduck hospital room");

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            SceneMaterials materials = CreateMaterials();
            Vector3 center = ResolveTrainingCenter();
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Madduck hospital room");
            root.transform.position = center;

            HideSimpleGroundIfPresent();
            BuildRoomShell(root.transform, materials);
            BuildClinicalProps(root.transform, materials);
            BuildSignsAndLights(root.transform, materials);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();

            Debug.Log("Madduck hospital training room built around the CPR training area.");
        }

        [MenuItem("Tools/CPR/0.7/Fix Madduck Hospital Materials In Current Scene", false, -56)]
        public static void FixMaterialsInCurrentScene()
        {
            EnsureAssetFolder(MaterialFolder);
            CreateMaterials();
            DeleteGeneratedText("Room_Title_Text");
            DeleteGeneratedText("AED_Label_Text");
            DeleteGeneratedText("Procedure_Text");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Madduck hospital materials fixed for the active render pipeline.");
        }

        private static void PrepareImportSettings()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { FbxRoot });
            foreach (string guid in modelGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.meshCompression != ModelImporterMeshCompression.Medium)
                {
                    importer.meshCompression = ModelImporterMeshCompression.Medium;
                    changed = true;
                }

                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    changed = true;
                }

                if (importer.importAnimation)
                {
                    importer.importAnimation = false;
                    changed = true;
                }

                if (importer.importCameras)
                {
                    importer.importCameras = false;
                    changed = true;
                }

                if (importer.importLights)
                {
                    importer.importLights = false;
                    changed = true;
                }

                if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot });
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                string lower = path.ToLowerInvariant();
                bool normal = lower.Contains("_normal");
                bool color = lower.Contains("basecolor") || lower.Contains("base_color");

                bool changed = false;
                TextureImporterType textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != textureType)
                {
                    importer.textureType = textureType;
                    changed = true;
                }

                if (importer.maxTextureSize != 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }

                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    changed = true;
                }

                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    changed = true;
                }

                if (importer.sRGBTexture != color)
                {
                    importer.sRGBTexture = color;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static SceneMaterials CreateMaterials()
        {
            return new SceneMaterials
            {
                Bed = CreateMaterial("Madduck_Bed", "bed", new Color(0.82f, 0.86f, 0.88f), 0.35f),
                Ceiling = CreateMaterial("Madduck_Ceiling", "ceiling_1", new Color(0.78f, 0.82f, 0.82f), 0.42f, new Vector2(4f, 4f)),
                CeilingLight = CreateMaterial("Madduck_CeilingLight", "Ceiling_light", new Color(1f, 0.96f, 0.82f), 0.25f),
                ChairTable = CreateMaterial("Madduck_ChairsTable", "Chairs_table_1", new Color(0.45f, 0.48f, 0.50f), 0.38f),
                Door = CreateMaterial("Madduck_Door", "Door_1", new Color(0.78f, 0.80f, 0.78f), 0.33f),
                Doorway = CreateMaterial("Madduck_Doorway", "Doorway_1", new Color(0.64f, 0.68f, 0.69f), 0.32f),
                Exit = CreateMaterial("Madduck_ExitSign", "exit_sign", new Color(0.1f, 0.8f, 0.35f), 0.20f),
                Floor = CreateMaterial("Madduck_Floor", "Floors_1", new Color(0.58f, 0.62f, 0.62f), 0.35f, new Vector2(6f, 6f)),
                IVBag = CreateMaterial("Madduck_IVBag", "Iv_bag", new Color(0.85f, 0.95f, 1f), 0.18f),
                IVPole = CreateMaterial("Madduck_IVPole", "Iv_pole", new Color(0.72f, 0.74f, 0.74f), 0.18f),
                Magazine = CreateMaterial("Madduck_Magazine", "Magazine", new Color(0.85f, 0.80f, 0.65f), 0.45f),
                Wall = CreateMaterial("Madduck_Wall", "Walls_1", new Color(0.78f, 0.86f, 0.88f), 0.45f, new Vector2(4f, 2f)),
                Wheelchair = CreateMaterial("Madduck_Wheelchair", "wheel_chair", new Color(0.28f, 0.30f, 0.32f), 0.30f),
                Trim = CreatePlainMaterial("Madduck_Trim", new Color(0.53f, 0.61f, 0.64f), 0.35f),
                Mat = CreatePlainMaterial("Madduck_CPR_Mat", new Color(0.06f, 0.16f, 0.20f), 0.55f),
                Warning = CreatePlainMaterial("Madduck_WarningYellow", new Color(1f, 0.75f, 0.1f), 0.45f),
                SignWhite = CreatePlainMaterial("Madduck_SignWhite", new Color(0.94f, 0.96f, 0.95f), 0.55f),
                TextDark = CreatePlainMaterial("Madduck_TextDark", new Color(0.04f, 0.08f, 0.10f), 0.55f)
            };
        }

        private static Material CreateMaterial(string name, string textureFolder, Color fallback, float smoothness, Vector2? tiling = null)
        {
            Material material = LoadOrCreateMaterial(name);
            material.color = fallback;
            SetSmoothness(material, smoothness);

            Texture2D baseMap = FindTexture(textureFolder, "basecolor", "base_color", "base color");
            Texture2D normal = FindTexture(textureFolder, "_normal");
            Texture2D metallic = FindTexture(textureFolder, "_metallic");

            if (baseMap != null)
            {
                SetTexture(material, "_BaseMap", "_MainTex", baseMap);
            }

            if (normal != null)
            {
                SetTexture(material, "_BumpMap", "_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (metallic != null)
            {
                SetTexture(material, "_MetallicGlossMap", "_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                SetFloat(material, "_Metallic", "_Metallic", 0.35f);
            }

            if (tiling.HasValue)
            {
                SetTextureScale(material, "_BaseMap", "_MainTex", tiling.Value);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePlainMaterial(string name, Color color, float smoothness)
        {
            Material material = LoadOrCreateMaterial(name);
            material.color = color;
            SetSmoothness(material, smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = FindLitShader();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Shader FindLitShader()
        {
            Shader shader = null;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader;
        }

        private static Texture2D FindTexture(string folderName, params string[] tokens)
        {
            string folder = TextureRoot + "/" + folderName;
            if (!Directory.Exists(folder))
            {
                return null;
            }

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string lower = file.Replace("\\", "/").ToLowerInvariant();
                if (!lower.EndsWith(".png") && !lower.EndsWith(".jpg") && !lower.EndsWith(".jpeg") && !lower.EndsWith(".tga"))
                {
                    continue;
                }

                foreach (string token in tokens)
                {
                    string loweredToken = token.ToLowerInvariant();
                    if (loweredToken.Contains("normal") && lower.Contains("directx"))
                    {
                        continue;
                    }

                    if (lower.Contains(loweredToken))
                    {
                        return AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace("\\", "/"));
                    }
                }
            }

            return null;
        }

        private static void BuildRoomShell(Transform root, SceneMaterials materials)
        {
            CreateCube("Hospital_Floor", root, new Vector3(0f, -0.03f, 0.25f), new Vector3(8f, 0.06f, 6.5f), materials.Floor, true);
            CreateCube("Hospital_Back_Wall", root, new Vector3(0f, 1.45f, 3.5f), new Vector3(8f, 2.9f, 0.12f), materials.Wall, true);
            CreateCube("Hospital_Left_Wall", root, new Vector3(-4f, 1.45f, 0.25f), new Vector3(0.12f, 2.9f, 6.5f), materials.Wall, true);
            CreateCube("Hospital_Right_Wall", root, new Vector3(4f, 1.45f, 0.25f), new Vector3(0.12f, 2.9f, 6.5f), materials.Wall, true);
            CreateCube("Hospital_Ceiling", root, new Vector3(0f, 2.9f, 0.25f), new Vector3(8f, 0.08f, 6.5f), materials.Ceiling, true);
            CreateCube("Entrance_Left_Return", root, new Vector3(-3f, 1.35f, -3f), new Vector3(2f, 2.7f, 0.12f), materials.Wall, true);
            CreateCube("Entrance_Right_Return", root, new Vector3(3f, 1.35f, -3f), new Vector3(2f, 2.7f, 0.12f), materials.Wall, true);
            CreateCube("Entrance_Header", root, new Vector3(0f, 2.55f, -3f), new Vector3(2f, 0.35f, 0.12f), materials.Wall, true);

            CreateCube("CPR_Training_Mat", root, new Vector3(0f, 0.012f, 0f), new Vector3(2.2f, 0.024f, 2.5f), materials.Mat, false);
            CreateCube("CPR_Mat_Left_Stripe", root, new Vector3(-1.14f, 0.028f, 0f), new Vector3(0.06f, 0.012f, 2.55f), materials.Warning, false);
            CreateCube("CPR_Mat_Right_Stripe", root, new Vector3(1.14f, 0.028f, 0f), new Vector3(0.06f, 0.012f, 2.55f), materials.Warning, false);
            CreateCube("Back_Wall_Trim", root, new Vector3(0f, 0.1f, 3.42f), new Vector3(7.8f, 0.2f, 0.06f), materials.Trim, true);
            CreateCube("Left_Wall_Trim", root, new Vector3(-3.92f, 0.1f, 0.25f), new Vector3(0.06f, 0.2f, 6.3f), materials.Trim, true);
            CreateCube("Right_Wall_Trim", root, new Vector3(3.92f, 0.1f, 0.25f), new Vector3(0.06f, 0.2f, 6.3f), materials.Trim, true);
        }

        private static void BuildClinicalProps(Transform root, SceneMaterials materials)
        {
            SpawnModel("bed.fbx", "Hospital_Bed", root, new Vector3(2.3f, 0f, 1.55f), new Vector3(0f, 180f, 0f), 2.3f, materials.Bed, true, true);
            SpawnModel("wheel_chair.fbx", "Wheelchair", root, new Vector3(-3.1f, 0f, -1.85f), new Vector3(0f, 35f, 0f), 1.2f, materials.Wheelchair, true, true);
            SpawnModel("IV_Bag_holder.fbx", "IV_Bag_Holder", root, new Vector3(-1.35f, 0f, 0.85f), new Vector3(0f, 0f, 0f), 1.8f, materials.IVPole, true, true);
            SpawnModel("IV_Bag.fbx", "IV_Bag", root, new Vector3(-1.38f, 1.48f, 0.87f), new Vector3(0f, 0f, 0f), 0.35f, materials.IVBag, false, true);
            SpawnModel("cabinet_1.fbx", "Supply_Cabinet_1", root, new Vector3(-3.35f, 0f, 2.65f), new Vector3(0f, 90f, 0f), 1.65f, materials.Doorway, true, true);
            SpawnModel("cabinet_2.fbx", "Supply_Cabinet_2", root, new Vector3(-2.55f, 0f, 2.65f), new Vector3(0f, 90f, 0f), 1.5f, materials.Doorway, true, true);
            SpawnModel("cabinet_3.fbx", "Supply_Cabinet_3", root, new Vector3(-1.8f, 0f, 2.65f), new Vector3(0f, 90f, 0f), 1.45f, materials.Doorway, true, true);
            SpawnModel("table.fbx", "Waiting_Table", root, new Vector3(2.75f, 0f, -1.85f), new Vector3(0f, 20f, 0f), 1.15f, materials.ChairTable, true, true);
            SpawnModel("chair.fbx", "Waiting_Chair_A", root, new Vector3(2.05f, 0f, -2.25f), new Vector3(0f, -20f, 0f), 0.9f, materials.ChairTable, true, true);
            SpawnModel("chair.fbx", "Waiting_Chair_B", root, new Vector3(3.4f, 0f, -2.1f), new Vector3(0f, 25f, 0f), 0.9f, materials.ChairTable, true, true);
            SpawnModel("bench.fbx", "Visitor_Bench", root, new Vector3(3.25f, 0f, 1.9f), new Vector3(0f, -90f, 0f), 1.7f, materials.ChairTable, true, true);
            SpawnModel("Magazine1.fbx", "Waiting_Magazine", root, new Vector3(2.75f, 0.58f, -1.85f), new Vector3(0f, 35f, 0f), 0.45f, materials.Magazine, false, false);
            SpawnModel("door_1.fbx", "Hospital_Door_Left", root, new Vector3(-0.55f, 0f, -3.04f), new Vector3(0f, 0f, 0f), 1.9f, materials.Door, true, true);
            SpawnModel("door_2.fbx", "Hospital_Door_Right", root, new Vector3(0.55f, 0f, -3.04f), new Vector3(0f, 180f, 0f), 1.9f, materials.Door, true, true);
        }

        private static void BuildSignsAndLights(Transform root, SceneMaterials materials)
        {
            SpawnModel("Exit_sign.fbx", "Exit_Sign", root, new Vector3(0f, 2.28f, -2.92f), new Vector3(0f, 180f, 0f), 0.8f, materials.Exit, false, false);
            SpawnModel("ceiling_light.fbx", "Ceiling_Light_A", root, new Vector3(-1.8f, 2.78f, 0.5f), new Vector3(0f, 0f, 0f), 1.1f, materials.CeilingLight, false, false);
            SpawnModel("ceiling_light.fbx", "Ceiling_Light_B", root, new Vector3(1.8f, 2.78f, 0.5f), new Vector3(0f, 0f, 0f), 1.1f, materials.CeilingLight, false, false);

            CreateCube("Room_Title_Backplate", root, new Vector3(0f, 2.2f, 3.42f), new Vector3(2.8f, 0.45f, 0.04f), materials.SignWhite, false);

            CreateCube("AED_Label_Backplate", root, new Vector3(-3.38f, 1.45f, -0.65f), new Vector3(0.04f, 0.6f, 0.85f), materials.SignWhite, false);

            CreateCube("Procedure_Backplate", root, new Vector3(3.38f, 1.55f, 0.45f), new Vector3(0.04f, 1.05f, 1.15f), materials.SignWhite, false);

            CreatePointLight("Warm_Clinical_Light_A", root, new Vector3(-1.8f, 2.55f, 0.5f), 1.4f, 5f);
            CreatePointLight("Warm_Clinical_Light_B", root, new Vector3(1.8f, 2.55f, 0.5f), 1.4f, 5f);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;
            obj.isStatic = true;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!collider)
            {
                Object.DestroyImmediate(obj.GetComponent<Collider>());
            }

            return obj;
        }

        private static GameObject SpawnModel(
            string fileName,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEuler,
            float targetMaxDimension,
            Material material,
            bool alignToFloor,
            bool addCollider)
        {
            string path = FbxRoot + "/" + fileName;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("Missing Madduck model: " + path);
                return null;
            }

            GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (obj == null)
            {
                obj = Object.Instantiate(prefab);
            }

            Undo.RegisterCreatedObjectUndo(obj, "Create " + objectName);
            obj.name = objectName;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.Euler(localEuler);
            obj.transform.localScale = Vector3.one;

            AssignMaterialRecursive(obj, material);
            FitToMaxDimension(obj, targetMaxDimension);

            if (alignToFloor)
            {
                AlignBottomToWorldY(obj, parent.position.y);
            }

            if (addCollider)
            {
                AddApproximateBoxCollider(obj);
            }

            SetStaticRecursive(obj);
            return obj;
        }

        private static void FitToMaxDimension(GameObject obj, float targetMaxDimension)
        {
            if (targetMaxDimension <= 0f || !TryGetWorldRenderBounds(obj, out Bounds bounds))
            {
                return;
            }

            float max = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (max <= 0.001f)
            {
                return;
            }

            float factor = targetMaxDimension / max;
            obj.transform.localScale *= factor;
        }

        private static void AlignBottomToWorldY(GameObject obj, float floorY)
        {
            if (!TryGetWorldRenderBounds(obj, out Bounds bounds))
            {
                return;
            }

            obj.transform.position += Vector3.up * (floorY - bounds.min.y);
        }

        private static void AssignMaterialRecursive(GameObject obj, Material material)
        {
            if (material == null)
            {
                return;
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    sharedMaterials[i] = material;
                }

                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static void AddApproximateBoxCollider(GameObject obj)
        {
            if (obj.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            if (!TryGetLocalRenderBounds(obj.transform, out Bounds localBounds))
            {
                return;
            }

            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = localBounds.size;
        }

        private static bool TryGetWorldRenderBounds(GameObject obj, out Bounds bounds)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(obj.transform.position, Vector3.zero);
            bool found = false;

            foreach (Renderer renderer in renderers)
            {
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private static bool TryGetLocalRenderBounds(Transform target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool found = false;

            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 local = target.InverseTransformPoint(corner);
                    if (!found)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return found;
        }

        private static void CreateWorldText(string name, Transform parent, string text, Vector3 localPosition, Quaternion localRotation, float characterSize, Material material)
        {
            GameObject obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = characterSize;
            mesh.fontSize = 96;
            mesh.color = material != null ? material.color : Color.black;

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetFontMaterial(mesh.font, mesh.color);
            }
        }

        private static Material GetFontMaterial(Font font, Color color)
        {
            string path = MaterialFolder + "/Madduck_TextMesh_Font.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Transparent");
                }

                material = new Material(shader)
                {
                    name = "Madduck_TextMesh_Font"
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (font != null && font.material != null && font.material.mainTexture != null)
            {
                material.mainTexture = font.material.mainTexture;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePointLight(string name, Transform parent, Vector3 localPosition, float intensity, float range)
        {
            GameObject obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            Light light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static Vector3 ResolveTrainingCenter()
        {
            GameObject chestDetector = GameObject.Find("CPR_ChestDetector");
            if (chestDetector != null)
            {
                Vector3 pos = chestDetector.transform.position;
                return new Vector3(pos.x, 0f, pos.z);
            }

            string[] patientNames =
            {
                "Standing React Death Backward",
                "XBot_Patient",
                "Patient",
                "Remy@Laying Seizure",
                "X Bot"
            };

            foreach (string patientName in patientNames)
            {
                GameObject patient = GameObject.Find(patientName);
                if (patient != null)
                {
                    Vector3 pos = patient.transform.position;
                    return new Vector3(pos.x, 0f, pos.z);
                }
            }

            return new Vector3(0f, 0f, 1f);
        }

        private static void HideSimpleGroundIfPresent()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null || ground.transform.parent != null)
            {
                return;
            }

            MeshFilter filter = ground.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            string meshName = filter.sharedMesh.name.ToLowerInvariant();
            if (!meshName.Contains("plane") && !meshName.Contains("quad"))
            {
                return;
            }

            Undo.RecordObject(ground, "Hide simple ground");
            ground.SetActive(false);
        }

        private static void SetStaticRecursive(GameObject obj)
        {
            obj.isStatic = true;
            foreach (Transform child in obj.transform)
            {
                SetStaticRecursive(child.gameObject);
            }
        }

        private static void DeleteGeneratedText(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private static void SetTexture(Material material, string urpProperty, string standardProperty, Texture texture)
        {
            if (material.HasProperty(urpProperty))
            {
                material.SetTexture(urpProperty, texture);
            }
            else if (material.HasProperty(standardProperty))
            {
                material.SetTexture(standardProperty, texture);
            }
        }

        private static void SetTextureScale(Material material, string urpProperty, string standardProperty, Vector2 scale)
        {
            if (material.HasProperty(urpProperty))
            {
                material.SetTextureScale(urpProperty, scale);
            }

            if (material.HasProperty(standardProperty))
            {
                material.SetTextureScale(standardProperty, scale);
            }
        }

        private static void SetFloat(Material material, string urpProperty, string standardProperty, float value)
        {
            if (material.HasProperty(urpProperty))
            {
                material.SetFloat(urpProperty, value);
            }
            else if (material.HasProperty(standardProperty))
            {
                material.SetFloat(standardProperty, value);
            }
        }

        private static void SetSmoothness(Material material, float value)
        {
            SetFloat(material, "_Smoothness", "_Glossiness", value);
        }

        private static void EnsureAssetFolder(string folderPath)
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

        private sealed class SceneMaterials
        {
            public Material Bed;
            public Material Ceiling;
            public Material CeilingLight;
            public Material ChairTable;
            public Material Door;
            public Material Doorway;
            public Material Exit;
            public Material Floor;
            public Material IVBag;
            public Material IVPole;
            public Material Magazine;
            public Material Wall;
            public Material Wheelchair;
            public Material Trim;
            public Material Mat;
            public Material Warning;
            public Material SignWhite;
            public Material TextDark;
        }
    }
}
