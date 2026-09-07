using UnityEngine;

/// <summary>
/// Quest 3 快速启动助手
/// 自动检测并提示安装所需的包
/// </summary>
public class Quest3QuickStart : MonoBehaviour
{
    [Header("=== Quest 3 快速启动 ===")]
    [Tooltip("显示详细的设置步骤")]
    public bool showDetailedInstructions = true;
    
    void Start()
    {
        CheckAndDisplayInstructions();
    }
    
    [ContextMenu("🎯 显示Quest 3设置步骤")]
    public void CheckAndDisplayInstructions()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                                                              ║");
        Debug.Log("║           🎮 Quest 3 VR 心肺复苏系统设置指南                 ║");
        Debug.Log("║                                                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        // 检查是否已安装 Meta XR SDK
        bool hasMetaXR = CheckMetaXRSDK();
        
        if (!hasMetaXR)
        {
            Debug.LogWarning("⚠️ 未检测到 Meta XR SDK！");
            Debug.Log("");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log("📦 第1步：安装 Meta XR SDK");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log("");
            Debug.Log("方法A - 使用Unity Package Manager（推荐）：");
            Debug.Log("  1. 点击 Unity 顶部菜单 → Window → Package Manager");
            Debug.Log("  2. 左上角点击 '+' → Add package by name");
            Debug.Log("  3. 输入：com.meta.xr.sdk.core");
            Debug.Log("  4. 点击 'Add' 按钮");
            Debug.Log("  5. 等待安装完成（约1-2分钟）");
            Debug.Log("");
            Debug.Log("方法B - 使用Asset Store：");
            Debug.Log("  1. 打开 Unity Asset Store");
            Debug.Log("  2. 搜索 'Meta XR All-in-One SDK'");
            Debug.Log("  3. 下载并导入");
            Debug.Log("");
        }
        else
        {
            Debug.Log("✅ Meta XR SDK 已安装！");
        }
        
        Debug.Log("");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("⚙️ 第2步：配置 Android 构建设置");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("");
        Debug.Log("  1. 点击 File → Build Settings");
        Debug.Log("  2. 选择 'Android' 平台");
        Debug.Log("  3. 点击 'Switch Platform' 按钮");
        Debug.Log("  4. 等待切换完成（首次约5-10分钟）");
        Debug.Log("");
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🔌 第3步：连接 Quest 3");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("");
        Debug.Log("  1. 用 USB-C 数据线连接 Quest 3 到电脑");
        Debug.Log("  2. 戴上 Quest 3 头显");
        Debug.Log("  3. 在头显中允许 USB 调试");
        Debug.Log("  4. 在 Unity 中点击 File → Build And Run");
        Debug.Log("");
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 第4步：开始训练！");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("");
        Debug.Log("  1. 应用会自动安装到 Quest 3");
        Debug.Log("  2. 在 VR 中找到绿色的胸部区域");
        Debug.Log("  3. 用手触碰胸部进行按压");
        Debug.Log("  4. 观察 UI 显示的按压数据");
        Debug.Log("");
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("💡 快速测试（不用Quest 3）");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("");
        Debug.Log("  如果暂时没有 Quest 3，可以在编辑器中测试：");
        Debug.Log("");
        Debug.Log("  1. 在 CPR 控制面板中点击 '🎾 创建测试球体'");
        Debug.Log("  2. 点击 Unity 顶部的播放按钮 ▶️");
        Debug.Log("  3. 观察球体掉落触发按压检测");
        Debug.Log("  4. 查看 UI 数据更新");
        Debug.Log("");
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("❓ 遇到问题？");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("");
        Debug.Log("  • 在 CPR 控制面板中点击 '⚡ 修复所有Quest 3问题'");
        Debug.Log("  • 或点击 '📊 诊断当前问题' 查看详细信息");
        Debug.Log("  • 查看文档：README_Quest3问题解决.md");
        Debug.Log("");
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                        🚀 准备就绪！                          ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
    }
    
    bool CheckMetaXRSDK()
    {
        // 检查常见的 Meta XR 命名空间
        try
        {
            #if USING_META_XR_SDK
            return true;
            #else
            // 尝试查找 OVRManager 类型
            var ovrManagerType = System.Type.GetType("OVRManager, Assembly-CSharp");
            if (ovrManagerType != null)
            {
                return true;
            }
            
            // 尝试查找 Meta XR 包相关的文件
            if (System.IO.Directory.Exists("Packages/com.meta.xr.sdk.core"))
            {
                return true;
            }
            #endif
        }
        catch
        {
            // 忽略错误
        }
        
        return false;
    }
    
    [ContextMenu("🎾 创建简单测试球")]
    public void CreateTestBall()
    {
        // 创建测试球
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "TestBall_PressSimulator";
        ball.transform.position = new Vector3(0, 2f, 2f);
        ball.transform.localScale = Vector3.one * 0.15f;
        
        // 添加物理组件
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ball.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.useGravity = true;
        
        // 设置颜色
        Renderer renderer = ball.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0f); // 橙色
            renderer.material = mat;
        }
        
        // 添加标签
        ball.tag = "Hand";
        
        Debug.Log("✅ 测试球创建完成！");
        Debug.Log("   位置：胸部上方");
        Debug.Log("   点击播放按钮 ▶️ 测试");
        Debug.Log("");
    }
}

