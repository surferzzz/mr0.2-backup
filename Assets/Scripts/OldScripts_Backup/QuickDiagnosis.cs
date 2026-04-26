using UnityEngine;

/// <summary>
/// 快速诊断工具 - 检查为什么碰撞检测不工作
/// </summary>
public class QuickDiagnosis : MonoBehaviour
{
    [Header("=== 诊断工具 ===")]
    [Tooltip("自动检查这个对象的碰撞设置")]
    public bool autoCheck = true;
    
    void Start()
    {
        if (autoCheck)
        {
            CheckThisObject();
        }
    }
    
    [ContextMenu("🔍 检查这个对象")]
    public void CheckThisObject()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log($"║  🔍 诊断对象: {gameObject.name}");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        // 检查 Collider
        Collider[] colliders = GetComponents<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogError("❌ 没有 Collider！必须添加碰撞器！");
        }
        else
        {
            Debug.Log($"✅ 找到 {colliders.Length} 个 Collider:");
            foreach (Collider col in colliders)
            {
                Debug.Log($"   • {col.GetType().Name}");
                Debug.Log($"     - Is Trigger: {col.isTrigger}");
                Debug.Log($"     - Enabled: {col.enabled}");
            }
        }
        
        // 检查 Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("⚠️ 没有 Rigidbody！建议添加");
        }
        else
        {
            Debug.Log("✅ 有 Rigidbody:");
            Debug.Log($"   - Is Kinematic: {rb.isKinematic}");
            Debug.Log($"   - Use Gravity: {rb.useGravity}");
            Debug.Log($"   - Mass: {rb.mass}");
        }
        
        // 检查碰撞检测脚本
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        bool hasCollisionScript = false;
        foreach (MonoBehaviour script in scripts)
        {
            if (script.GetType().Name.Contains("Chest") || 
                script.GetType().Name.Contains("Press") ||
                script.GetType().Name.Contains("Demo"))
            {
                Debug.Log($"✅ 找到检测脚本: {script.GetType().Name}");
                hasCollisionScript = true;
            }
        }
        
        if (!hasCollisionScript)
        {
            Debug.LogWarning("⚠️ 没有碰撞检测脚本！");
        }
        
        // 检查 Layer
        Debug.Log($"📋 Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        Debug.Log("");
        Debug.Log("═══════════════════════════════════════════════════════════════");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🎯 QuickDiagnosis 检测到碰撞！");
        Debug.Log($"   对象: {collision.gameObject.name}");
        Debug.Log($"   标签: {collision.gameObject.tag}");
        Debug.Log($"   碰撞点数: {collision.contacts.Length}");
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🎯 QuickDiagnosis 检测到触发器！");
        Debug.Log($"   对象: {other.gameObject.name}");
        Debug.Log($"   标签: {other.tag}");
    }
}

