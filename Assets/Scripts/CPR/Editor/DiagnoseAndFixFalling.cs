using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 诊断并修复患者掉地下的问题
    /// </summary>
    public class DiagnoseAndFixFalling
    {
        [MenuItem("Tools/CPR/诊断并修复掉地下问题", false, 10)]
        public static void DiagnoseAndFix()
        {
            var report = "===== 患者掉地下问题诊断 =====\n\n";
            bool foundIssues = false;

            // 1. 查找患者模型
            var patient = GameObject.Find("Standing React Death Backward") 
                ?? GameObject.Find("Patient") 
                ?? GameObject.Find("XBot_Patient");

            if (patient == null)
            {
                EditorUtility.DisplayDialog("未找到患者", "场景中没有患者模型。", "确定");
                return;
            }

            report += $"患者模型: {patient.name}\n";
            report += $"位置: {patient.transform.position}\n";
            report += $"旋转: {patient.transform.rotation.eulerAngles}\n\n";

            // 2. 检查冲突的控制器脚本
            report += "【控制器脚本检查】\n";
            
            var trainingController = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>();
            var gameController = Object.FindObjectOfType<CPRSimulation.Game.CPRGameController>();
            var simpleMenuController = Object.FindObjectOfType<CPRSimulation.Game.SimpleMenuController>();
            var patientFallController = patient.GetComponent<CPRSimulation.Core.PatientFallController>();

            if (trainingController != null)
                report += $"✓ CPRTrainingController: {trainingController.gameObject.name}\n";
            if (gameController != null)
            {
                report += $"⚠ CPRGameController: {gameController.gameObject.name} (冲突！)\n";
                foundIssues = true;
            }
            if (simpleMenuController != null)
            {
                report += $"⚠ SimpleMenuController: {simpleMenuController.gameObject.name} (冲突！)\n";
                foundIssues = true;
            }
            if (patientFallController != null)
            {
                report += $"⚠ PatientFallController 在患者上 (冲突！会用代码翻转模型)\n";
                foundIssues = true;
            }

            report += "\n";

            // 3. 检查 Animator 设置
            report += "【Animator 检查】\n";
            var animator = patient.GetComponent<Animator>();
            if (animator != null)
            {
                report += $"✓ Animator 存在\n";
                report += $"  - Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "无")}\n";
                report += $"  - Apply Root Motion: {animator.applyRootMotion} {(animator.applyRootMotion ? "⚠ 应该关闭" : "✓")}\n";
                report += $"  - Enabled: {animator.enabled}\n";
                if (animator.applyRootMotion)
                    foundIssues = true;
            }
            else
            {
                report += "✗ 缺少 Animator\n";
            }

            report += "\n";

            // 4. 检查 Rigidbody 设置
            report += "【Rigidbody 检查】\n";
            var rb = patient.GetComponent<Rigidbody>();
            if (rb != null)
            {
                report += $"✓ Rigidbody 存在\n";
                report += $"  - Use Gravity: {rb.useGravity} {(rb.useGravity ? "⚠ 应该关闭" : "✓")}\n";
                report += $"  - Is Kinematic: {rb.isKinematic} {(rb.isKinematic ? "✓" : "⚠ 应该开启")}\n";
                report += $"  - Constraints: {rb.constraints}\n";
                if (rb.useGravity || !rb.isKinematic)
                    foundIssues = true;
            }
            else
            {
                report += "⚠ 缺少 Rigidbody（建议添加并设为 Kinematic）\n";
                foundIssues = true;
            }

            report += "\n";

            // 5. 检查子物体的物理组件
            report += "【子物体物理组件检查】\n";
            var childRbs = patient.GetComponentsInChildren<Rigidbody>();
            var childCols = patient.GetComponentsInChildren<Collider>();
            
            if (childRbs.Length > 1)
            {
                report += $"⚠ 发现 {childRbs.Length} 个 Rigidbody（包括根节点），子物体的 Rigidbody 可能导致掉落\n";
                foundIssues = true;
            }
            else
            {
                report += $"✓ 只有根节点有 Rigidbody\n";
            }

            report += "\n===== 诊断完成 =====\n\n";

            if (foundIssues)
            {
                report += "发现问题！是否自动修复？";
                if (EditorUtility.DisplayDialog("诊断结果", report, "自动修复", "取消"))
                {
                    AutoFix(patient, trainingController, gameController, simpleMenuController, patientFallController, animator, rb);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("诊断结果", report + "未发现明显问题。", "确定");
            }

            Debug.Log(report);
        }

        private static void AutoFix(GameObject patient, 
            CPRSimulation.Game.CPRTrainingController trainingController,
            CPRSimulation.Game.CPRGameController gameController,
            CPRSimulation.Game.SimpleMenuController simpleMenuController,
            CPRSimulation.Core.PatientFallController patientFallController,
            Animator animator, Rigidbody rb)
        {
            int fixCount = 0;

            // 1. 禁用冲突的控制器
            if (gameController != null && gameController.enabled)
            {
                Undo.RecordObject(gameController, "Disable CPRGameController");
                gameController.enabled = false;
                Debug.Log("[修复] 已禁用 CPRGameController");
                fixCount++;
            }

            if (simpleMenuController != null && simpleMenuController.enabled)
            {
                Undo.RecordObject(simpleMenuController, "Disable SimpleMenuController");
                simpleMenuController.enabled = false;
                Debug.Log("[修复] 已禁用 SimpleMenuController");
                fixCount++;
            }

            // 2. 移除 PatientFallController（会用代码翻转）
            if (patientFallController != null)
            {
                Undo.DestroyObjectImmediate(patientFallController);
                Debug.Log("[修复] 已移除 PatientFallController");
                fixCount++;
            }

            // 3. 修复 Animator 设置
            if (animator != null)
            {
                if (animator.applyRootMotion)
                {
                    Undo.RecordObject(animator, "Fix Animator");
                    animator.applyRootMotion = false;
                    Debug.Log("[修复] 已关闭 Apply Root Motion");
                    fixCount++;
                }
            }

            // 4. 修复 Rigidbody 设置
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(patient);
                Debug.Log("[修复] 已添加 Rigidbody");
                fixCount++;
            }

            if (rb != null)
            {
                Undo.RecordObject(rb, "Fix Rigidbody");
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                Debug.Log("[修复] 已配置 Rigidbody（禁用重力，冻结 Y 轴）");
                fixCount++;
            }

            // 5. 锁定位置在地面
            Vector3 pos = patient.transform.position;
            if (pos.y < 0f || pos.y > 0.1f)
            {
                Undo.RecordObject(patient.transform, "Fix Position");
                pos.y = 0f;
                patient.transform.position = pos;
                Debug.Log("[修复] 已将位置锁定在地面");
                fixCount++;
            }

            // 6. 确保有 CPRTrainingController
            if (trainingController == null)
            {
                var go = new GameObject("CPRTrainingController");
                Undo.RegisterCreatedObjectUndo(go, "Add CPRTrainingController");
                go.AddComponent<CPRSimulation.Game.CPRTrainingController>();
                Debug.Log("[修复] 已添加 CPRTrainingController");
                fixCount++;
            }

            EditorUtility.DisplayDialog("修复完成", 
                $"已完成 {fixCount} 项修复。\n\n" +
                "✓ 已移除冲突的控制器脚本\n" +
                "✓ 已配置 Rigidbody 防止掉落\n" +
                "✓ 已关闭 Apply Root Motion\n\n" +
                "现在运行游戏应该不会再掉地下了。", "确定");
        }
    }
}
