using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CPRSimulation.Game;

namespace CPRSimulation.Research
{
    /// <summary>
    /// 起動直後に出す実験設定パネル。参加者ID・群・何回目をその場で入力して確定する。
    ///
    /// これまで実機で ID を変える手段は adb push した session_config.json だけだった。
    /// ヘッドセットを被ったまま直せないので、実験者は「前の参加者の設定のまま
    /// 次の人を 1 回ぶん記録してしまう」事故を防げない。CSV は後から見れば分かるが、
    /// そのときには参加者はもう帰っている。
    ///
    /// なので主菜单より手前に出し、確定するまで菜单を触れなくする。
    /// 毎回かならず目を通す場所を 1 つ作るのが目的で、省略できては意味がない。
    ///
    /// 表示は ASCII だけ。CJK フォントアトラスの読み込み状況に左右されたくない。
    /// 実験者が読むものなので、日本語でなくても困らない。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResearchSetupPanel : MonoBehaviour
    {
        private const string RuntimeRootName = "ResearchSetupPanel";

        /// <summary>参加者ID の数字部分の桁数。P001 まで。</summary>
        private const int MaxDigits = 3;

        private const float CanvasDistance = 1.6f;
        private const float CanvasScale = 0.002f;

        private static readonly Vector2 PanelSize = new Vector2(1160f, 860f);

        private static readonly Color PanelColor = new Color(0.02f, 0.04f, 0.06f, 0.95f);
        private static readonly Color FieldColor = new Color(0.05f, 0.09f, 0.13f, 1f);
        private static readonly Color KeyColor = new Color(0.16f, 0.20f, 0.24f, 1f);
        private static readonly Color SelectedColor = new Color(0.12f, 0.52f, 0.78f, 1f);
        private static readonly Color StartColor = new Color(0.16f, 0.58f, 0.32f, 1f);
        private static readonly Color StartBlockedColor = new Color(0.20f, 0.22f, 0.24f, 1f);
        private static readonly Color HintColor = new Color(0.64f, 0.68f, 0.72f, 1f);

        private Canvas _canvas;

        /// <summary>確定するまで伏せておく主菜单の Canvas。確定後に戻す。</summary>
        private GameObject _hiddenMenuRoot;

        private Text _idValue;
        private Button _startButton;
        private Image _startImage;
        private Button _groupAButton;
        private Button _groupBButton;
        private readonly Button[] _trialButtons = new Button[3];
        private Button _hideScoreButton;
        private Button _showScoreButton;

        private string _digits = string.Empty;
        private FeedbackCondition _condition = FeedbackCondition.VisualOnly;
        private int _trial = 1;
        private bool _hideScore = true;

        /// <summary>
        /// ロガーや HUD ラベルと同じ自己起動。ただし出すのは主菜单のシーンだけ
        /// （MainMenuUI がいるかどうかで判定する）。訓練シーンには出さない。
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
            if (FindObjectOfType<ResearchSetupPanel>() != null)
                return;

            if (FindObjectOfType<MainMenuUI>() == null)
                return;

            var go = new GameObject(RuntimeRootName);
            go.AddComponent<ResearchSetupPanel>();
        }

        private void Awake()
        {
            // 現在の設定を初期値にする。前の参加者の値がそのまま出るのが正しい ――
            // 「変え忘れ」はここで気づいてもらうためのもので、空欄から始めては意味がない。
            _digits = ExtractDigits(ResearchSession.ParticipantId);
            _condition = ResearchSession.Condition;
            _trial = ResearchSession.TrialIndex;
            _hideScore = ResearchSession.HideScoreFromParticipant;
        }

        private void Update()
        {
            // OVRCameraRig はシーンの作りによっては数フレーム遅れて現れる。
            if (_canvas != null)
                return;

            Transform anchor = ResolveAnchor();
            if (anchor == null)
                return;

            HideMainMenu();
            Build(anchor);
        }

        private static Transform ResolveAnchor()
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
                return rig.centerEyeAnchor;

            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>
        /// 主菜单を伏せる。Canvas コンポーネントを無効にするだけでは足りない ――
        /// HandPokeButtonInteractor は Button を名前ではなく有効な
        /// GameObject から拾うので、見えないまま押せてしまう。
        /// </summary>
        private void HideMainMenu()
        {
            var menu = FindObjectOfType<MainMenuUI>();
            if (menu == null)
                return;

            Canvas menuCanvas = menu.GetComponentInParent<Canvas>();
            if (menuCanvas == null)
                return;

            _hiddenMenuRoot = menuCanvas.gameObject;
            _hiddenMenuRoot.SetActive(false);
        }

        private void RestoreMainMenu()
        {
            if (_hiddenMenuRoot == null)
                return;

            _hiddenMenuRoot.SetActive(true);

            // 伏せている間に Start() が持ち越されていた場合、ここで走って菜单を組み立てる。
            // すでに走っていたなら、選択中のパネルを出し直すだけ。
            var menu = _hiddenMenuRoot.GetComponentInChildren<MainMenuUI>(true);
            if (menu != null)
                menu.ShowMainMenu();

            _hiddenMenuRoot = null;
        }

        private void Build(Transform anchor)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(anchor, false);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            Camera anchorCamera = anchor.GetComponent<Camera>();
            _canvas.worldCamera = anchorCamera != null ? anchorCamera : Camera.main;
            // 主菜单と同じ距離に置くと z ファイティングを起こすので、少し手前に出す。
            _canvas.sortingOrder = 100;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.localPosition = new Vector3(0f, 0f, CanvasDistance);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * CanvasScale;

            GameObject panel = CreateRect("Panel", canvasObject.transform, Vector2.zero, PanelSize);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;

            Transform root = panel.transform;

            CreateLabel(root, "Title", "RESEARCH SETUP", new Vector2(0f, 370f), new Vector2(1000f, 70f), 44);
            CreateLabel(root, "Hint", "Check against the allocation sheet before each run.",
                new Vector2(0f, 312f), new Vector2(1000f, 44f), 26, HintColor);

            GameObject field = CreateRect("IdField", root, new Vector2(0f, 215f), new Vector2(1000f, 120f));
            var fieldImage = field.AddComponent<Image>();
            fieldImage.color = FieldColor;
            fieldImage.raycastTarget = false;
            _idValue = CreateLabel(field.transform, "Value", "P00", Vector2.zero, new Vector2(960f, 110f), 84);

            BuildKeypad(root);
            BuildOptions(root);

            _startButton = CreateButton(root, "StartButton", "START", new Vector2(0f, -370f),
                new Vector2(460f, 110f), StartColor, 38);
            _startImage = _startButton.GetComponent<Image>();
            _startButton.onClick.AddListener(Confirm);

            RefreshAll();
        }

        private void BuildKeypad(Transform root)
        {
            // 1..9 / DEL 0 CLR。数字だけなので VR でも押し間違えにくい。
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "DEL", "0", "CLR" };
            float[] columns = { -430f, -310f, -190f };
            float[] rows = { 90f, -25f, -140f, -255f };

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                Vector2 position = new Vector2(columns[i % 3], rows[i / 3]);
                Button button = CreateButton(root, "Key_" + key, key, position,
                    new Vector2(110f, 100f), KeyColor, key.Length > 1 ? 26 : 40);
                button.onClick.AddListener(() => OnKey(key));
            }
        }

        private void BuildOptions(Transform root)
        {
            CreateLabel(root, "GroupLabel", "GROUP", new Vector2(70f, 90f), new Vector2(180f, 60f), 30, HintColor);
            _groupAButton = CreateButton(root, "GroupA", "A", new Vector2(270f, 90f), new Vector2(130f, 100f), KeyColor, 40);
            _groupBButton = CreateButton(root, "GroupB", "B", new Vector2(420f, 90f), new Vector2(130f, 100f), KeyColor, 40);
            _groupAButton.onClick.AddListener(() => { _condition = FeedbackCondition.VisualOnly; RefreshAll(); });
            _groupBButton.onClick.AddListener(() => { _condition = FeedbackCondition.VisualHaptic; RefreshAll(); });

            CreateLabel(root, "TrialLabel", "TRIAL", new Vector2(70f, -25f), new Vector2(180f, 60f), 30, HintColor);
            for (int i = 0; i < _trialButtons.Length; i++)
            {
                int trial = i + 1;
                _trialButtons[i] = CreateButton(root, "Trial" + trial, trial.ToString(),
                    new Vector2(250f + i * 115f, -25f), new Vector2(100f, 100f), KeyColor, 36);
                _trialButtons[i].onClick.AddListener(() => { _trial = trial; RefreshAll(); });
            }

            CreateLabel(root, "ScoreLabel", "SCORE", new Vector2(70f, -140f), new Vector2(180f, 60f), 30, HintColor);
            _hideScoreButton = CreateButton(root, "ScoreHide", "HIDE", new Vector2(290f, -140f),
                new Vector2(170f, 100f), KeyColor, 28);
            _showScoreButton = CreateButton(root, "ScoreShow", "SHOW", new Vector2(475f, -140f),
                new Vector2(170f, 100f), KeyColor, 28);
            _hideScoreButton.onClick.AddListener(() => { _hideScore = true; RefreshAll(); });
            _showScoreButton.onClick.AddListener(() => { _hideScore = false; RefreshAll(); });

            CreateLabel(root, "ScoreHint", "whether the participant sees their score",
                new Vector2(275f, -215f), new Vector2(560f, 40f), 22, HintColor);
        }

        private void OnKey(string key)
        {
            if (key == "DEL")
            {
                if (_digits.Length > 0)
                    _digits = _digits.Substring(0, _digits.Length - 1);
            }
            else if (key == "CLR")
            {
                _digits = string.Empty;
            }
            else if (_digits.Length < MaxDigits)
            {
                _digits += key;
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            bool hasId = _digits.Length > 0;

            if (_idValue != null)
                _idValue.text = hasId ? "P" + _digits : "P---";

            SetSelected(_groupAButton, _condition == FeedbackCondition.VisualOnly);
            SetSelected(_groupBButton, _condition == FeedbackCondition.VisualHaptic);

            for (int i = 0; i < _trialButtons.Length; i++)
                SetSelected(_trialButtons[i], _trial == i + 1);

            SetSelected(_hideScoreButton, _hideScore);
            SetSelected(_showScoreButton, !_hideScore);

            // ID が空のまま始められては、この画面を出した意味がなくなる。
            if (_startButton != null)
                _startButton.interactable = hasId;
            if (_startImage != null)
                _startImage.color = hasId ? StartColor : StartBlockedColor;
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = selected ? SelectedColor : KeyColor;

            var outline = button.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = selected
                    ? new Color(1f, 1f, 1f, 0.85f)
                    : new Color(1f, 1f, 1f, 0.24f);
        }

        private void Confirm()
        {
            if (_digits.Length == 0)
                return;

            ResearchSession.ParticipantId = "P" + _digits;
            ResearchSession.Condition = _condition;
            ResearchSession.TrialIndex = _trial;
            ResearchSession.HideScoreFromParticipant = _hideScore;

            Debug.Log("[Research] 設定を確定: " + ResearchSession.Describe()
                + " / score " + (_hideScore ? "hidden" : "shown"));

            RestoreMainMenu();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 確定せずに消えた場合でも菜单を出し直す。触れない画面だけが残るのが最悪。
            RestoreMainMenu();
        }

        // ───────────────────────────────── 部品づくり

        private static GameObject CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return go;
        }

        private static Text CreateLabel(Transform parent, string name, string value,
            Vector2 position, Vector2 size, int fontSize)
        {
            return CreateLabel(parent, name, value, position, size, fontSize, Color.white);
        }

        private static Text CreateLabel(Transform parent, string name, string value,
            Vector2 position, Vector2 size, int fontSize, Color color)
        {
            GameObject go = CreateRect(name, parent, position, size);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 押せるのはボタンだけにする。ラベルが指の判定を横取りしないように。
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color color, int fontSize)
        {
            GameObject go = CreateRect(name, parent, position, size);

            var image = go.AddComponent<Image>();
            image.color = color;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.24f);
            outline.effectDistance = new Vector2(2f, -2f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateLabel(go.transform, "Label", label, Vector2.zero, size, fontSize);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>"P07" から "07" を取り出す。書式が崩れていても数字だけ拾う。</summary>
        private static string ExtractDigits(string participantId)
        {
            if (string.IsNullOrEmpty(participantId))
                return string.Empty;

            var sb = new System.Text.StringBuilder(MaxDigits);
            foreach (char c in participantId)
            {
                if (c >= '0' && c <= '9' && sb.Length < MaxDigits)
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
