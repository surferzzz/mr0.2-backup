using UnityEngine;

/// <summary>
/// 超级简单的碰撞测试 - 保证能看到效果
/// </summary>
public class SuperSimpleCollisionTest : MonoBehaviour
{
    [Header("=== 超简单碰撞测试 ===")]
    public Color normalColor = Color.green;
    public Color hitColor = Color.red;
    public int hitCount = 0;
    
    private Renderer rend;
    private Material mat;
    
    void Start()
    {
        // 获取或创建渲染器
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            mat.color = normalColor;
        }
        
        Debug.Log($"✅ SuperSimpleCollisionTest 启动: {gameObject.name}");
        Debug.Log($"   位置: {transform.position}");
        
        // 自动检查配置
        CheckSetup();
    }
    
    void CheckSetup()
    {
        bool hasCollider = GetComponent<Collider>() != null;
        bool hasRigidbody = GetComponent<Rigidbody>() != null;
        
        Debug.Log("📋 配置检查:");
        Debug.Log($"   Collider: {(hasCollider ? "✅" : "❌")}");
        Debug.Log($"   Rigidbody: {(hasRigidbody ? "✅" : "⚠️ 建议添加")}");
        
        if (!hasCollider)
        {
            Debug.LogError("❌ 缺少 Collider！请添加：");
            Debug.LogError("   Inspector → Add Component → Box Collider");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        hitCount++;
        
        Debug.Log("════════════════════════════════════════════════");
        Debug.Log($"🎯🎯🎯 碰撞检测！第 {hitCount} 次");
        Debug.Log($"   我是: {gameObject.name}");
        Debug.Log($"   对方是: {collision.gameObject.name}");
        Debug.Log($"   对方标签: {collision.gameObject.tag}");
        Debug.Log($"   碰撞力度: {collision.relativeVelocity.magnitude:F2}");
        Debug.Log($"   接触点数: {collision.contacts.Length}");
        Debug.Log("════════════════════════════════════════════════");
        
        // 变色
        if (rend != null && mat != null)
        {
            mat.color = hitColor;
            Debug.Log("🎨 颜色已变为红色");
        }
        
        // 通知VR UI
        NotifyVRUI(collision.gameObject.name);
    }
    
    void NotifyVRUI(string objectName)
    {
        VRSimpleUI vrUI = FindObjectOfType<VRSimpleUI>();
        if (vrUI != null)
        {
            vrUI.OnCollisionDetected(objectName);
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // 持续碰撞
        if (rend != null && mat != null)
        {
            mat.color = hitColor;
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"👋 碰撞结束: {collision.gameObject.name}");
        
        // 恢复颜色
        if (rend != null && mat != null)
        {
            mat.color = normalColor;
            Debug.Log("🎨 颜色已恢复为绿色");
        }
    }
    
    // 也监听触发器（万一Is Trigger被勾选了）
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log($"⚡ 触发器检测！");
        Debug.Log($"   对方: {other.gameObject.name}");
        Debug.Log($"   注意：这是触发器模式，不是碰撞模式");
        Debug.Log("═══════════════════════════════════════════════════");
    }
    
    // 每秒显示状态
    void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"💚 {gameObject.name} 运行中... (碰撞次数: {hitCount})");
        }
    }
}

