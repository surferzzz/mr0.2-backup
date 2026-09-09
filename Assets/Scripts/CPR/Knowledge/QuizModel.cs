using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CPRSimulation.Knowledge
{
    /// <summary>
    /// 三语文本。字段名就是 JSON 里的键，配合 JsonUtility 直接反序列化。
    /// 解析时复用 LanguageManager.Get，连「没有 manager 时回退日语」的行为都一致。
    /// </summary>
    [Serializable]
    public class LocalizedText
    {
        public string zh;
        public string ja;
        public string en;

        public string Resolve()
        {
            return LanguageManager.Get(zh ?? string.Empty, ja ?? string.Empty, en ?? string.Empty);
        }

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(zh) &&
            string.IsNullOrWhiteSpace(ja) &&
            string.IsNullOrWhiteSpace(en);

        /// <summary>哪些语言缺文案。用于加载时给出可操作的提示。</summary>
        public string MissingLanguages()
        {
            var missing = new List<string>(3);
            if (string.IsNullOrWhiteSpace(zh)) missing.Add("zh");
            if (string.IsNullOrWhiteSpace(ja)) missing.Add("ja");
            if (string.IsNullOrWhiteSpace(en)) missing.Add("en");
            return missing.Count == 0 ? null : string.Join("/", missing);
        }
    }

    [Serializable]
    public class QuizTopic
    {
        public string id;
        public LocalizedText title;
    }

    /// <summary>
    /// 一道题。single 和 truefalse 走同一条渲染路径 —— 判断题就是两个选项，
    /// 不值得为它单开一套 UI。
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        public string id;
        public string topic;
        public string type = "single";

        public LocalizedText stem;
        public LocalizedText[] options;

        /// <summary>正确选项在 options 里的下标，从 0 开始。</summary>
        public int answer = -1;

        public LocalizedText explanation;

        /// <summary>
        /// 答错后可以跳去实操练习的模式名（CPR / AED / Story），留空表示没有对应实操。
        /// 这是把书面知识和已有实操模块连起来的钩子。
        /// </summary>
        public string practiceMode;

        /// <summary>
        /// 医学内容是否已被教员逐条核实。false 的题会在加载时汇总提醒 ——
        /// 这是把「内容准确性责任」显式化，而不是让占位题悄悄混进正式训练。
        /// </summary>
        public bool reviewed;
    }

    [Serializable]
    public class QuizBank
    {
        public int version;
        public QuizTopic[] topics;
        public QuizQuestion[] questions;

        public QuizTopic FindTopic(string topicId)
        {
            if (topics == null || string.IsNullOrEmpty(topicId))
                return null;

            foreach (QuizTopic t in topics)
            {
                if (t != null && t.id == topicId)
                    return t;
            }

            return null;
        }
    }

    /// <summary>
    /// 从 Resources 加载题库并校验。
    ///
    /// 为什么校验这么严：一道题的 answer 下标写错，界面上不会报错，
    /// 只会把错的选项判成对的 —— 对医学训练来说这是实质性的危害，
    /// 必须在加载阶段就拦下来，而不是等学员答完才发现。
    /// </summary>
    public static class QuizBankLoader
    {
        public const string ResourcePath = "VRTrainingUI/Quiz/questions";
        public const int SupportedVersion = 1;

        private static QuizBank _cached;
        private static bool _loaded;

        public static QuizBank Load(bool forceReload = false)
        {
            if (_loaded && !forceReload)
                return _cached;

            _loaded = true;
            _cached = null;

            TextAsset json = Resources.Load<TextAsset>(ResourcePath);
            if (json == null)
            {
                Debug.LogError(
                    $"[QuizBank] 找不到题库 Resources/{ResourcePath}.json。" +
                    "知识测验会显示为空。");
                return null;
            }

            QuizBank bank;
            try
            {
                bank = JsonUtility.FromJson<QuizBank>(json.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuizBank] 题库 JSON 解析失败：{e.Message}");
                return null;
            }

            if (bank == null)
            {
                Debug.LogError("[QuizBank] 题库解析结果为空，检查 JSON 根节点是否是对象。");
                return null;
            }

            if (!Validate(bank, out string report))
            {
                Debug.LogError("[QuizBank] 题库校验未通过，已拒绝加载：\n" + report);
                return null;
            }

            if (!string.IsNullOrEmpty(report))
                Debug.LogWarning("[QuizBank] 题库可用，但有以下提醒：\n" + report);

            _cached = bank;
            Debug.Log($"[QuizBank] 载入 {bank.questions.Length} 道题 / " +
                      $"{(bank.topics != null ? bank.topics.Length : 0)} 个主题（version {bank.version}）");
            return bank;
        }

        /// <summary>
        /// 返回 false 表示有致命问题、不能用。
        /// report 同时收集致命错误和非致命提醒，一次把所有问题列全，
        /// 免得改一个、重跑一次、再发现下一个。
        /// </summary>
        private static bool Validate(QuizBank bank, out string report)
        {
            var fatal = new StringBuilder();
            var warn = new StringBuilder();

            if (bank.version != SupportedVersion)
            {
                warn.AppendLine($"  · version={bank.version}，当前代码按 version {SupportedVersion} 解析");
            }

            if (bank.questions == null || bank.questions.Length == 0)
            {
                fatal.AppendLine("  · questions 为空，题库里没有任何题目");
                report = fatal.ToString();
                return false;
            }

            var seenIds = new HashSet<string>();
            int unreviewed = 0;

            for (int i = 0; i < bank.questions.Length; i++)
            {
                QuizQuestion q = bank.questions[i];
                string where = $"questions[{i}]" + (string.IsNullOrEmpty(q?.id) ? "" : $" (id={q.id})");

                if (q == null)
                {
                    fatal.AppendLine($"  · {where} 是 null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(q.id))
                    fatal.AppendLine($"  · {where} 缺 id");
                else if (!seenIds.Add(q.id))
                    fatal.AppendLine($"  · {where} id 重复");

                if (q.stem == null || q.stem.IsEmpty)
                    fatal.AppendLine($"  · {where} 缺题干 stem");

                if (q.options == null || q.options.Length < 2)
                {
                    fatal.AppendLine($"  · {where} 选项少于 2 个");
                }
                else
                {
                    for (int o = 0; o < q.options.Length; o++)
                    {
                        if (q.options[o] == null || q.options[o].IsEmpty)
                            fatal.AppendLine($"  · {where} options[{o}] 是空文案");
                    }

                    // 这条最要紧：下标越界会让「正确答案」永远选不中
                    if (q.answer < 0 || q.answer >= q.options.Length)
                    {
                        fatal.AppendLine(
                            $"  · {where} answer={q.answer} 越界（选项共 {q.options.Length} 个，" +
                            $"合法范围 0..{q.options.Length - 1}）");
                    }
                }

                if (!string.IsNullOrEmpty(q.topic) && bank.FindTopic(q.topic) == null)
                    warn.AppendLine($"  · {where} 的 topic「{q.topic}」不在 topics 列表里");

                if (q.explanation == null || q.explanation.IsEmpty)
                    warn.AppendLine($"  · {where} 没有解析文案，答错时学员看不到原因");

                if (!q.reviewed)
                    unreviewed++;

                string miss = q.stem?.MissingLanguages();
                if (!string.IsNullOrEmpty(miss))
                    warn.AppendLine($"  · {where} 题干缺 {miss} 语言，会回退到日语");
            }

            if (unreviewed > 0)
            {
                warn.AppendLine(
                    $"  · 有 {unreviewed} 道题 reviewed=false（医学内容未经核实）。" +
                    "正式训练前请由教员逐条核对后改为 true。");
            }

            if (fatal.Length > 0)
            {
                report = fatal.ToString() + (warn.Length > 0 ? warn.ToString() : "");
                return false;
            }

            report = warn.ToString();
            return true;
        }
    }
}
