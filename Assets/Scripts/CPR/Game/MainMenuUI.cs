using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CPRSimulation;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 主菜单UI控制器 - 支持中文 / 日語 / English 三语言切换
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI面板")]
        public GameObject mainMenuPanel;
        public GameObject tutorialPanel;
        public GameObject settingsPanel;
        public GameObject trainingModePanel;

        [Header("主菜单按钮")]
        public Button startButton;
        public Button tutorialButton;
        public Button settingsButton;
        public Button quitButton;
        public Button storyModeButton;
        public Button cprModeButton;
        public Button aedModeButton;
        public Button quizModeButton;
        public Button backFromTrainingModeButton;
        public Button exitFromTrainingModeButton;

        [Header("教程面板按钮")]
        public Button beginTrainingButton;
        public Button backFromTutorialButton;

        [Header("场景设置")]
        public string trainingSceneName = "Assets/demo/0.7.unity";

        [Header("VR设置")]
        public bool isVRMode = true;

        // 运行时引用 - 主菜单文字
        private Text _titleText;
        private Text _subTitleText;
        private Text _startBtnText;
        private Text _tutorialBtnText;
        private Text _quitBtnText;
        private Text _beginTrainingBtnText;
        private Text _backBtnText;
        private Text _trainingModeTitleText;
        private Text _storyModeBtnText;
        private Text _cprModeBtnText;
        private Text _aedModeBtnText;
        private Text _quizModeBtnText;
        private Text _backFromTrainingModeBtnText;
        private Text _exitFromTrainingModeBtnText;

        private const string FallbackTrainingScenePath = "Assets/demo/0.7.unity";

        // 语言选择按钮（运行时创建）
        private Button _langCNBtn;
        private Button _langJPBtn;
        private Button _langENBtn;

        private void Start()
        {
            // Quest/VR 下将 Screen Space Overlay Canvas 转为 World Space
            CPRSimulation.VRCanvasSetup.SetupAllCanvasesForVR();
            EnsureLanguageManager();
            SetupButtons();
            BuildLanguageSelectButtons();
            ShowMainMenu();
            RefreshAllTexts();

            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged += RefreshAllTexts;
        }

        private void OnDestroy()
        {
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= RefreshAllTexts;
        }

        /// <summary>确保场景中有 LanguageManager（若无则自动创建）</summary>
        private void EnsureLanguageManager()
        {
            if (LanguageManager.Instance == null)
            {
                var go = new GameObject("LanguageManager");
                go.AddComponent<LanguageManager>();
                Debug.Log("[MainMenuUI] 自动创建 LanguageManager");
            }
        }

        private void SetupButtons()
        {
            EnsureTrainingModePanel();

            // 主菜单按钮
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (tutorialButton != null)
                tutorialButton.onClick.AddListener(OnTutorialClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // 教程面板按钮
            if (beginTrainingButton != null)
                beginTrainingButton.onClick.AddListener(OnBeginTrainingClicked);

            if (backFromTutorialButton != null)
                backFromTutorialButton.onClick.AddListener(ShowMainMenu);

            if (storyModeButton != null)
                storyModeButton.onClick.AddListener(() => OnTrainingModeClicked(CPRTrainingController.TrainingMode.Story));

            if (cprModeButton != null)
                cprModeButton.onClick.AddListener(() => OnTrainingModeClicked(CPRTrainingController.TrainingMode.CPR));

            if (aedModeButton != null)
                aedModeButton.onClick.AddListener(() => OnTrainingModeClicked(CPRTrainingController.TrainingMode.AED));

            if (quizModeButton != null)
                quizModeButton.onClick.AddListener(() => OnTrainingModeClicked(CPRTrainingController.TrainingMode.Quiz));

            if (backFromTrainingModeButton != null)
                backFromTrainingModeButton.onClick.AddListener(ShowMainMenu);

            if (exitFromTrainingModeButton != null)
                exitFromTrainingModeButton.onClick.AddListener(OnQuitClicked);

            // 缓存文字引用（从按钮子节点找 Text）
            _startBtnText       = GetBtnText(startButton);
            _tutorialBtnText    = GetBtnText(tutorialButton);
            _quitBtnText        = GetBtnText(quitButton);
            _beginTrainingBtnText = GetBtnText(beginTrainingButton);
            _backBtnText        = GetBtnText(backFromTutorialButton);
            _storyModeBtnText = GetBtnText(storyModeButton);
            _cprModeBtnText = GetBtnText(cprModeButton);
            _aedModeBtnText = GetBtnText(aedModeButton);
            _quizModeBtnText = GetBtnText(quizModeButton);
            _backFromTrainingModeBtnText = GetBtnText(backFromTrainingModeButton);
            _exitFromTrainingModeBtnText = GetBtnText(exitFromTrainingModeButton);

            // 从 mainMenuPanel 找 Title / SubTitle 文字
            if (mainMenuPanel != null)
            {
                var texts = mainMenuPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    string name = t.gameObject.name.ToLower();
                    if (name.Contains("title") && !name.Contains("sub") && _titleText == null)
                        _titleText = t;
                    else if ((name.Contains("sub") || name.Contains("desc") || name.Contains("quest")) && _subTitleText == null)
                        _subTitleText = t;
                }
            }
        }

        private Text GetBtnText(Button btn)
        {
            if (btn == null) return null;
            return btn.GetComponentInChildren<Text>(true);
        }

        private void EnsureTrainingModePanel()
        {
            if (trainingModePanel == null)
            {
                Transform searchRoot = mainMenuPanel != null && mainMenuPanel.transform.parent != null
                    ? mainMenuPanel.transform.parent
                    : transform;
                Transform existing = searchRoot.Find("TrainingModePanel");
                if (existing != null)
                    trainingModePanel = existing.gameObject;
            }

            if (trainingModePanel == null)
            {
                Transform parent = mainMenuPanel != null && mainMenuPanel.transform.parent != null
                    ? mainMenuPanel.transform.parent
                    : transform;

                trainingModePanel = new GameObject("TrainingModePanel", typeof(RectTransform), typeof(Image));
                trainingModePanel.transform.SetParent(parent, false);

                RectTransform panelRect = trainingModePanel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(900f, 620f);

                Image panelImage = trainingModePanel.GetComponent<Image>();
                panelImage.color = new Color(0.02f, 0.04f, 0.05f, 0.82f);
            }

            Transform panel = trainingModePanel.transform;

            _trainingModeTitleText = EnsureMenuText(
                panel,
                "TrainingModeTitle",
                GetTrainingModeTitleLabel(),
                new Vector2(0f, 235f),
                new Vector2(760f, 70f),
                42
            );

            if (storyModeButton == null)
                storyModeButton = FindMenuButton("StoryModeButton") ?? CreateRuntimeModeCard("StoryModeButton", panel, new Vector2(-225f, 95f), CPRTrainingController.TrainingMode.Story);

            if (cprModeButton == null)
                cprModeButton = FindMenuButton("CPRModeButton") ?? CreateRuntimeModeCard("CPRModeButton", panel, new Vector2(225f, 95f), CPRTrainingController.TrainingMode.CPR);

            if (aedModeButton == null)
                aedModeButton = FindMenuButton("AEDModeButton") ?? CreateRuntimeModeCard("AEDModeButton", panel, new Vector2(-225f, -35f), CPRTrainingController.TrainingMode.AED);

            if (quizModeButton == null)
                quizModeButton = FindMenuButton("QuizModeButton") ?? CreateRuntimeModeCard("QuizModeButton", panel, new Vector2(225f, -35f), CPRTrainingController.TrainingMode.Quiz);

            if (backFromTrainingModeButton == null)
                backFromTrainingModeButton = FindMenuButton("BackFromTrainingModeButton") ?? CreateRuntimeMenuButton("BackFromTrainingModeButton", panel, new Vector2(-160f, -205f), new Color(0.18f, 0.22f, 0.26f, 0.95f), new Vector2(260f, 72f));

            if (exitFromTrainingModeButton == null)
                exitFromTrainingModeButton = FindMenuButton("ExitFromTrainingModeButton") ?? CreateRuntimeMenuButton("ExitFromTrainingModeButton", panel, new Vector2(160f, -205f), new Color(0.18f, 0.22f, 0.26f, 0.95f), new Vector2(260f, 72f));

            trainingModePanel.SetActive(false);
        }

        private Text EnsureMenuText(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
        {
            Transform existing = parent.Find(name);
            GameObject textObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Text));

            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(16, fontSize - 12);
            label.resizeTextMaxSize = fontSize;
            return label;
        }

        private Button FindMenuButton(string name)
        {
            if (trainingModePanel == null)
                return null;

            Button[] buttons = trainingModePanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button != null && button.gameObject.name == name)
                    return button;
            }

            return null;
        }

        private Button CreateRuntimeMenuButton(string name, Transform parent, Vector2 position, Color color)
        {
            return CreateRuntimeMenuButton(name, parent, position, color, new Vector2(400f, 116f));
        }

        private Button CreateRuntimeMenuButton(string name, Transform parent, Vector2 position, Color color, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.24f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.supportRichText = true;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 26;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary);
            VRTrainingUIStyle.ApplyButton(button, color);
            return button;
        }

        private Button CreateRuntimeModeCard(string name, Transform parent, Vector2 position, CPRTrainingController.TrainingMode mode)
        {
            TrainingModeCardDefinition definition = TrainingModeCatalog.Get(mode);
            Button button = CreateRuntimeMenuButton(name, parent, position, definition.AccentColor);
            var cardView = button.GetComponent<TrainingModeCardView>();
            if (cardView == null)
                cardView = button.gameObject.AddComponent<TrainingModeCardView>();
            cardView.Apply(definition, 20, 18);
            return button;
        }

        /// <summary>在主菜单面板顶部自动创建语言切换按钮行</summary>
        private void BuildLanguageSelectButtons()
        {
            // 找 Canvas（从自身往上找）
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MainMenuUI] 未找到 Canvas，无法创建语言切换按钮");
                return;
            }

            // 容器 Panel
            var container = new GameObject("LanguageSelectPanel", typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);

            var panelRt = container.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot     = new Vector2(0.5f, 1f);
            // 放在 Canvas 正上方，稍微往下偏移
            panelRt.anchoredPosition = new Vector2(0f, -20f);
            panelRt.sizeDelta        = new Vector2(560f, 70f);

            // 半透明背景
            var bg = container.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            // 三个语言按钮
            string[] labels = { CPRTextLibrary.LangBtnChinese, CPRTextLibrary.LangBtnJapanese, CPRTextLibrary.LangBtnEnglish };
            float[] xPositions = { -180f, 0f, 180f };
            Language[] langs   = { Language.Chinese, Language.Japanese, Language.English };

            for (int i = 0; i < 3; i++)
            {
                var btnGo = new GameObject($"LangBtn_{langs[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(container.transform, false);

                var rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(160f, 54f);
                rt.anchoredPosition = new Vector2(xPositions[i], 0f);

                var img = btnGo.GetComponent<Image>();
                img.color = GetLangBtnColor(langs[i]);

                var btn = btnGo.GetComponent<Button>();
                btn.targetGraphic = img;

                // 高亮颜色
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.15f, 0.85f, 1f, 1f);
                colors.pressedColor     = new Color(0.05f, 0.55f, 1f, 1f);
                colors.selectedColor    = colors.highlightedColor;
                btn.colors = colors;

                // Outline
                var outline = btnGo.AddComponent<Outline>();
                outline.effectColor    = new Color(1f, 1f, 1f, 0.5f);
                outline.effectDistance = new Vector2(2f, -2f);

                // 文字
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(btnGo.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                var ltxt = labelGo.GetComponent<Text>();
                ltxt.text      = labels[i];
                ltxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ltxt.fontSize  = 26;
                ltxt.alignment = TextAnchor.MiddleCenter;
                ltxt.color     = Color.white;

                // 绑定点击
                Language captured = langs[i];
                btn.onClick.AddListener(() => OnLangBtnClicked(captured));

                // 缓存引用
                if (langs[i] == Language.Chinese)  _langCNBtn = btn;
                if (langs[i] == Language.Japanese) _langJPBtn = btn;
                if (langs[i] == Language.English)  _langENBtn = btn;
            }

            // 初始高亮当前语言按钮
            UpdateLangButtonHighlight();

            Debug.Log("[MainMenuUI] 语言切换按钮已创建");
        }

        private Color GetLangBtnColor(Language lang)
        {
            return lang switch
            {
                Language.Chinese  => new Color(0.85f, 0.2f, 0.2f, 0.85f),
                Language.Japanese => new Color(0.15f, 0.45f, 0.85f, 0.85f),
                Language.English  => new Color(0.15f, 0.55f, 0.25f, 0.85f),
                _                 => new Color(0.3f, 0.3f, 0.3f, 0.85f)
            };
        }

        private void OnLangBtnClicked(Language lang)
        {
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.SetLanguage(lang);
            UpdateLangButtonHighlight();
        }

        private void UpdateLangButtonHighlight()
        {
            if (LanguageManager.Instance == null) return;
            Language cur = LanguageManager.Instance.CurrentLanguage;

            SetBtnHighlight(_langCNBtn, cur == Language.Chinese);
            SetBtnHighlight(_langJPBtn, cur == Language.Japanese);
            SetBtnHighlight(_langENBtn, cur == Language.English);
        }

        private void SetBtnHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            var c = img.color;
            img.color = active ? new Color(c.r, c.g, c.b, 1f) : new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.7f);

            var outline = btn.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = active ? new Color(1f, 1f, 0.3f, 1f) : new Color(1f, 1f, 1f, 0.4f);
        }

        /// <summary>语言切换时刷新所有文字</summary>
        private void RefreshAllTexts()
        {
            if (_titleText    != null) _titleText.text    = CPRTextLibrary.MainTitle;
            if (_subTitleText != null) _subTitleText.text = CPRTextLibrary.SubTitle;

            if (_startBtnText         != null) _startBtnText.text         = GetTrainingModeEntryLabel();
            if (_tutorialBtnText      != null) _tutorialBtnText.text      = CPRTextLibrary.BtnTutorial;
            if (_quitBtnText          != null) _quitBtnText.text          = CPRTextLibrary.BtnQuit;
            if (_beginTrainingBtnText != null) _beginTrainingBtnText.text = GetTrainingModeEntryLabel();
            if (_backBtnText          != null) _backBtnText.text          = CPRTextLibrary.BtnBack;
            if (_trainingModeTitleText != null) _trainingModeTitleText.text = GetTrainingModeTitleLabel();
            ApplyModeCard(storyModeButton, _storyModeBtnText, CPRTrainingController.TrainingMode.Story);
            ApplyModeCard(cprModeButton, _cprModeBtnText, CPRTrainingController.TrainingMode.CPR);
            ApplyModeCard(aedModeButton, _aedModeBtnText, CPRTrainingController.TrainingMode.AED);
            ApplyModeCard(quizModeButton, _quizModeBtnText, CPRTrainingController.TrainingMode.Quiz);
            if (_backFromTrainingModeBtnText != null) _backFromTrainingModeBtnText.text = GetBackButtonLabel();
            if (_exitFromTrainingModeBtnText != null) _exitFromTrainingModeBtnText.text = GetExitButtonLabel();

            UpdateLangButtonHighlight();

            Debug.Log($"[MainMenuUI] UI文字已刷新 → {LanguageManager.Instance?.CurrentLanguage}");
        }

        private void ApplyModeCard(Button button, Text text, CPRTrainingController.TrainingMode mode)
        {
            TrainingModeCardDefinition definition = TrainingModeCatalog.Get(mode);
            if (button != null)
            {
                var cardView = button.GetComponent<TrainingModeCardView>();
                if (cardView == null)
                    cardView = button.gameObject.AddComponent<TrainingModeCardView>();
                cardView.Apply(definition, 20, 18);
                text = cardView.Label ?? text;
            }

            SetModeCardText(text, definition.BuildRichLabel(20, 18));
        }

        private string GetTrainingModeEntryLabel()
        {
            return TrainingModeCatalog.EntryLabel;
        }

        private string GetTrainingModeTitleLabel()
        {
            return TrainingModeCatalog.SelectionTitle;
        }







        private void SetModeCardText(Text text, string value)
        {
            if (text == null)
                return;

            text.supportRichText = true;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 26;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
        }




        private string GetBackButtonLabel()
        {
            return LanguageManager.Get(
                "\u8fd4\u56de",
                "\u623b\u308b",
                "Back"
            );
        }

        private string GetExitButtonLabel()
        {
            return LanguageManager.Get(
                "\u9000\u51fa",
                "\u7d42\u4e86",
                "Exit"
            );
        }

        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(tutorialPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(trainingModePanel, false);
        }

        private void ShowTrainingModePanel()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(tutorialPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(trainingModePanel, true);
            RefreshAllTexts();
        }

        private void OnStartClicked() => ShowTrainingModePanel();
        private void OnTutorialClicked() { SetPanelActive(mainMenuPanel, false); SetPanelActive(tutorialPanel, true); SetPanelActive(trainingModePanel, false); }
        private void OnSettingsClicked() { SetPanelActive(mainMenuPanel, false); SetPanelActive(settingsPanel, true); SetPanelActive(trainingModePanel, false); }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnBeginTrainingClicked() => ShowTrainingModePanel();

        private void OnTrainingModeClicked(CPRTrainingController.TrainingMode mode)
        {
            CPRTrainingController.SetRequestedTrainingMode(mode);
            LoadTrainingScene();
        }

        private void LoadTrainingScene()
        {
            string sceneToLoad = ResolveTrainingSceneName();
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogWarning("[MainMenuUI] No loadable training scene found.");
                return;
            }

            Debug.Log($"[MainMenuUI] 加载训练场景: {trainingSceneName}");
            SceneManager.LoadScene(sceneToLoad);
        }

        private string ResolveTrainingSceneName()
        {
            if (!string.IsNullOrEmpty(trainingSceneName) &&
                Application.CanStreamedLevelBeLoaded(trainingSceneName))
            {
                return trainingSceneName;
            }

            if (Application.CanStreamedLevelBeLoaded(FallbackTrainingScenePath))
                return FallbackTrainingScenePath;

            if (Application.CanStreamedLevelBeLoaded("0.7"))
                return "0.7";

            return string.Empty;
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
