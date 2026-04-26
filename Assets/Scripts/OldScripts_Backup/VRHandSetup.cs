using UnityEngine;

/// <summary>
/// VR手部自动设置工具
/// 自动为左右手添加碰撞检测
/// </summary>
public class VRHandSetup : MonoBehaviour
{
    [Header("=== 自动设置 ===")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("=== 手部锚点 ===")]
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;
    
    [Header("=== 碰撞器设置 ===")]
    [SerializeField] private float handColliderRadius = 0.08f;
    [SerializeField] private bool usePhysicalHands = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupVRHands();
        }
    }
    
    [ContextMenu("🤚 设置VR手部碰撞")]
    public void SetupVRHands()
    {
        Debug.Log("🤚 开始设置VR手部碰撞系统...");
        
        // 查找手部锚点
        FindHandAnchors();
        
        // 设置左手
        if (leftHandAnchor != null)
        {
            SetupHandCollider(leftHandAnchor, true);
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到左手锚点");
        }
        
        // 设置右手
        if (rightHandAnchor != null)
        {
            SetupHandCollider(rightHandAnchor, false);
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到右手锚点");
        }
        
        Debug.Log("✅ VR手部碰撞系统设置完成");
    }
    
    void FindHandAnchors()
    {
        // 尝试多种方式查找手部锚点
        if (leftHandAnchor == null)
        {
            // 查找常见的左手锚点名称
            string[] leftHandNames = {
                "LeftHandAnchor",
                "Left Hand",
                "LeftHand",
                "[BuildingBlock] Hand Tracking left"
            };
            
            foreach (string name in leftHandNames)
            {
                GameObject leftHandGO = GameObject.Find(name);
                if (leftHandGO != null)
                {
                    leftHandAnchor = leftHandGO.transform;
                    Debug.Log($"✅ 找到左手: {name}");
                    break;
                }
            }
        }
        
        if (rightHandAnchor == null)
        {
            // 查找常见的右手锚点名称
            string[] rightHandNames = {
                "RightHandAnchor",
                "Right Hand",
                "RightHand",
                "[BuildingBlock] Hand Tracking right"
            };
            
            foreach (string name in rightHandNames)
            {
                GameObject rightHandGO = GameObject.Find(name);
                if (rightHandGO != null)
                {
                    rightHandAnchor = rightHandGO.transform;
                    Debug.Log($"✅ 找到右手: {name}");
                    break;
                }
            }
        }
    }
    
    void SetupHandCollider(Transform handAnchor, bool isLeftHand)
    {
        // 为手掌创建碰撞器对象
        string handName = isLeftHand ? "LeftHandCollider" : "RightHandCollider";
        Transform colliderTransform = handAnchor.Find(handName);
        GameObject colliderGO;
        
        if (colliderTransform == null)
        {
            colliderGO = new GameObject(handName);
            colliderGO.transform.SetParent(handAnchor);
            colliderGO.transform.localPosition = Vector3.zero;
            colliderGO.transform.localRotation = Quaternion.identity;
        }
        else
        {
            colliderGO = colliderTransform.gameObject;
        }
        
        // 添加碰撞组件
        SphereCollider collider = colliderGO.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = colliderGO.AddComponent<SphereCollider>();
        }
        collider.radius = handColliderRadius;
        collider.isTrigger = false;
        
        // 添加刚体
        Rigidbody rb = colliderGO.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = colliderGO.AddComponent<Rigidbody>();
        }
        
        if (usePhysicalHands)
        {
            // 物理手部模式
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.mass = 0.5f;
            rb.drag = 10f;
            rb.angularDrag = 10f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            // 运动学模式（手不会被推开）
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // 添加VR手部碰撞器脚本
        VRHandCollider handColliderScript = colliderGO.GetComponent<VRHandCollider>();
        if (handColliderScript == null)
        {
            handColliderScript = colliderGO.AddComponent<VRHandCollider>();
        }
        
        Debug.Log($"✅ {(isLeftHand ? "左手" : "右手")} 碰撞器已设置");
    }
    
    [ContextMenu("🧹 移除手部碰撞器")]
    public void RemoveHandColliders()
    {
        if (leftHandAnchor != null)
        {
            Transform leftCollider = leftHandAnchor.Find("LeftHandCollider");
            if (leftCollider != null)
            {
                DestroyImmediate(leftCollider.gameObject);
                Debug.Log("🧹 已移除左手碰撞器");
            }
        }
        
        if (rightHandAnchor != null)
        {
            Transform rightCollider = rightHandAnchor.Find("RightHandCollider");
            if (rightCollider != null)
            {
                DestroyImmediate(rightCollider.gameObject);
                Debug.Log("🧹 已移除右手碰撞器");
            }
        }
    }
}


