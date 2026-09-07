using UnityEngine;

/// <summary>
/// 超简单Demo - 第1步：基础碰撞检测
/// 这是最简单的版本，只检测碰撞，不需要任何复杂设置
/// </summary>
public class SimpleDemo_Step1 : MonoBehaviour
{
    [Header("=== 简单碰撞检测Demo ===")]
    [Tooltip("检测到碰撞时改变的颜色")]
    public Color hitColor = Color.red;
    
    [Tooltip("默认颜色")]
    public Color normalColor = Color.green;
    
    [Header("=== 状态显示 ===")]
    [Tooltip("当前碰撞次数")]
    public int hitCount = 0;
    
    private Renderer objectRenderer;
    private Color currentColor;
    
    void Start()
    {
        // 获取渲染器
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }
        
        // 设置初始颜色
        if (objectRenderer != null)
        {
            currentColor = normalColor;
            objectRenderer.material.color = currentColor;
        }
        
        Debug.Log($"✅ SimpleDemo_Step1 初始化完成：{gameObject.name}");
        Debug.Log("   等待碰撞检测...");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        hitCount++;
        
        Debug.Log($"🎯 碰撞检测！第 {hitCount} 次");
        Debug.Log($"   碰撞对象: {collision.gameObject.name}");
        Debug.Log($"   碰撞点数量: {collision.contacts.Length}");
        
        // 改变颜色
        if (objectRenderer != null)
        {
            objectRenderer.material.color = hitColor;
        }
        
        // 1秒后恢复颜色
        Invoke("ResetColor", 1f);
    }
    
    void OnCollisionStay(Collision collision)
    {
        // 持续碰撞时
        if (objectRenderer != null)
        {
            objectRenderer.material.color = hitColor;
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"👋 碰撞结束");
        ResetColor();
    }
    
    void ResetColor()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = normalColor;
        }
    }
    
    // 在Scene视图中显示信息
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}

