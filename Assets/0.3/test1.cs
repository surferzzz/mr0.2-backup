using Bhaptics.SDK2;
using UnityEngine;

public class BhapticsEventTest : MonoBehaviour
{
    void Update()
    {
        // 1 键：过浅  cpr_shallow
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryPlayEvent("cpr_shallow");
        }

        // 2 键：良好  cpr_good_depth
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryPlayEvent("cpr_good_depth");
        }

        // 3 键：过深  cpr_too_deep
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryPlayEvent("cpr_too_deep");
        }
    }

    private void TryPlayEvent(string eventName)
    {
        if (!BhapticsLibrary.IsBhapticsAvailable(false))
        {
            Debug.LogWarning("[BhapticsEventTest] bHaptics 不可用，请检查 Player 是否运行、手套是否连接");
            return;
        }

        Debug.Log("[BhapticsEventTest] Play Event: " + eventName);
        BhapticsLibrary.Play(eventName);
    }
}