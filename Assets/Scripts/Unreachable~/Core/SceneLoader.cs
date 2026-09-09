using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRFirstAid.Core
{
    /// <summary>
    /// 场景加载器 - 处理场景切换和加载
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private float loadingDelay = 0.5f;

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private System.Collections.IEnumerator LoadSceneCoroutine(string sceneName)
        {
            yield return new WaitForSeconds(loadingDelay);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                // 可以在这里更新加载进度UI
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log($"Loading progress: {progress * 100}%");
                yield return null;
            }
        }

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
