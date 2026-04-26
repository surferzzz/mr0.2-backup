using UnityEngine;

public class SimpleMetaXRChest : MonoBehaviour
{
    [Header("=== 胸部变形目标 ===")]
    public Transform chestTarget;
    
    [Header("=== 按压参数 ===")]
    public float maxPressDepth = 0.05f;
    public float recoverySpeed = 5f;
    public float pressForceMultiplier = 1f;
    
    [Header("=== 按压检测设置 ===")]
    public float minPressForce = 0.1f;
    public float pressDetectionThreshold = 0.001f;
    
    [Header("=== 调试信息 ===")]
    public int activeHands = 0;
    public float currentDepth = 0f;
    public float currentPressForce = 0f;
    public bool isBeingPressed = false;
    
    private Vector3 originalPosition;
    private int handCollisionCount = 0;
    private Rigidbody rb;
    private float lastDepth = 0f;
    private float pressStartTime = 0f;
    
    void Start()
    {
        if (chestTarget == null)
        {
            chestTarget = transform;
            Debug.LogWarning("⚠️ 未设置 chestTarget，使用自身");
        }
        
        originalPosition = chestTarget.localPosition;
        
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            Debug.Log("✅ Rigidbody 配置完成");
        }
        
        // 自动添加 BoxCollider（如果没有）
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.3f, 0.1f, 0.2f);
            Debug.LogWarning("⚠️ 自动添加了 BoxCollider");
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (IsHandObject(collision.gameObject))
        {
            handCollisionCount = 1;
            
            // 计算按压力度（基于接触点数量）
            float pressure = Mathf.Min(collision.contacts.Length / 3f, 1f);
            
            // 计算相对速度作为力度参考
            float relativeVelocity = collision.relativeVelocity.magnitude;
            currentPressForce = relativeVelocity * pressForceMultiplier;
            
            // 根据力度和接触点调整按压深度
            float forceMultiplier = Mathf.Clamp01(currentPressForce / minPressForce);
            float targetDepth = maxPressDepth * pressure * forceMultiplier;
            currentDepth = Mathf.Lerp(currentDepth, targetDepth, Time.deltaTime * 10f);
            
            Debug.Log($"✋ 检测到手部按压！接触点: {collision.contacts.Length}, 力度: {currentPressForce:F3}");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (IsHandObject(collision.gameObject))
        {
            handCollisionCount++;
            Debug.Log($"🤚 手部接触开始！对象: {collision.gameObject.name}");
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (IsHandObject(collision.gameObject))
        {
            handCollisionCount = Mathf.Max(0, handCollisionCount - 1);
            Debug.Log($"👋 手部离开！剩余: {handCollisionCount}");
        }
    }
    
    void Update()
    {
        activeHands = handCollisionCount;
        
        // 检测按压状态变化
        bool wasPressed = isBeingPressed;
        isBeingPressed = currentDepth > pressDetectionThreshold;
        
        // 按压开始
        if (!wasPressed && isBeingPressed)
        {
            pressStartTime = Time.time;
            Debug.Log("🔽 按压开始检测");
        }
        
        if (handCollisionCount == 0)
        {
            currentDepth = Mathf.Lerp(currentDepth, 0f, Time.deltaTime * recoverySpeed);
            currentPressForce = 0f;
        }
        
        // 更新胸部位置
        if (chestTarget != null)
        {
            Vector3 targetPos = originalPosition + Vector3.down * currentDepth;
            chestTarget.localPosition = Vector3.Lerp(
                chestTarget.localPosition, 
                targetPos, 
                Time.deltaTime * 10f
            );
        }
        
        // 向数据管理器发送深度数据
        if (PressDataManager.Instance != null)
        {
            PressDataManager.Instance.UpdatePressDepth(currentDepth);
        }
        
        lastDepth = currentDepth;
    }
    
    private bool IsHandObject(GameObject obj)
    {
        // 检查对象名称
        string name = obj.name.ToLower();
        if (name.Contains("hand") || 
            name.Contains("controller") || 
            name.Contains("anchor") ||
            name.Contains("palm") ||
            name.Contains("finger"))
        {
            Debug.Log($"🎯 识别到手部对象: {obj.name}");
            return true;
        }
        
        // 检查父对象 (暂时禁用OVRHand依赖)
        /*
        if (obj.GetComponentInParent<OVRHand>() != null)
        {
            Debug.Log($"🎯 通过 OVRHand 识别手部: {obj.name}");
            return true;
        }
        */
        
        return false;
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = handCollisionCount > 0 ? Color.red : Color.green;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}
