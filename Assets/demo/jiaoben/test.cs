using UnityEngine;
using Bhaptics.SDK2;   // 按你实际命名空间来

public class Test : MonoBehaviour   // 类名必须和 Test.cs 完全一样（含大小写）
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BhapticsLibrary.Play("e030");   // 或 "testevent"，写你事件的名字
        }
    }
}