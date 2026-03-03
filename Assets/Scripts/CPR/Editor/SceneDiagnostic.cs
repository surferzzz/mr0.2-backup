using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 场景诊断和一键修复工具
    /// </summary>
    public class SceneDiagnostic : EditorWindow
    {
        private Vector2 scrollPos;
        private List<DiagnosticResult> results = new List<DiagnosticResult>();
        private bool diagnosed = false;

        [MenuItem("Tools/CPR/★ 一键诊断并修复场景 ★", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneDiagnostic>("场景诊断修复");
            window.minSize = new Vector2(500, 400);
            window.RunDiagnostic();
        }

        [MenuItem("Tools/CPR/快速修复/修复所有问题", false, 100)]
        public static void QuickFixAll()
        {
            Debug.Log("========== 开始一键修复 ==========");
            
            int fixCount = 0;
            
            // 1. 修复地面位置
            fixCount += FixGroundPosition();
            
            // 2. 修复角色位置
            fixCount += FixCharacterPosition();
            
            // 3. 修复Animator设置
            fixCount += FixAnimatorSettings();
            
            // 4. 禁用有问题的Oculus组件
            fixCount += DisableProblematicOculusComponents();
            
            // 5. 清理缺失脚本
            fixCount += CleanupMissingScripts();
            
            Debug.Log($"========== 修复完成: 共修复 {fixCount} 个问题 ==========");
            EditorUtility.DisplayDialog("修复完成", $"共修复了 {fixCount} 个问题！\n\n请点击 Play 测试效果。", "确定");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 重新诊断", GUILayout.Height(35)))
            {
                RunDiagnostic();
            }
            if (GUILayout.Button("🔧 一键修复所有", GUILayout.Height(35)))
            {
                QuickFixAll();
                RunDiagnostic();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("诊断结果:", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            if (!diagnosed)
            {
                EditorGUILayout.HelpBox("点击 '重新诊断' 开始检查场景问题", MessageType.Info);
            }
            else if (results.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ 没有发现问题！场景配置正确。", MessageType.Info);
            }
            else
            {
                foreach (var result in results)
                {
                    DrawDiagnosticResult(result);
                }
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("单独修复选项:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("修复地面"))
            {
                FixGroundPosition();
                RunDiagnostic();
            }
            if (GUILayout.Button("修复角色"))
            {
                FixCharacterPosition();
                RunDiagnostic();
            }
            if (GUILayout.Button("修复动画"))
            {
                FixAnimatorSettings();
                RunDiagnostic();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("禁用Oculus错误"))
            {
                DisableProblematicOculusComponents();
                RunDiagnostic();
            }
            if (GUILayout.Button("清理缺失脚本"))
            {
                CleanupMissingScripts();
                RunDiagnostic();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiagnosticResult(DiagnosticResult result)
        {
            var bgColor = result.severity == Severity.Error ? new Color(1, 0.3f, 0.3f, 0.3f) :
                         result.severity == Severity.Warning ? new Color(1, 0.8f, 0.2f, 0.3f) :
                         new Color(0.2f, 0.8f, 0.2f, 0.3f);
            
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;
            
            string icon = result.severity == Severity.Error ? "❌" :
                         result.severity == Severity.Warning ? "⚠️" : "ℹ️";
            
            EditorGUILayout.LabelField($"{icon} {result.title}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(result.description, EditorStyles.wordWrappedLabel);
            
            if (!string.IsNullOrEmpty(result.solution))
            {
                EditorGUILayout.LabelField($"解决方案: {result.solution}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void RunDiagnostic()
        {
            results.Clear();
            diagnosed = true;
            
            // 检查地面
            CheckGround();
            
            // 检查角色
            CheckCharacter();
            
            // 检查Animator
            CheckAnimator();
            
            // 检查Oculus组件
            CheckOculusComponents();
            
            // 检查缺失脚本
            CheckMissingScripts();
            
            Repaint();
        }

        private void CheckGround()
        {
            var grounds = Object.FindObjectsOfType<Transform>()
                .Where(t => t.name.ToLower().Contains("ground") || 
                           t.name.ToLower().Contains("floor") ||
                           t.name.ToLower().Contains("plane"))
                .ToList();

            if (grounds.Count == 0)
            {
                results.Add(new DiagnosticResult
                {
                    severity = Severity.Warning,
                    title = "没有找到地面",
                    description = "场景中没有名为 Ground/Floor/Plane 的对象",
                    solution = "创建一个地面对象"
                });
                return;
            }

            foreach (var ground in grounds)
            {
                if (Mathf.Abs(ground.position.y) > 0.1f)
                {
                    results.Add(new DiagnosticResult
                    {
                        severity = Severity.Error,
                        title = $"地面位置不正确: {ground.name}",
                        description = $"当前 Y = {ground.position.y:F2}，应该为 0",
                        solution = "将地面 Position Y 设为 0"
                    });
                }
            }
        }

        private void CheckCharacter()
        {
            var characters = Object.FindObjectsOfType<Animator>()
                .Where(a => a.avatar != null && a.avatar.isHuman)
                .Select(a => a.transform)
                .ToList();

            // 也查找特定名称的角色
            var namedChars = Object.FindObjectsOfType<Transform>()
                .Where(t => t.name.ToLower().Contains("death") ||
                           t.name.ToLower().Contains("remy") ||
                           t.name.ToLower().Contains("patient") ||
                           t.name.ToLower().Contains("character"))
                .ToList();

            var allChars = characters.Union(namedChars).Distinct().ToList();

            if (allChars.Count == 0)
            {
                results.Add(new DiagnosticResult
                {
                    severity = Severity.Info,
                    title = "没有找到角色",
                    description = "场景中没有人形角色",
                    solution = "添加角色模型到场景"
                });
                return;
            }

            foreach (var character in allChars)
            {
                // 检查位置
                if (character.position.y < -0.5f)
                {
                    results.Add(new DiagnosticResult
                    {
                        severity = Severity.Error,
                        title = $"角色在地面以下: {character.name}",
                        description = $"当前 Y = {character.position.y:F2}",
                        solution = "将角色 Position Y 设为 0 或更高"
                    });
                }

                // 检查Animator
                var animator = character.GetComponent<Animator>();
                if (animator != null)
                {
                    if (animator.applyRootMotion)
                    {
                        results.Add(new DiagnosticResult
                        {
                            severity = Severity.Warning,
                            title = $"Root Motion 已启用: {character.name}",
                            description = "Apply Root Motion 会导致角色位置被动画控制，可能掉到地下",
                            solution = "取消勾选 Apply Root Motion"
                        });
                    }

                    if (animator.runtimeAnimatorController == null)
                    {
                        results.Add(new DiagnosticResult
                        {
                            severity = Severity.Warning,
                            title = $"缺少 Animator Controller: {character.name}",
                            description = "角色没有设置动画控制器",
                            solution = "为角色设置 Animator Controller"
                        });
                    }
                }
            }
        }

        private void CheckAnimator()
        {
            var animators = Object.FindObjectsOfType<Animator>();
            
            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController != null && animator.applyRootMotion)
                {
                    // 已经在 CheckCharacter 中处理
                }
            }
        }

        private void CheckOculusComponents()
        {
            // 查找所有可能有问题的Oculus组件
            var allComponents = Object.FindObjectsOfType<MonoBehaviour>(true);
            int problematicCount = 0;

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                
                string typeName = comp.GetType().FullName ?? "";
                if (typeName.Contains("Oculus") || typeName.Contains("OVR"))
                {
                    if (!comp.gameObject.activeInHierarchy || !comp.enabled)
                        continue;
                    
                    problematicCount++;
                }
            }

            if (problematicCount > 0)
            {
                results.Add(new DiagnosticResult
                {
                    severity = Severity.Warning,
                    title = $"发现 {problematicCount} 个活跃的 Oculus 组件",
                    description = "这些组件在编辑器中可能导致 NullReferenceException 错误",
                    solution = "禁用不需要的 Oculus 相关对象，或在 VR 设备上测试"
                });
            }
        }

        private void CheckMissingScripts()
        {
            var allObjects = Object.FindObjectsOfType<GameObject>(true);
            int missingCount = 0;

            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null)
                    {
                        missingCount++;
                    }
                }
            }

            if (missingCount > 0)
            {
                results.Add(new DiagnosticResult
                {
                    severity = Severity.Error,
                    title = $"发现 {missingCount} 个缺失的脚本",
                    description = "某些脚本引用已丢失，可能导致警告信息",
                    solution = "清理缺失的脚本组件"
                });
            }
        }

        // ============ 修复方法 ============

        private static int FixGroundPosition()
        {
            int fixed_count = 0;
            var grounds = Object.FindObjectsOfType<Transform>()
                .Where(t => t.name.ToLower().Contains("ground") || 
                           t.name.ToLower().Contains("floor") ||
                           t.name.ToLower().Contains("plane") ||
                           t.name.ToLower().Contains("rug"))
                .ToList();

            foreach (var ground in grounds)
            {
                if (Mathf.Abs(ground.position.y) > 0.01f)
                {
                    Undo.RecordObject(ground, "Fix Ground Position");
                    ground.position = new Vector3(ground.position.x, 0, ground.position.z);
                    Debug.Log($"[修复] 地面 '{ground.name}' Position Y 已设为 0");
                    fixed_count++;
                }
            }

            return fixed_count;
        }

        private static int FixCharacterPosition()
        {
            int fixed_count = 0;
            
            var characters = Object.FindObjectsOfType<Transform>()
                .Where(t => t.name.ToLower().Contains("death") ||
                           t.name.ToLower().Contains("remy") ||
                           t.name.ToLower().Contains("patient") ||
                           t.name.ToLower().Contains("character") ||
                           t.GetComponent<Animator>()?.avatar?.isHuman == true)
                .ToList();

            foreach (var character in characters)
            {
                if (character.position.y < -0.1f || character.position.y > 5f)
                {
                    Undo.RecordObject(character, "Fix Character Position");
                    character.position = new Vector3(character.position.x, 0, character.position.z);
                    Debug.Log($"[修复] 角色 '{character.name}' Position Y 已设为 0");
                    fixed_count++;
                }
            }

            return fixed_count;
        }

        private static int FixAnimatorSettings()
        {
            int fixed_count = 0;
            var animators = Object.FindObjectsOfType<Animator>();

            foreach (var animator in animators)
            {
                if (animator.applyRootMotion)
                {
                    Undo.RecordObject(animator, "Disable Root Motion");
                    animator.applyRootMotion = false;
                    Debug.Log($"[修复] '{animator.name}' 的 Apply Root Motion 已禁用");
                    fixed_count++;
                }

                if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                {
                    Undo.RecordObject(animator, "Set Culling Mode");
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    Debug.Log($"[修复] '{animator.name}' 的 Culling Mode 已设为 Always Animate");
                    fixed_count++;
                }
            }

            return fixed_count;
        }

        private static int DisableProblematicOculusComponents()
        {
            int fixed_count = 0;
            
            // 查找可能导致问题的对象
            string[] problematicNames = new string[]
            {
                "OVRCameraRig",
                "OculusInteraction",
                "HandTracking",
                "ControllerTracking"
            };

            var allObjects = Object.FindObjectsOfType<GameObject>(true);
            
            foreach (var go in allObjects)
            {
                // 检查对象名称
                bool isProblematic = false;
                foreach (var name in problematicNames)
                {
                    if (go.name.Contains(name))
                    {
                        isProblematic = true;
                        break;
                    }
                }

                // 检查组件类型
                if (!isProblematic)
                {
                    var components = go.GetComponents<MonoBehaviour>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        string typeName = comp.GetType().FullName ?? "";
                        if (typeName.Contains("Oculus.Interaction.Input"))
                        {
                            isProblematic = true;
                            break;
                        }
                    }
                }

                if (isProblematic && go.activeInHierarchy)
                {
                    // 只禁用那些导致编辑器错误的特定组件
                    var components = go.GetComponents<MonoBehaviour>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        string typeName = comp.GetType().FullName ?? "";
                        if (typeName.Contains("Oculus.Interaction.Input.Tracking"))
                        {
                            Undo.RecordObject(comp, "Disable Oculus Component");
                            comp.enabled = false;
                            Debug.Log($"[修复] 禁用了 '{go.name}' 上的 {comp.GetType().Name}");
                            fixed_count++;
                        }
                    }
                }
            }

            if (fixed_count == 0)
            {
                Debug.Log("[信息] 没有找到需要禁用的 Oculus 组件");
            }

            return fixed_count;
        }

        private static int CleanupMissingScripts()
        {
            int fixed_count = 0;
            var allObjects = Object.FindObjectsOfType<GameObject>(true);

            foreach (var go in allObjects)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0)
                {
                    Debug.Log($"[修复] 从 '{go.name}' 移除了 {removed} 个缺失脚本");
                    fixed_count += removed;
                }
            }

            if (fixed_count == 0)
            {
                Debug.Log("[信息] 没有找到缺失的脚本");
            }

            return fixed_count;
        }

        private class DiagnosticResult
        {
            public Severity severity;
            public string title;
            public string description;
            public string solution;
        }

        private enum Severity
        {
            Info,
            Warning,
            Error
        }
    }
}
