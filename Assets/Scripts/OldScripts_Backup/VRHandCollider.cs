using UnityEngine;

/// <summary>
/// VR手部碰撞检测器
/// 自动为Quest手部跟踪添加碰撞体
/// </summary>
public class VRHandCollider : MonoBehaviour
{
    [Header("=== 手部设置 ===")]
    [SerializeField] private bool isLeftHand = true;
    [SerializeField] private float colliderRadius = 0.08f;
    [SerializeField] private bool autoSetup = true;
    
    [Header("=== 碰撞层设置 ===")]
    [SerializeField] private LayerMask collisionLayers = -1;
    
    private SphereCollider handCollider;
    private Rigidbody handRigidbody;
    private Transform handTransform;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupHandCollider();
        }
    }
    
    void SetupHandCollider()
    {
        // 添加碰撞体
        handCollider = gameObject.GetComponent<SphereCollider>();
        if (handCollider == null)
        {
            handCollider = gameObject.AddComponent<SphereCollider>();
        }
        handCollider.radius = colliderRadius;
        handCollider.isTrigger = false; // 使用物理碰撞，不是触发器
        
        // 添加刚体
        handRigidbody = gameObject.GetComponent<Rigidbody>();
        if (handRigidbody == null)
        {
            handRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // 配置刚体
        handRigidbody.isKinematic = false; // 非运动学，可以产生碰撞
        handRigidbody.useGravity = false; // 不受重力影响
        handRigidbody.mass = 0.5f; // 轻量级
        handRigidbody.drag = 10f; // 高阻力，防止飞出去
        handRigidbody.angularDrag = 10f;
        handRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 连续碰撞检测
        
        // 冻结旋转，只允许位置变化
        handRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        
        Debug.Log($"✅ {(isLeftHand ? "左手" : "右手")} 碰撞器设置完成");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🤚 {(isLeftHand ? "左手" : "右手")} 碰撞到: {collision.gameObject.name}");
    }
    
    [ContextMenu("手动设置碰撞器")]
    public void ManualSetup()
    {
        SetupHandCollider();
    }
}


