#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 清理所有CPR训练系统对象，准备重新开始
/// </summary>
public class CleanupAndReset : MonoBehaviour
{
    [MenuItem("CPR训练系统/🧹 完全清理并重置", false, 100)]
    public static void CompleteCleanup()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "确认完全清理", 
            "这将删除所有CPR训练系统相关对象：\n\n" +
            "• 所有管理器对象\n" +
            "• 所有UI对象\n" +
            "• 测试对象\n" +
            "• 自动创建的Patient模型\n" +
            "• ChestArea等检测区\n\n" +
            "您的场景、Remy模型和其他资源会保留。\n\n" +
            "确定要清理吗？", 
            "确定清理", 
            "取消");
        
        if (!confirm)
        {
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                  🧹 开始完全清理...                           ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        int deletedCount = 0;
        
        // 要删除的对象名称列表
        string[] objectsToDelete = new string[]
        {
            "Patient",
            "SystemManager",
            "SimplePressUI",
            "SimplePressUI_Canvas",
            "VRHandSetup",
            "CPRSetup",
            "QuickErrorFix",
            "TestBall",
            "TestBall_PressSimulator",
            "EasySetup"
        };
        
        // 删除这些对象
        foreach (string objName in objectsToDelete)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
                Debug.Log($"✅ 已删除: {objName}");
                deletedCount++;
            }
        }
        
        // 清理Remy模型上的ChestArea
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "ChestArea" || obj.name == "ChestVisual")
            {
                Undo.DestroyObjectImmediate(obj);
                Debug.Log($"✅ 已删除: {obj.name}");
                deletedCount++;
            }
        }
        
        // 清理所有SimpleMetaXRChest脚本
        SimpleMetaXRChest[] chestScripts = FindObjectsOfType<SimpleMetaXRChest>();
        foreach (SimpleMetaXRChest script in chestScripts)
        {
            Undo.DestroyObjectImmediate(script);
            Debug.Log($"✅ 已移除按压检测脚本: {script.gameObject.name}");
        }
        
        // 清理所有添加的MeshCollider（保留原始的）
        MeshCollider[] meshColliders = FindObjectsOfType<MeshCollider>();
        foreach (MeshCollider collider in meshColliders)
        {
            if (collider.convex && collider.gameObject.GetComponent<SkinnedMeshRenderer>() != null)
            {
                Undo.DestroyObjectImmediate(collider);
                Debug.Log($"✅ 已移除网格碰撞器: {collider.gameObject.name}");
            }
        }
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log($"║              ✅ 清理完成！共删除 {deletedCount} 个对象             ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("🎯 现在可以重新开始了！");
        Debug.Log("   场景已清理干净，准备从头开始制作。");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "清理完成！✅", 
            $"已成功删除 {deletedCount} 个对象！\n\n" +
            "场景已清理干净。\n\n" +
            "下一步：\n" +
            "查看 '从零开始制作指南.txt'", 
            "好的");
    }
}
#endif

