using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CPRSimulation.Game;

namespace CPRSimulation.Research
{
    /// <summary>
    /// 視界の隅に出しっぱなしにする実験タグ：参加者ID / 群 / 何回目、そして記録中かどうか。
    ///
    /// 実験当日いちばん怖い事故は「ID や群を変え忘れて、前の参加者の設定のまま
    /// 1 回ぶん記録してしまう」こと。CSV は後から見れば分かるが、そのときには
    /// もう参加者は帰っている。だから走らせる前に目で確かめられる場所へ出す。
    ///
    /// 値そのものは <see cref="ResearchSession"/>（PlayerPrefs ＋ session_config.json）。
    /// ここは表示するだけで、訓練にも記録にもいっさい触らない。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResearchHudLabel : MonoBehaviour
    {
        private const string RuntimeRootName = "ResearchHudLabel";

        // 視界の左上あたり。カメラのローカル座標（メートル）。
        private static readonly Vector3 LocalOffset = new Vector3(-0.22f, 0.17f, 0.6f);
        private const float CanvasScale = 0.0005f;
        private const float RefreshIntervalSec = 0.5f;

        private static readonly Color IdleColor = new Color(0.78f, 0.80f, 0.84f, 1f);
        private static readonly Color RecordingColor = new Color(0.45f, 0.92f, 0.55f, 1f);

        private Camera _camera;
        private TMP_Text _text;
        private string _shown;
        private bool _recording;
        private float _nextRefreshAt;

        /// <summary>
        /// ロガーと同じ自己起動。起動シーン（主菜单）には訓練コントローラがいないので、
        /// sceneLoaded でも拾い直す。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoadedForBootstrap;
            SceneManager.sceneLoaded += HandleSceneLoadedForBootstrap;

            TryCreateHost();
        }

        private static void HandleSceneLoadedForBootstrap(Scene scene, LoadSceneMode mode)
        {
            TryCreateHost();
        }

        private static void TryCreateHost()
        {
            if (FindObjectOfType<ResearchHudLabel>() != null)
                return;

            // 訓練シーンと主菜单の両方に出す。確定したあとも値が見えていないと、
            // 「設定画面では直したが、始める前にもう一度確かめる」ができない。
            if (FindObjectOfType<CPRTrainingController>() == null &&
                FindObjectOfType<MainMenuUI>() == null)
                return;

            var go = new GameObject(RuntimeRootName);
            go.AddComponent<ResearchHudLabel>();
        }

        private void OnEnable()
        {
            CPRTrainingController.OnSessionStarted += HandleSessionStarted;
            CPRTrainingController.OnSessionCompleted += HandleSessionCompleted;
        }

        private void OnDisable()
        {
            CPRTrainingController.OnSessionStarted -= HandleSessionStarted;
            CPRTrainingController.OnSessionCompleted -= HandleSessionCompleted;
        }

        private void HandleSessionStarted(CPRTrainingController controller)
        {
            _recording = true;
            _nextRefreshAt = 0f;
        }

        private void HandleSessionCompleted(CPRTrainingController controller)
        {
            _recording = false;
            _nextRefreshAt = 0f;
        }

        private void Update()
        {
            // OVRCameraRig はシーンの作りによっては数フレーム遅れて現れる。
            // 見つかるまで黙って待ち、見つかったらその場で作る。
            if (_text == null)
            {
                if (!TryResolveCamera())
                    return;

                BuildLabel();
                if (_text == null)
                    return;
            }

            if (Time.unscaledTime < _nextRefreshAt)
                return;

            _nextRefreshAt = Time.unscaledTime + RefreshIntervalSec;
            Refresh();
        }

        private bool TryResolveCamera()
        {
            if (_camera != null)
                return true;

            _camera = Camera.main;
            return _camera != null;
        }

        private void BuildLabel()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(_camera.transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(360f, 64f);
            canvasRect.localPosition = LocalOffset;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * CanvasScale;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            var backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.03f, 0.05f, 0.08f, 0.72f);
            // GraphicRaycaster は付けない＋ raycastTarget も切る。
            // 手のポーク判定を横取りして、後ろのボタンが押せなくなるのを避ける。
            background.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 6f);
            textRect.offsetMax = new Vector2(-14f, -6f);

            _text = textObject.AddComponent<TextMeshProUGUI>();
            // ASCII だけで組む。CJK フォントアトラスの読み込み状況に左右されたくない。
            _text.fontSize = 30f;
            _text.alignment = TextAlignmentOptions.Left;
            _text.enableWordWrapping = false;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.raycastTarget = false;

            _shown = null;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null)
                return;

            string line = (_recording ? "REC  " : "IDLE  ") + ResearchSession.Describe();
            if (line == _shown)
                return;

            _shown = line;
            _text.text = line;
            _text.color = _recording ? RecordingColor : IdleColor;
        }
    }
}
