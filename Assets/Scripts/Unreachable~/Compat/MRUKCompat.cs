#if META_MR_UTILITY_KIT_DEFINED
using UnityEngine;
using Meta.XR.MRUtilityKit;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// 兼容补丁：新版 MultiplayerBlocks 期望 MRUK 提供 SetCustomWorldLockAnchor。
    /// 旧版 MRUK 没有该方法时，用扩展方法兜底，避免编译错误，简单禁用世界锁。
    /// </summary>
    public static class MRUKCompatExtensions
    {
        public static void SetCustomWorldLockAnchor(this MRUK mruk, OVRAnchor anchor, Pose pose)
        {
            if (mruk == null) return;
            mruk.EnableWorldLock = false;
        }
    }
}
#endif
