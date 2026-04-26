using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手の位置と速度を使って胸部圧迫を検出するシンプルなジェスチャーディテクタ
/// </summary>
public class MetaXRGestureDetector : MonoBehaviour
{
    [Header("=== 手のトラッキング設定 ===")]
    // [SerializeField] private OVRHand leftHand;
    // [SerializeField] private OVRHand rightHand;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;
    
    [Header("=== 圧迫検出設定 ===")]
    [SerializeField] private Transform chestTarget;
    [SerializeField] private float pressDetectionDistance = 0.1f;
    [SerializeField] private float pressForceThreshold = 0.5f;
    [SerializeField] private LayerMask chestLayerMask = -1;
    
    [Header("=== デバッグ情報 ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float leftHandDistance = 0f;
    [SerializeField] private float rightHandDistance = 0f;
    [SerializeField] private bool leftHandPressing = false;
    [SerializeField] private bool rightHandPressing = false;
    
    private SimpleMetaXRChest chestController;
    private Vector3 leftHandVelocity;
    private Vector3 rightHandVelocity;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    
    void Start()
    {
        InitializeHandReferences();
        FindChestController();
        
        Debug.Log("🎯 MetaXRGestureDetector 初期化完了");
    }
    
    void InitializeHandReferences()
    {
        // 手のコンポーネントを自動探索（OVRHand 依存は一時無効）
        /*
        if (leftHand == null || rightHand == null)
        {
            OVRHand[] hands = FindObjectsOfType<OVRHand>();
            foreach (var hand in hands)
            {
                if (hand.HandType == OVRHand.Hand.HandLeft)
                {
                    leftHand = hand;
                    leftHandTransform = hand.transform;
                }
                else if (hand.HandType == OVRHand.Hand.HandRight)
                {
                    rightHand = hand;
                    rightHandTransform = hand.transform;
                }
            }
        }
        */
        
        // まだ見つからない場合は名前で検索
        if (leftHandTransform == null)
        {
            GameObject leftHandGO = GameObject.Find("LeftHandAnchor");
            if (leftHandGO == null) leftHandGO = GameObject.Find("Left Hand");
            if (leftHandGO != null) leftHandTransform = leftHandGO.transform;
        }
        
        if (rightHandTransform == null)
        {
            GameObject rightHandGO = GameObject.Find("RightHandAnchor");
            if (rightHandGO == null) rightHandGO = GameObject.Find("Right Hand");
            if (rightHandGO != null) rightHandTransform = rightHandGO.transform;
        }
        
        if (leftHandTransform != null) lastLeftHandPos = leftHandTransform.position;
        if (rightHandTransform != null) lastRightHandPos = rightHandTransform.position;
        
        Debug.Log($"🤚 手の参照設定完了 - 左手: {leftHandTransform != null}, 右手: {rightHandTransform != null}");
    }
    
    void FindChestController()
    {
        if (chestTarget == null)
        {
            // 胸部ターゲットを検索
            SimpleMetaXRChest[] chests = FindObjectsOfType<SimpleMetaXRChest>();
            if (chests.Length > 0)
            {
                chestController = chests[0];
                chestTarget = chestController.transform;
                Debug.Log($"🎯 胸部コントローラを自動検出: {chestTarget.name}");
            }
        }
        else
        {
            chestController = chestTarget.GetComponent<SimpleMetaXRChest>();
        }
    }
    
    void Update()
    {
        UpdateHandVelocities();
        DetectHandPressing();
        
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }
    
    void UpdateHandVelocities()
    {
        if (leftHandTransform != null)
        {
            leftHandVelocity = (leftHandTransform.position - lastLeftHandPos) / Time.deltaTime;
            lastLeftHandPos = leftHandTransform.position;
        }
        
        if (rightHandTransform != null)
        {
            rightHandVelocity = (rightHandTransform.position - lastRightHandPos) / Time.deltaTime;
            lastRightHandPos = rightHandTransform.position;
        }
    }
    
    void DetectHandPressing()
    {
        if (chestTarget == null) return;
        
        // 左手の圧迫検出
        if (leftHandTransform != null)
        {
            leftHandDistance = Vector3.Distance(leftHandTransform.position, chestTarget.position);
            bool wasLeftPressing = leftHandPressing;
            leftHandPressing = IsHandPressing(leftHandTransform, leftHandVelocity);
            
            if (!wasLeftPressing && leftHandPressing)
            {
                OnHandPressStart("左手");
            }
            else if (wasLeftPressing && !leftHandPressing)
            {
                OnHandPressEnd("左手");
            }
        }
        
        // 右手の圧迫検出
        if (rightHandTransform != null)
        {
            rightHandDistance = Vector3.Distance(rightHandTransform.position, chestTarget.position);
            bool wasRightPressing = rightHandPressing;
            rightHandPressing = IsHandPressing(rightHandTransform, rightHandVelocity);
            
            if (!wasRightPressing && rightHandPressing)
            {
                OnHandPressStart("右手");
            }
            else if (wasRightPressing && !rightHandPressing)
            {
                OnHandPressEnd("右手");
            }
        }
    }
    
    bool IsHandPressing(Transform handTransform, Vector3 handVelocity)
    {
        if (handTransform == null || chestTarget == null) return false;
        
        // 距離チェック
        float distance = Vector3.Distance(handTransform.position, chestTarget.position);
        if (distance > pressDetectionDistance) return false;
        
        // 速度チェック（胸部方向への移動）
        Vector3 directionToChest = (chestTarget.position - handTransform.position).normalized;
        float velocityTowardChest = Vector3.Dot(handVelocity, directionToChest);
        
        // 手の姿勢チェック（OVRHand 依存は一時無効）
        bool handConfident = true;
        /*
        OVRHand hand = handTransform.GetComponent<OVRHand>();
        if (hand != null)
        {
            handConfident = hand.IsTracked && hand.HandConfidence == OVRHand.TrackingConfidence.High;
        }
        */
        
        return handConfident && (distance < pressDetectionDistance * 0.5f || velocityTowardChest > pressForceThreshold);
    }
    
    void OnHandPressStart(string handName)
    {
        Debug.Log($"🔽 {handName} が圧迫開始");
        
        // ここにハプティクスを追加可能（現在は無効）
        /*
        if (handName == "左手" && leftHand != null)
        {
            // 触发左手震动
            // OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
        }
        else if (handName == "右手" && rightHand != null)
        {
            // 触发右手震动
            // OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
        }
        */
    }
    
    void OnHandPressEnd(string handName)
    {
        Debug.Log($"🔼 {handName} が圧迫終了");
    }
    
    void UpdateDebugInfo()
    {
        // デバッグ情報は Inspector に表示済み
    }
    
    // 公開インターフェース
    public bool IsAnyHandPressing()
    {
        return leftHandPressing || rightHandPressing;
    }
    
    public bool IsLeftHandPressing()
    {
        return leftHandPressing;
    }
    
    public bool IsRightHandPressing()
    {
        return rightHandPressing;
    }
    
    public float GetLeftHandDistance()
    {
        return leftHandDistance;
    }
    
    public float GetRightHandDistance()
    {
        return rightHandDistance;
    }
    
    public Vector3 GetLeftHandVelocity()
    {
        return leftHandVelocity;
    }
    
    public Vector3 GetRightHandVelocity()
    {
        return rightHandVelocity;
    }
    
    // 手の参照を手動設定（実行時設定用）
    public void SetHandReferences(Transform leftHand, Transform rightHand)
    {
        leftHandTransform = leftHand;
        rightHandTransform = rightHand;
        
        if (leftHandTransform != null) 
        {
            lastLeftHandPos = leftHandTransform.position;
            // this.leftHand = leftHandTransform.GetComponent<OVRHand>();
        }
        
        if (rightHandTransform != null) 
        {
            lastRightHandPos = rightHandTransform.position;
            // this.rightHand = rightHandTransform.GetComponent<OVRHand>();
        }
        
        Debug.Log("🤚 手の参照を手動で設定しました");
    }
    
    public void SetChestTarget(Transform chest)
    {
        chestTarget = chest;
        chestController = chest?.GetComponent<SimpleMetaXRChest>();
        Debug.Log($"🎯 胸部ターゲットを設定: {chest?.name}");
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // 手から胸部へのラインを描画
        if (leftHandTransform != null && chestTarget != null)
        {
            Gizmos.color = leftHandPressing ? Color.red : Color.green;
            Gizmos.DrawLine(leftHandTransform.position, chestTarget.position);
            Gizmos.DrawWireSphere(leftHandTransform.position, 0.02f);
        }
        
        if (rightHandTransform != null && chestTarget != null)
        {
            Gizmos.color = rightHandPressing ? Color.red : Color.blue;
            Gizmos.DrawLine(rightHandTransform.position, chestTarget.position);
            Gizmos.DrawWireSphere(rightHandTransform.position, 0.02f);
        }
        
        // 圧迫検出の範囲を描画
        if (chestTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(chestTarget.position, pressDetectionDistance);
        }
    }
}
