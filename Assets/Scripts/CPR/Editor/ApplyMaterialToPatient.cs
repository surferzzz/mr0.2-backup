using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 为患者模型设置材质和颜色（替换默认白色）
    /// </summary>
    public class ApplyMaterialToPatient
    {
        [MenuItem("Tools/CPR/一键设置患者（颜色+动画+防掉落）", false, 32)]
        public static void CompletePatientSetup()
        {
            var patient = GameObject.Find("Standing React Death Backward") 
                ?? GameObject.Find("Patient") 
                ?? GameObject.Find("XBot_Patient");

            if (patient == null)
            {
                EditorUtility.DisplayDialog("未找到患者", "请先把 Standing React Death Backward.fbx 拖到场景中。", "确定");
                return;
            }

            // 1. 创建 Mixamo 风格的材质（肤色+衣服）
            var skinMat = CreateMixamoSkinMaterial();
            var clothMat = CreateMixamoClothMaterial();

            // 2. 应用材质到渲染器
            var skinnedRenderers = patient.GetComponentsInChildren<SkinnedMeshRenderer>();
            int rendererCount = 0;
            
            foreach (var renderer in skinnedRenderers)
            {
                Undo.RecordObject(renderer, "Apply Mixamo Materials");
                
                // 根据网格名称智能分配材质
                string meshName = renderer.sharedMesh != null ? renderer.sharedMesh.name.ToLower() : "";
                
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    // 如果是身体/皮肤部分用肤色，衣服部分用衣服色
                    if (meshName.Contains("body") || meshName.Contains("skin") || meshName.Contains("head") || meshName.Contains("arm") || meshName.Contains("leg"))
                        mats[i] = skinMat;
                    else
                        mats[i] = clothMat;
                }
                renderer.sharedMaterials = mats;
                rendererCount++;
            }

            // 3. 添加/配置 Animator
            var animator = patient.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(patient);
            }

            // 从 FBX 获取 Avatar
            const string fbxPath = "Assets/00/Standing React Death Backward.fbx";
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var avatar = System.Array.Find(allAssets, a => a is Avatar) as Avatar;
            if (avatar != null && animator.avatar == null)
            {
                animator.avatar = avatar;
            }

            // 禁用 Root Motion（防止掉地下）
            animator.applyRootMotion = false;

            // 4. 分配倒地动画控制器
            AutoAnimatorSetup.SetupStandingReactDeathBackward();

            // 5. 添加 Rigidbody 并禁用重力（防止掉地下）
            var rb = patient.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(patient);
            }
            rb.useGravity = false;
            rb.isKinematic = true;

            // 6. 调整位置
            if (patient.transform.position.magnitude < 0.1f)
            {
                patient.transform.position = new Vector3(0, 0, 2f);
            }
            patient.transform.rotation = Quaternion.Euler(0, 180, 0);

            EditorUtility.DisplayDialog("完成", 
                $"已为 {patient.name} 完成设置：\n\n" +
                $"✓ 已设置 Mixamo 风格材质（肤色+衣服）\n" +
                $"✓ 已配置倒地动画控制器\n" +
                $"✓ 已禁用重力（防止掉地下）\n" +
                $"✓ 已应用到 {rendererCount} 个渲染器\n\n" +
                "现在运行游戏，点击「开始」按钮即可看到倒地效果。", "确定");

            Selection.activeGameObject = patient;
        }

        private static Material CreateMixamoSkinMaterial()
        {
            string path = "Assets/Materials/MixamoSkin.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "MixamoSkin";
            
            // Mixamo 默认肤色（偏粉的浅肤色）
            mat.color = new Color(0.96f, 0.82f, 0.76f, 1f);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.4f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateMixamoClothMaterial()
        {
            string path = "Assets/Materials/MixamoCloth.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "MixamoCloth";
            
            // 深蓝灰色（类似运动服）
            mat.color = new Color(0.25f, 0.3f, 0.35f, 1f);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.2f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        [MenuItem("Tools/CPR/为患者设置皮肤颜色（自动）", false, 33)]
        public static void ApplySkinMaterial()
        {
            // 查找患者模型
            var patient = GameObject.Find("Standing React Death Backward") 
                ?? GameObject.Find("Patient") 
                ?? GameObject.Find("XBot_Patient");

            if (patient == null)
            {
                EditorUtility.DisplayDialog("未找到患者", "请在场景中放置患者模型（Standing React Death Backward / Patient）。", "确定");
                return;
            }

            // 获取所有 SkinnedMeshRenderer 和 MeshRenderer
            var skinnedRenderers = patient.GetComponentsInChildren<SkinnedMeshRenderer>();
            var meshRenderers = patient.GetComponentsInChildren<MeshRenderer>();

            if (skinnedRenderers.Length == 0 && meshRenderers.Length == 0)
            {
                EditorUtility.DisplayDialog("未找到渲染器", patient.name + " 上没有 Renderer 组件。", "确定");
                return;
            }

            // 尝试加载现有的皮肤材质
            Material skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/model mixamo/materials/SimpleSkin.mat");
            
            // 如果没有现成材质，创建一个
            if (skinMat == null)
            {
                skinMat = CreateSkinMaterial();
            }

            int count = 0;

            // 应用到所有 SkinnedMeshRenderer
            foreach (var renderer in skinnedRenderers)
            {
                Undo.RecordObject(renderer, "Apply Skin Material");
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = skinMat;
                }
                renderer.sharedMaterials = mats;
                count++;
            }

            // 应用到所有 MeshRenderer
            foreach (var renderer in meshRenderers)
            {
                Undo.RecordObject(renderer, "Apply Skin Material");
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = skinMat;
                }
                renderer.sharedMaterials = mats;
                count++;
            }

            EditorUtility.DisplayDialog("完成", 
                $"已为 {patient.name} 设置皮肤材质。\n\n" +
                $"• 材质: {AssetDatabase.GetAssetPath(skinMat)}\n" +
                $"• 已应用到 {count} 个渲染器\n" +
                $"• 颜色: 肤色（可在 Inspector 里调整）", "确定");

            Selection.activeGameObject = patient;
        }

        private static Material CreateSkinMaterial()
        {
            // 创建皮肤材质（肤色）
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            var mat = new Material(shader);
            mat.name = "PatientSkin";

            // 设置肤色（浅棕色）
            mat.color = new Color(0.95f, 0.8f, 0.7f, 1f);

            // 如果是 URP Lit，设置一些基础属性
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.3f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            // 保存材质
            string folder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            string path = folder + "/PatientSkin.mat";
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();

            Debug.Log("[CPR] 已创建皮肤材质: " + path);
            return mat;
        }

        [MenuItem("Tools/CPR/为患者设置自定义颜色", false, 33)]
        public static void ApplyCustomColor()
        {
            var patient = GameObject.Find("Standing React Death Backward") 
                ?? GameObject.Find("Patient") 
                ?? GameObject.Find("XBot_Patient");

            if (patient == null)
            {
                EditorUtility.DisplayDialog("未找到患者", "请在场景中放置患者模型。", "确定");
                return;
            }

            // 让用户选择颜色
            Color currentColor = new Color(0.95f, 0.8f, 0.7f, 1f);
            var renderers = patient.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length > 0 && renderers[0].sharedMaterial != null)
            {
                currentColor = renderers[0].sharedMaterial.color;
            }

            // 注意：EditorUtility.ColorField 需要在 OnGUI 里调用，这里简化为预设颜色选择
            var choice = EditorUtility.DisplayDialogComplex("选择颜色", 
                "为患者选择材质颜色：", 
                "肤色（浅棕）", "蓝色（医护服）", "灰色");

            Color selectedColor = choice switch
            {
                0 => new Color(0.95f, 0.8f, 0.7f, 1f),  // 肤色
                1 => new Color(0.3f, 0.5f, 0.8f, 1f),   // 蓝色
                _ => new Color(0.6f, 0.6f, 0.6f, 1f)    // 灰色
            };

            ApplyColorToPatient(patient, selectedColor);

            EditorUtility.DisplayDialog("完成", $"已为 {patient.name} 设置颜色。", "确定");
        }

        private static void ApplyColorToPatient(GameObject patient, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.color = color;

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.3f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            var skinnedRenderers = patient.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in skinnedRenderers)
            {
                Undo.RecordObject(renderer, "Apply Color");
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }
                renderer.sharedMaterials = mats;
            }

            var meshRenderers = patient.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                Undo.RecordObject(renderer, "Apply Color");
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }
                renderer.sharedMaterials = mats;
            }
        }
    }
}
