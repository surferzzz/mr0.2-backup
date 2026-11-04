using UnityEngine;

namespace CPRSimulation.Research
{
    /// <summary>視覚のみ / 視覚＋触覚 の 2 条件。</summary>
    public enum FeedbackCondition
    {
        /// <summary>A：画面の表示だけ（グローブの振動なし）。</summary>
        VisualOnly,
        /// <summary>B：画面の表示＋手の振動。</summary>
        VisualHaptic
    }

    /// <summary>
    /// 実験 1 回ぶんの識別情報（参加者ID・群・何回目）。
    /// 被験者間計画なので、1 人は A か B のどちらか一方しか体験しない。
    /// Condition は「今回どちらか」ではなく、その参加者が割り付けられた群。
    ///
    /// シーンを跨いでも消えないよう PlayerPrefs に置く。実験当日は
    /// 「ID や群を変え忘れて前の参加者の設定のまま記録される」のが一番怖いので、
    /// 値は HUD に出しっぱなしにして実験者が常に確認できるようにする。
    /// 割付表と突き合わせてから始めること。
    /// </summary>
    public static class ResearchSession
    {
        private const string KeyParticipant = "Research.ParticipantId";
        private const string KeyCondition = "Research.Condition";
        private const string KeyTrial = "Research.TrialIndex";
        private const string KeyHideScore = "Research.HideScore";

        /// <summary>
        /// 設定画面で確定したか。PlayerPrefs には入れない ――
        /// 「この起動で人が目で見て確定したか」だけを指すので、持ち越してはいけない。
        /// </summary>
        private static bool _configuredInThisLaunch;

        public static string ParticipantId
        {
            get { return PlayerPrefs.GetString(KeyParticipant, "P00"); }
            set
            {
                PlayerPrefs.SetString(KeyParticipant, string.IsNullOrEmpty(value) ? "P00" : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static FeedbackCondition Condition
        {
            get
            {
                return PlayerPrefs.GetInt(KeyCondition, (int)FeedbackCondition.VisualOnly) == 1
                    ? FeedbackCondition.VisualHaptic
                    : FeedbackCondition.VisualOnly;
            }
            set
            {
                PlayerPrefs.SetInt(KeyCondition, value == FeedbackCondition.VisualHaptic ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 何回目の実施か。被験者間計画なので通常は 1 のまま。
        /// 機材トラブルなどでやり直したときに 2 以上にして、
        /// 同じ参加者の行がどちらなのか後から分かるようにする。
        /// </summary>
        public static int TrialIndex
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(KeyTrial, 1), 1, 9); }
            set
            {
                PlayerPrefs.SetInt(KeyTrial, Mathf.Clamp(value, 1, 9));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 体験の最後に、被験者へ成績を見せないようにする。
        ///
        /// 見せると、そのすぐあとに答える自己効力感とリアリティの項目が点数に
        /// 引きずられる。測りたいのは体験そのものの効果であって、
        /// 「80 点と言われた直後の気分」ではない。
        ///
        /// 採点そのものは止めない。計算も CSV への記録もこれまでどおりで、
        /// 画面に出さないだけ。教材として使うときは false に戻す。
        /// </summary>
        public static bool HideScoreFromParticipant
        {
            get { return PlayerPrefs.GetInt(KeyHideScore, 1) != 0; }
            set
            {
                PlayerPrefs.SetInt(KeyHideScore, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>CSV に入れる条件コード。質問紙の P2 と同じ A / B。</summary>
        public static string ConditionCode
        {
            get { return Condition == FeedbackCondition.VisualHaptic ? "B" : "A"; }
        }

        /// <summary>HUD 表示用。例：P07 / B / 2回目</summary>
        public static string Describe()
        {
            return string.Format("{0} / {1} / {2}", ParticipantId, ConditionCode, TrialIndex);
        }

        /// <summary>
        /// 実機で ID を変える手段。ビルド後は Inspector が使えないので、
        /// 実験者が adb push した JSON を起動時に読む。
        ///   adb push session_config.json /sdcard/Android/data/&lt;package&gt;/files/research/
        /// 中身："{\"participantId\":\"P07\",\"condition\":\"B\",\"trialIndex\":2}"
        /// ファイルが無ければ何もしない（前回の設定がそのまま残る）。
        ///
        /// 設定画面で確定済みのときは読まない。ロガーは訓練シーンで生まれるので、
        /// 順番としては「画面で確定 → シーン遷移 → ここ」になる。古い JSON が
        /// 端末に残っていると、いま入力したばかりの値を黙って上書きしてしまう。
        /// </summary>
        public static bool TryLoadFromConfigFile(string path)
        {
            if (_configuredInThisLaunch)
            {
                Debug.Log("[Research] 設定画面で確定済みのため session_config.json は読まない");
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                    return false;

                var cfg = JsonUtility.FromJson<ConfigFile>(System.IO.File.ReadAllText(path));
                if (cfg == null)
                    return false;

                if (!string.IsNullOrEmpty(cfg.participantId))
                    ParticipantId = cfg.participantId;
                if (!string.IsNullOrEmpty(cfg.condition))
                    Condition = cfg.condition.Trim().ToUpperInvariant() == "B"
                        ? FeedbackCondition.VisualHaptic
                        : FeedbackCondition.VisualOnly;
                if (cfg.trialIndex > 0)
                    TrialIndex = cfg.trialIndex;
                // 文字列にしてあるのは「キーが無い」と「0 が書いてある」を
                // 区別するため。数値だと既定値の 0 と見分けがつかない。
                if (!string.IsNullOrEmpty(cfg.hideScore))
                {
                    string v = cfg.hideScore.Trim().ToLowerInvariant();
                    HideScoreFromParticipant = v != "0" && v != "false";
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Research] session_config.json の読み込みに失敗: " + e.Message);
                return false;
            }
        }

        [System.Serializable]
        private class ConfigFile
        {
            public string participantId;
            public string condition;
            public int trialIndex;
            public string hideScore;
        }

        /// <summary>
        /// 設定画面で確定したことを記録する。以降この起動では
        /// session_config.json に上書きさせない。
        /// </summary>
        public static void MarkConfiguredInThisLaunch()
        {
            _configuredInThisLaunch = true;
        }

        public static void ToggleCondition()
        {
            Condition = Condition == FeedbackCondition.VisualHaptic
                ? FeedbackCondition.VisualOnly
                : FeedbackCondition.VisualHaptic;
        }

        public static void NextTrial()
        {
            TrialIndex = TrialIndex >= 2 ? 1 : TrialIndex + 1;
        }
    }
}
