using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation.Knowledge
{
    /// <summary>一次测验的结果。</summary>
    public struct QuizResult
    {
        public int correct;
        public int total;
        /// <summary>答错题目所属的主题 id，用于提示复习方向。</summary>
        public List<string> wrongTopics;
        /// <summary>答错题目建议的实操模式（CPR / AED / Story），已去重。</summary>
        public List<string> suggestedPractice;
    }

    /// <summary>
    /// 知识测验的界面与流程。自己在挂载物体下搭 UI 并复用，
    /// 不放进 CPRTrainingController —— 那个文件已经 4200 多行。
    ///
    /// 单选题和判断题共用这一条渲染路径：判断题就是只有两个选项的单选题。
    /// </summary>
    public sealed class KnowledgeQuizView : MonoBehaviour
    {
        private const int MaxOptionButtons = 6;

        // 面板 920x640，y 范围 -320..+320
        public const float PanelWidth = 920f;
        public const float PanelHeight = 640f;

        [Tooltip("是否打乱题目顺序。调试期建议关闭，保持顺序可复现。")]
        public bool shuffleQuestions = false;

        private GameObject _root;
        private TMP_Text _progressText;
        private TMP_Text _stemText;
        private TMP_Text _feedbackText;
        private Button _nextButton;
        private TMP_Text _nextLabel;

        private readonly List<Button> _optionButtons = new List<Button>(MaxOptionButtons);
        private readonly List<TMP_Text> _optionLabels = new List<TMP_Text>(MaxOptionButtons);

        private QuizBank _bank;
        private List<QuizQuestion> _order;
        private int _index;
        private int _correct;
        private bool _answered;
        // 语言切换时要按新语言重画反馈行，所以记「答得对不对」而不是已拼好的字符串
        private bool _lastAnswerCorrect;
        private LanguageManager _languageHookedTo;
        private Action<QuizResult> _onFinished;
        private readonly List<string> _wrongTopics = new List<string>();
        private readonly List<string> _suggestedPractice = new List<string>();

        /// <summary>开始一轮测验。bank 为 null 或没有题目时直接报错返回 false。</summary>
        public bool Begin(QuizBank bank, Action<QuizResult> onFinished)
        {
            SubscribeToLanguageChanges();

            if (bank == null || bank.questions == null || bank.questions.Length == 0)
            {
                Debug.LogError("[KnowledgeQuiz] 题库为空，无法开始测验");
                return false;
            }

            _bank = bank;
            _onFinished = onFinished;

            _order = new List<QuizQuestion>(bank.questions.Length);
            foreach (QuizQuestion q in bank.questions)
            {
                if (q != null)
                    _order.Add(q);
            }

            if (shuffleQuestions)
                Shuffle(_order);

            _index = 0;
            _correct = 0;
            _wrongTopics.Clear();
            _suggestedPractice.Clear();

            EnsureUI();
            SetUIVisible(true);
            ShowCurrent();
            return true;
        }

        private static void Shuffle(List<QuizQuestion> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ------------------------------------------------------------ UI 搭建

        /// <summary>
        /// 显示/隐藏整套题目 UI。做完题后要让位给结果文字 ——
        /// 两者都是面板的子物体，不隐藏会叠在一起。
        /// </summary>
        public void SetUIVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        private void EnsureUI()
        {
            if (_stemText != null)
                return;

            // 所有题目 UI 挂在一个容器下，这样隐藏只需一次 SetActive
            _root = new GameObject("QuizRoot", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;

            _progressText = MakeText("QuizProgress", new Vector2(0f, 285f), new Vector2(840f, 40f),
                                     22, TextAlignmentOptions.Center, UIColorScheme.TextSecondary);

            _stemText = MakeText("QuizStem", new Vector2(0f, 200f), new Vector2(840f, 120f),
                                 30, TextAlignmentOptions.Center, UIColorScheme.TextPrimary);

            for (int i = 0; i < MaxOptionButtons; i++)
            {
                int captured = i;   // 闭包必须捕获副本，否则所有按钮都指向最后一个下标
                Button btn = MakeButton($"QuizOption{i}", new Vector2(0f, 80f - i * 68f),
                                        new Vector2(740f, 60f), out TMP_Text label);
                btn.onClick.AddListener(() => OnOptionClicked(captured));
                _optionButtons.Add(btn);
                _optionLabels.Add(label);
            }

            _feedbackText = MakeText("QuizFeedback", new Vector2(0f, -215f), new Vector2(840f, 120f),
                                     23, TextAlignmentOptions.Top, UIColorScheme.TextSecondary);

            _nextButton = MakeButton("QuizNext", new Vector2(0f, -292f), new Vector2(240f, 52f),
                                     out _nextLabel);
            _nextButton.onClick.AddListener(OnNextClicked);
        }

        private TMP_Text MakeText(string name, Vector2 pos, Vector2 size, int fontSize,
                                  TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root != null ? _root.transform : transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12, fontSize - 8);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            VRTrainingUIStyle.ApplyText(text, color, fontSize >= 30);
            return text;
        }

        private Button MakeButton(string name, Vector2 pos, Vector2 size, out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root != null ? _root.transform : transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            Button btn = go.AddComponent<Button>();
            VRTrainingUIStyle.ApplyButton(btn, UIColorScheme.Primary);

            label = MakeText(name + "Label", Vector2.zero, new Vector2(size.x - 40f, size.y - 12f),
                             26, TextAlignmentOptions.Center, UIColorScheme.TextPrimary);
            label.transform.SetParent(go.transform, false);
            var lrt = label.transform as RectTransform;
            lrt.anchoredPosition = Vector2.zero;

            return btn;
        }

        // ------------------------------------------------------------ 流程

        private QuizQuestion Current =>
            (_order != null && _index >= 0 && _index < _order.Count) ? _order[_index] : null;

        private void ShowCurrent()
        {
            QuizQuestion q = Current;
            if (q == null)
            {
                Finish();
                return;
            }

            _answered = false;

            RenderQuestionTexts();

            int count = q.options != null ? Mathf.Min(q.options.Length, MaxOptionButtons) : 0;
            for (int i = 0; i < count; i++)
            {
                _optionButtons[i].interactable = true;
                VRTrainingUIStyle.ApplyButton(_optionButtons[i], UIColorScheme.Primary);
            }

            _feedbackText.text = string.Empty;
            _nextButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// 只写文字，不碰作答状态和按钮配色。ShowCurrent 和语言切换都走这里 ——
        /// 切语言直接调 ShowCurrent 的话会把 _answered 重置，已经答过的题能再答一次。
        /// </summary>
        private void RenderQuestionTexts()
        {
            QuizQuestion q = Current;
            if (q == null)
                return;

            _progressText.text = LanguageManager.Get(
                $"第 {_index + 1} / {_order.Count} 题",
                $"問題 {_index + 1} / {_order.Count}",
                $"Question {_index + 1} / {_order.Count}");

            _stemText.text = q.stem != null ? q.stem.Resolve() : "(missing stem)";

            int count = q.options != null ? Mathf.Min(q.options.Length, MaxOptionButtons) : 0;
            for (int i = 0; i < MaxOptionButtons; i++)
            {
                bool used = i < count;
                _optionButtons[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                _optionLabels[i].text = q.options[i] != null ? q.options[i].Resolve() : "(missing option)";
            }
        }

        /// <summary>
        /// 语言切换时重画。这个类有 7 处 LanguageManager.Get 却从没订阅过
        /// OnLanguageChanged —— 题目画出来之后就锁死在当时的语言上。
        /// </summary>
        private void RefreshLocalizedUi()
        {
            if (_root == null || Current == null)
                return;

            RenderQuestionTexts();

            if (_answered)
                RenderFeedback(_lastAnswerCorrect);
        }

        private void OnEnable()
        {
            SubscribeToLanguageChanges();
        }

        private void OnDisable()
        {
            if (_languageHookedTo != null)
            {
                _languageHookedTo.OnLanguageChanged -= RefreshLocalizedUi;
                _languageHookedTo = null;
            }
        }

        /// <summary>
        /// 本组件是运行时 AddComponent 的，OnEnable 时 LanguageManager 可能还没建好，
        /// 所以 Begin() 里再补订阅一次。
        /// </summary>
        private void SubscribeToLanguageChanges()
        {
            LanguageManager manager = LanguageManager.Instance;
            if (manager == null || ReferenceEquals(manager, _languageHookedTo))
                return;

            if (_languageHookedTo != null)
                _languageHookedTo.OnLanguageChanged -= RefreshLocalizedUi;

            _languageHookedTo = manager;
            manager.OnLanguageChanged += RefreshLocalizedUi;
        }

        private void OnOptionClicked(int optionIndex)
        {
            if (_answered)
                return;

            QuizQuestion q = Current;
            if (q == null || q.options == null || optionIndex >= q.options.Length)
                return;

            _answered = true;
            bool right = optionIndex == q.answer;

            if (right)
            {
                _correct++;
            }
            else
            {
                if (!string.IsNullOrEmpty(q.topic) && !_wrongTopics.Contains(q.topic))
                    _wrongTopics.Add(q.topic);
                if (!string.IsNullOrEmpty(q.practiceMode) && !_suggestedPractice.Contains(q.practiceMode))
                    _suggestedPractice.Add(q.practiceMode);
            }

            // 选中的按钮染成对/错，同时把正确答案标出来 ——
            // 答错时只说「错了」没有教学价值，必须让学员看到正确的是哪个
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                if (!_optionButtons[i].gameObject.activeSelf)
                    continue;

                _optionButtons[i].interactable = false;

                if (i == q.answer)
                    VRTrainingUIStyle.ApplyButton(_optionButtons[i], UIColorScheme.Success);
                else if (i == optionIndex)
                    VRTrainingUIStyle.ApplyButton(_optionButtons[i], UIColorScheme.Danger);
            }

            _lastAnswerCorrect = right;
            RenderFeedback(right);
            _nextButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 反馈行和「下一题」按钮的文字。答题时和语言切换时共用，
        /// 这样切语言不用重走 OnOptionClicked（那会重复计分）。
        /// </summary>
        private void RenderFeedback(bool right)
        {
            QuizQuestion q = Current;
            if (q == null)
                return;

            string head = right
                ? LanguageManager.Get("正确", "正解", "Correct")
                : LanguageManager.Get("不正确", "不正解", "Incorrect");

            string body = q.explanation != null && !q.explanation.IsEmpty
                ? q.explanation.Resolve()
                : string.Empty;

            _feedbackText.text = string.IsNullOrEmpty(body) ? head : head + "\n" + body;
            _feedbackText.color = right ? UIColorScheme.Success : UIColorScheme.Warning;

            bool last = _index >= _order.Count - 1;
            _nextLabel.text = last
                ? LanguageManager.Get("查看结果", "結果を見る", "See results")
                : LanguageManager.Get("下一题", "次の問題", "Next question");
        }

        private void OnNextClicked()
        {
            if (!_answered)
                return;

            _index++;
            if (_index >= _order.Count)
                Finish();
            else
                ShowCurrent();
        }

        private void Finish()
        {
            var result = new QuizResult
            {
                correct = _correct,
                total = _order != null ? _order.Count : 0,
                wrongTopics = new List<string>(_wrongTopics),
                suggestedPractice = new List<string>(_suggestedPractice)
            };

            SetUIVisible(false);

            Debug.Log($"[KnowledgeQuiz] 完成：{result.correct}/{result.total} 正确" +
                      (result.wrongTopics.Count > 0
                          ? $"，需复习主题：{string.Join("、", result.wrongTopics)}"
                          : ""));

            // 研究用ログ。_onFinished は画面側が握っているので、
            // ログ採取はそれとは別口で受け取る。
            OnQuizFinished?.Invoke(result);

            _onFinished?.Invoke(result);
        }

        /// <summary>知識テストの結果。研究用のログ採取だけが購読する。</summary>
        public static event Action<QuizResult> OnQuizFinished;

        /// <summary>把结果拼成给学员看的文字（含需复习的主题）。</summary>
        public string BuildResultText(QuizResult result)
        {
            int total = Mathf.Max(1, result.total);
            int pct = Mathf.RoundToInt(100f * result.correct / total);

            string line1 = LanguageManager.Get(
                $"答对 {result.correct} / {result.total} 题（{pct}%）",
                $"正解 {result.correct} / {result.total} 問（{pct}%）",
                $"{result.correct} / {result.total} correct ({pct}%)");

            if (result.wrongTopics == null || result.wrongTopics.Count == 0)
                return line1;

            var names = new List<string>(result.wrongTopics.Count);
            foreach (string topicId in result.wrongTopics)
            {
                QuizTopic t = _bank != null ? _bank.FindTopic(topicId) : null;
                names.Add(t != null && t.title != null ? t.title.Resolve() : topicId);
            }

            string line2 = LanguageManager.Get(
                "建议复习：" + string.Join("、", names),
                "復習推奨：" + string.Join("、", names),
                "Review: " + string.Join(", ", names));

            return line1 + "\n" + line2;
        }
    }
}
