using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CPRSimulation.EditorTools
{
    /// <summary>
    /// 一次性场景维护：清理开发残留、修正光照模式、烘焙。
    /// 可重复执行（已清理过的项会跳过）。
    /// </summary>
    public static class SceneMaintenance
    {
        private const string ScenePath = "Assets/demo/0.7.unity";

        // 场景里重复的方向光。保留 "Directional Light"（自带 Soft 阴影），
        // 删除 OneClickGameSetup 用 new GameObject("DirectionalLight") 造出来的那盏。
        private const string KeepLightName = "Directional Light";
        private const string DuplicateLightName = "DirectionalLight";

        // 只挂着键盘触发的 bHaptics 测试脚本，Quest 上没有键盘。
        private const string TestObjectName = "GameObject";

        // 已禁用，且带 2 个 Missing Script；实际使用的是 OVRCameraRig。
        private const string DeadCameraRigName = "[BuildingBlock] Camera Rig";

        // HapticSource 的 _clip 为空，Awake() 里 _player.clip = null 会在
        // HapticClipPlayer.cs:319 解引用 value.json 抛 NullReferenceException。
        // 本项目触觉走 bHaptics（GloveHapticController + [bhaptics]），
        // Assets/Scripts 里没有任何代码用 Meta Haptics，此物体是 Building Block 残留。
        private const string DeadHapticsName = "[BuildingBlock] Haptics";

        [MenuItem("CPR/维护/清理场景并烘焙光照")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            Debug.Log($"[SceneMaintenance] 打开 {ScenePath}，根物体 {roots.Length} 个");

            int removed = 0;
            removed += DestroyRoot(roots, DuplicateLightName, "重复的方向光（与保留的那盏角度完全相同）");
            removed += DestroyRoot(roots, TestObjectName, "bHaptics 键盘测试脚本载体");
            removed += DestroyRoot(roots, DeadCameraRigName, "废弃的 BuildingBlock 相机装置（含 Missing Script）");
            removed += DestroyRoot(roots, DeadHapticsName, "空 clip 的 Meta HapticSource，运行时抛 NullReferenceException");

            int retuned = RetuneLights(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneMaintenance] 已保存场景：删除 {removed} 个物体，调整 {retuned} 盏灯");

            Debug.Log("[SceneMaintenance] 开始烘焙光照…");
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);

            int maps = LightmapSettings.lightmaps?.Length ?? 0;
            Debug.Log($"[SceneMaintenance] 烘焙完成：生成 {maps} 张 lightmap");
            Debug.Log("[SceneMaintenance] DONE");
        }

        // ---------------------------------------------------------------------
        // 第二步：修复烘焙质量。
        //   1) 医院 FBX 全部没开 Generate Lightmap UVs，烘焙时 UV 重叠会串光。
        //   2) 点光转 Baked 后只照亮 static 物体；场景没有 Light Probe，
        //      病人和床这类动态物体拿不到烘焙的间接光，会显得暗而平。
        // ---------------------------------------------------------------------

        private const string HospitalFbxRoot = "Assets/ThirdParty/MadduckHospital";
        private const string ProbeGroupName = "Light Probe Group";

        [MenuItem("CPR/维护/修复烘焙质量并重新烘焙")]
        public static void FixBakeQuality()
        {
            int reimported = EnableLightmapUVs();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int probes = EnsureLightProbes(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[SceneMaintenance] FBX 重新导入 {reimported} 个，放置 Light Probe {probes} 个");
            Debug.Log("[SceneMaintenance] 重新烘焙…");
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneMaintenance] DONE");
        }

        // ---------------------------------------------------------------------
        // 动态物体的光照。AED 是运行时从 StreamingAssets 加载的，病人是动画物体，
        // 两者都不在 lightmap 里；而我把两盏顶灯改成 Baked 之后，它们连这两盏的光
        // 都拿不到，只剩 Mixed 方向光 + 环境光 —— 无贴图的塑料在这种光下必然发平。
        // ---------------------------------------------------------------------

        [MenuItem("CPR/维护/修复动态物体光照(AED与病人)")]
        public static void FixDynamicObjectLighting()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int lights = SwitchPointLightsToMixed(scene);
            int probes = EnsureLightProbes(scene);
            bool reflectionAdded = EnsureReflectionProbe(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneMaintenance] 点光转 Mixed {lights} 盏；Light Probe {probes} 个；" +
                      $"Reflection Probe {(reflectionAdded ? "已创建" : "已存在，跳过")}");

            Debug.Log("[SceneMaintenance] 重新烘焙…");
            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneMaintenance] DONE");
        }

        /// <summary>
        /// Baked 点光只照亮标记 static 的物体。改成 Mixed 后静态部分仍走烘焙，
        /// 动态物体则能拿到实时直接光。关掉阴影以控制开销 ——
        /// Quest 3 上 2 盏无阴影点光是可以接受的，带 Soft 阴影就不行。
        /// </summary>
        private static int SwitchPointLightsToMixed(UnityEngine.SceneManagement.Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type != LightType.Point)
                        continue;

                    Debug.Log($"[SceneMaintenance] 「{light.gameObject.name}」 " +
                              $"{light.lightmapBakeType}/{light.shadows} -> Mixed/None");
                    light.lightmapBakeType = LightmapBakeType.Mixed;
                    light.shadows = LightShadows.None;
                    n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Built-in RP 下没有反射探针时，光滑表面只能采样天空盒 ——
        /// 室内物体会反射出室外天空，高光看着很假。
        /// </summary>
        private static bool EnsureReflectionProbe(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<ReflectionProbe>(true) != null)
                {
                    Debug.Log("[SceneMaintenance] 场景已有 Reflection Probe，跳过");
                    return false;
                }
            }

            var go = new GameObject("Reflection Probe");
            go.transform.position = new Vector3(0f, 1.4f, 0.25f);   // 房间中心偏上

            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.size = new Vector3(8.4f, 3.0f, 6.9f);   // 略大于房间 8 x 2.9 x 6.5
            probe.resolution = 128;                       // Quest 3 上 128 足够
            probe.boxProjection = true;                   // 盒投影，室内更贴合
            Debug.Log("[SceneMaintenance] 已创建 Reflection Probe (Baked, 128, box projection)");
            return true;
        }

        private static int EnableLightmapUVs()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { HospitalFbxRoot });
            int n = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || importer.generateSecondaryUV) continue;

                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
                Debug.Log($"[SceneMaintenance] 开启 Lightmap UV：{System.IO.Path.GetFileName(path)}");
                n++;
            }
            return n;
        }

        /// <summary>
        /// 在训练区上方铺一层探针网格。房间尺寸取自 MadduckHospitalSceneBuilder：
        /// 地板中心 (0, -0.03, 0.25)、尺寸 (8, 0.06, 6.5)，即 x∈[-4,4]、z∈[-3,3.5]。
        /// </summary>
        private static int EnsureLightProbes(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<LightProbeGroup>(true) != null)
                {
                    Debug.Log("[SceneMaintenance] 场景已有 Light Probe Group，跳过");
                    return 0;
                }
            }

            var positions = new System.Collections.Generic.List<Vector3>();
            for (float x = -3.5f; x <= 3.5f; x += 1.75f)
            for (float z = -2.5f; z <= 3.0f; z += 1.833f)
            foreach (var y in new[] { 0.35f, 1.2f, 2.4f })
                positions.Add(new Vector3(x, y, z));

            var go = new GameObject(ProbeGroupName);
            go.transform.position = Vector3.zero;
            var group = go.AddComponent<LightProbeGroup>();
            group.probePositions = positions.ToArray();
            Debug.Log($"[SceneMaintenance] 已创建「{ProbeGroupName}」，探针 {positions.Count} 个");
            return positions.Count;
        }

        private static int DestroyRoot(GameObject[] roots, string name, string why)
        {
            var hits = roots.Where(g => g != null && g.name == name).ToList();
            if (hits.Count == 0)
            {
                Debug.Log($"[SceneMaintenance] 跳过「{name}」：场景中不存在（可能已清理）");
                return 0;
            }

            foreach (var go in hits)
            {
                Debug.Log($"[SceneMaintenance] 删除「{go.name}」— {why}");
                Object.DestroyImmediate(go);
            }
            return hits.Count;
        }

        /// <summary>
        /// 4 盏灯原本全是 Baked，但项目从未烘焙过，等于完全不发光。
        /// 场景里没有 Light Probe，所以主方向光必须是 Mixed，
        /// 否则病人、床等动态物体拿不到任何直接光。
        /// </summary>
        private static int RetuneLights(UnityEngine.SceneManagement.Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    var want = light.type == LightType.Directional
                        ? LightmapBakeType.Mixed
                        : LightmapBakeType.Baked;

                    if (light.lightmapBakeType == want) continue;

                    Debug.Log($"[SceneMaintenance] 「{light.gameObject.name}」({light.type}) " +
                              $"{light.lightmapBakeType} -> {want}");
                    light.lightmapBakeType = want;
                    n++;
                }
            }
            return n;
        }
    }
}
