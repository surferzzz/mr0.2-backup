using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using CPRSimulation.Game;
using CPRSimulation.Knowledge;
using VRFirstAid.Equipment;

namespace CPRSimulation.Research
{
    /// <summary>
    /// 研究用のデータ採取。訓練の流れにはいっさい関与しない。
    ///
    /// CPRTrainingController は指標をぜんぶ計算していたのに、結果テキストに組み立てて
    /// 画面へ出すだけで保存していなかった（シーンを抜ければ消える）。ここはその出口。
    ///
    /// 出力は 2 つ：
    ///   sessions.csv      … 体験 1 回につき 1 行（質問紙と 1:1 で対応する）
    ///   compressions.csv  … 圧迫 1 回につき 1 行（学習曲線とばらつきの分析用）
    ///
    /// 平均値だけでは「触覚ありのほうがばらつきが小さい」「早く安定する」が出せない。
    /// 本研究ではそれが主役なので、1 回ごとの明細のほうが本体。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingDataLogger : MonoBehaviour
    {
        [Header("出力")]
        [Tooltip("persistentDataPath 配下のフォルダ名。Quest では /sdcard/Android/data/<package>/files/ の下。")]
        [SerializeField] private string folderName = "research";
        [SerializeField] private string sessionsFileName = "sessions.csv";
        [SerializeField] private string compressionsFileName = "compressions.csv";

        [Header("動作")]
        [Tooltip("1 回ごとの明細を書き出す。off にすると sessions.csv だけになる。")]
        [SerializeField] private bool logPerCompression = true;
        [Tooltip("書き出した行を Console にも出す。実機で adb logcat から確認したいとき用。")]
        [SerializeField] private bool echoToConsole = true;

        private readonly List<CompressionRow> _rows = new List<CompressionRow>();
        private CPRPressDetector _boundDetector;
        private float _cprStartTime;
        private bool _sessionActive;

        private struct CompressionRow
        {
            public int Seq;
            public float TimeSinceStart;
            public float DepthCm;
            public float RateBpm;
            public float RecoilDeficitCm;
            public bool RecoilIncomplete;
            public float OffsetCm;
            public bool OffTarget;
            public bool DepthOk;
            public bool RateOk;
        }

        private string Folder
        {
            get { return Path.Combine(Application.persistentDataPath, folderName); }
        }

        public string SessionsPath { get { return Path.Combine(Folder, sessionsFileName); } }
        public string CompressionsPath { get { return Path.Combine(Folder, compressionsFileName); } }

        private const string RuntimeRootName = "ResearchDataLogger";

        /// <summary>
        /// 自己起動。シーンに置いて回る運用にしない理由：
        /// 置き忘れた回はまるごと記録が残らず、しかも実験当日その場では気づけない
        /// （画面には結果が出るので、正常に見える）。実験のたびに人間の手順へ
        /// 依存させる部分を減らす。
        ///
        /// RuntimeInitializeOnLoadMethod は「最初のシーンが読み込まれた後」に
        /// 一度しか走らず、起動シーンは Scenes/MainMenu.unity で訓練コントローラが
        /// いない。だから AEDTrainingRuntime と同じく sceneLoaded でも拾い直す。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 先に外してから付ける：静的イベントの購読はシーンをまたいで生き残り、
            // エディタで Domain Reload を切っていると Play セッションもまたぐ。
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
            if (FindObjectOfType<TrainingDataLogger>() != null)
                return;

            // 訓練コントローラのいないシーン（主菜单）では何も採れないので黙って見送る。
            // 訓練シーンへ入れば sceneLoaded がもう一度来る。
            if (FindObjectOfType<CPRTrainingController>() == null)
                return;

            var go = new GameObject(RuntimeRootName);
            go.AddComponent<TrainingDataLogger>();
        }

        private void Awake()
        {
            // 実機では Inspector を触れないので、adb push した JSON があれば優先する。
            ResearchSession.TryLoadFromConfigFile(Path.Combine(Folder, "session_config.json"));
            if (echoToConsole)
                Debug.Log("[Research] " + ResearchSession.Describe() + " → " + SessionsPath);
        }

        private void OnEnable()
        {
            CPRTrainingController.OnSessionStarted += HandleSessionStarted;
            CPRTrainingController.OnSessionCompleted += HandleSessionCompleted;
            KnowledgeQuizView.OnQuizFinished += HandleQuizFinished;
        }

        private void OnDisable()
        {
            CPRTrainingController.OnSessionStarted -= HandleSessionStarted;
            CPRTrainingController.OnSessionCompleted -= HandleSessionCompleted;
            KnowledgeQuizView.OnQuizFinished -= HandleQuizFinished;
            UnbindDetector();
        }

        // ── セッションの開始・終了 ──

        private void HandleSessionStarted(CPRTrainingController controller)
        {
            _rows.Clear();
            _cprStartTime = Time.time;
            _sessionActive = true;

            UnbindDetector();
            if (controller != null && controller.PressDetector != null)
            {
                _boundDetector = controller.PressDetector;
                _boundDetector.OnCompressionComplete += HandleCompression;
            }

            if (echoToConsole)
                Debug.Log("[Research] 記録開始 " + ResearchSession.Describe());
        }

        /// <summary>
        /// 回弹と位置は検出器の Last* から読む。OnCompressionComplete は
        /// EndCompression の中で Last* を更新した直後に飛んでくるので、
        /// ここで読める値はこの 1 回ぶんのもの（controller 側と同じやり方）。
        /// </summary>
        private void HandleCompression(float depthCm, float rateBpm)
        {
            if (!_sessionActive || _boundDetector == null)
                return;

            float minDepth = _boundDetector.minGoodDepthCm;
            float maxDepth = _boundDetector.maxGoodDepthCm;
            float minRate = _boundDetector.minGoodRateBPM;
            float maxRate = _boundDetector.maxGoodRateBPM;

            var row = new CompressionRow
            {
                Seq = _rows.Count + 1,
                TimeSinceStart = Time.time - _cprStartTime,
                DepthCm = depthCm,
                RateBpm = rateBpm,
                RecoilDeficitCm = _boundDetector.LastRecoilDeficitCm,
                RecoilIncomplete = _boundDetector.LastRecoilIncomplete,
                OffsetCm = _boundDetector.LastOffsetCm,
                OffTarget = _boundDetector.LastOffTarget,
                DepthOk = depthCm >= minDepth && depthCm <= maxDepth,
                RateOk = rateBpm > 0.1f && rateBpm >= minRate && rateBpm <= maxRate
            };

            _rows.Add(row);
        }

        private void HandleSessionCompleted(CPRTrainingController controller)
        {
            if (controller == null)
                return;

            // 中断・再開始・アプリ終了がそれぞれ同じイベントを投げてくる。
            // 計測中でなければ書くものが無い＝二重記録の番人でもある。
            if (!_sessionActive)
                return;

            _sessionActive = false;
            UnbindDetector();

            try
            {
                WriteSessionRow(controller);
                if (logPerCompression && _rows.Count > 0)
                    WriteCompressionRows();
            }
            catch (Exception e)
            {
                // ログが落ちても訓練は続けられるべきなので、例外は外に出さない。
                Debug.LogWarning("[Research] 書き出しに失敗: " + e.Message);
            }
        }

        private void UnbindDetector()
        {
            if (_boundDetector != null)
                _boundDetector.OnCompressionComplete -= HandleCompression;
            _boundDetector = null;
        }

        // ── 1 行ぶんの組み立て ──

        private void WriteSessionRow(CPRTrainingController c)
        {
            int n = c.CompressionSamples;
            int rateN = c.RateSamples;

            float depthMean = n > 0 ? c.SumDepthCm / n : 0f;
            float rateMean = rateN > 0 ? c.SumRateBpm / rateN : 0f;
            float depthSd = StdDev(true);
            float rateSd = StdDev(false);

            var d = c.PressDetector;
            float depthMin = d != null ? d.minGoodDepthCm : 5f;
            float depthMax = d != null ? d.maxGoodDepthCm : 6f;
            float rateMin = d != null ? d.minGoodRateBPM : 100f;
            float rateMax = d != null ? d.maxGoodRateBPM : 120f;

            // HasResult は static なので、前の AED 回の値が残っていることがある。
            // この回が AED を含む流れだったときだけ採用する。
            bool aed = c.AedFlowIncludedInResult && AEDTrainingRuntime.HasResult;

            var f = new List<string>
            {
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                ResearchSession.ParticipantId,
                ResearchSession.ConditionCode,
                // 条件が本当に成立していたか。B 群なのに haptic_pulses が 0 なら、
                // 手袋が動いていない＝その回は「触覚あり」として扱えない。
                Bool(GloveHapticController.HapticsAllowed),
                Num(c.hapticController != null ? c.hapticController.PulsesThisSession : 0),
                Num(ResearchSession.TrialIndex),
                c.SelectedTrainingMode.ToString(),
                Application.version,
                Bool(c.SessionCompletedNormally),

                Num(depthMin), Num(depthMax), Num(rateMin), Num(rateMax),
                Num(c.InterruptionThresholdSeconds),

                Num(c.CompressionCountTotal),
                Num(c.TargetCompressions),
                Num(n),
                Num(depthMean), Num(depthSd),
                Pct(c.GoodDepthSamples, n),
                Num(c.TooShallowSamples), Num(c.TooDeepSamples),

                Num(rateN),
                Num(rateMean), Num(rateSd),
                Pct(c.GoodRateSamples, rateN),
                Num(c.TooSlowSamples), Num(c.TooFastSamples),
                Pct(c.GoodCombinedSamples, rateN),

                Pct(c.FullRecoilSamples, n),
                Num(n > 0 ? c.SumRecoilDeficitCm / n : 0f),
                Pct(c.OnTargetSamples, n),
                Num(n > 0 ? c.SumOffsetCm / n : 0f),

                Num(c.ChestCompressionFraction),
                Num(c.TotalHandsOffSeconds),
                Num(c.LongestHandsOffSeconds),
                Num(c.InterruptionCount),
                Num(c.BestQualityStreak),

                Num(c.ResponseCheckSeconds),
                Bool(c.ResponseCheckVoiceHeard),
                Bool(c.ResponseCheckSkipped),

                Num(c.VentilationCyclesDone),
                Num(c.TotalBreathsGiven),
                Num(c.LongestVentilationPauseSec),

                Bool(aed && AEDTrainingRuntime.ResultPadsPlaced),
                Bool(aed && AEDTrainingRuntime.ResultShockAdvised),
                Bool(aed && AEDTrainingRuntime.ResultShockDelivered),
                aed ? AEDTrainingRuntime.ResultRhythm.ToString() : "",
                aed ? Num(AEDTrainingRuntime.ResultDurationSeconds) : "",
                aed ? Num(AEDTrainingRuntime.ResultTimeToPadsSeconds) : "",
                aed ? Num(AEDTrainingRuntime.ResultTimeToShockSeconds) : "",

                "", "", "",   // quiz_correct / quiz_total / quiz_wrong_topics（知識テストは自分の行を持つ）

                // 中断で終わった回は結果テキストを組み立てていないので総合点が無い。
                // 0 を入れると「0 点だった」と読めてしまうので空欄にする。
                c.HasOverallScore ? Num(c.LastOverallScore) : "",
                Num(c.SessionDurationSeconds)
            };

            // 列を足したときに片方だけ直し忘れると、以降ぜんぶの行が 1 列ずれる。
            // 解析に入るまで誰も気づかないので、その場で鳴らす。
            if (f.Count != SessionColumnCount)
            {
                Debug.LogError(string.Format(
                    "[Research] 列数が合わない: 値 {0} 列 / ヘッダ {1} 列。sessions.csv がずれる。",
                    f.Count, SessionColumnCount));
            }

            AppendLine(SessionsPath, SessionHeader, string.Join(",", f.ToArray()));

            if (echoToConsole)
            {
                Debug.Log(string.Format(
                    "[Research] {0} 深さ適切 {1}% / テンポ適切 {2}% / 圧迫 {3} 回 → {4}",
                    ResearchSession.Describe(),
                    Pct(c.GoodDepthSamples, n), Pct(c.GoodRateSamples, rateN),
                    n, SessionsPath));
            }
        }

        private void HandleQuizFinished(QuizResult result)
        {
            try
            {
                var f = new List<string>(new string[SessionColumnCount]);
                for (int i = 0; i < f.Count; i++) f[i] = "";

                f[ColumnIndex("timestamp")] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                f[ColumnIndex("participant_id")] = ResearchSession.ParticipantId;
                f[ColumnIndex("condition")] = ResearchSession.ConditionCode;
                f[ColumnIndex("trial_index")] = Num(ResearchSession.TrialIndex);
                f[ColumnIndex("mode")] = "Quiz";
                f[ColumnIndex("app_version")] = Application.version;
                // 最後まで解いた時にしか飛んでこないイベントなので常に完了。
                f[ColumnIndex("completed")] = Bool(true);

                f[ColumnIndex("quiz_correct")] = Num(result.correct);
                f[ColumnIndex("quiz_total")] = Num(result.total);
                f[ColumnIndex("quiz_wrong_topics")] = Csv(result.wrongTopics != null
                    ? string.Join("|", result.wrongTopics.ToArray())
                    : "");

                AppendLine(SessionsPath, SessionHeader, string.Join(",", f.ToArray()));

                if (echoToConsole)
                    Debug.Log(string.Format("[Research] {0} 知識テスト {1}/{2}",
                        ResearchSession.Describe(), result.correct, result.total));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Research] 知識テストの書き出しに失敗: " + e.Message);
            }
        }

        private void WriteCompressionRows()
        {
            var sb = new StringBuilder();
            string pid = ResearchSession.ParticipantId;
            string cond = ResearchSession.ConditionCode;
            string trial = Num(ResearchSession.TrialIndex);

            for (int i = 0; i < _rows.Count; i++)
            {
                CompressionRow r = _rows[i];
                sb.Append(pid).Append(',')
                  .Append(cond).Append(',')
                  .Append(trial).Append(',')
                  .Append(Num(r.Seq)).Append(',')
                  .Append(Num(r.TimeSinceStart)).Append(',')
                  .Append(Num(r.DepthCm)).Append(',')
                  .Append(Num(r.RateBpm)).Append(',')
                  .Append(Num(r.RecoilDeficitCm)).Append(',')
                  .Append(Bool(r.RecoilIncomplete)).Append(',')
                  .Append(Num(r.OffsetCm)).Append(',')
                  .Append(Bool(r.OffTarget)).Append(',')
                  .Append(Bool(r.DepthOk)).Append(',')
                  .Append(Bool(r.RateOk))
                  .Append('\n');
            }

            AppendLine(CompressionsPath, CompressionHeader, sb.ToString().TrimEnd('\n'));
        }

        // ── ばらつき ──

        /// <summary>標本標準偏差。触覚の効果はここに出ることが多いので主要な派生値。</summary>
        private float StdDev(bool depth)
        {
            int n = 0;
            double sum = 0d;
            for (int i = 0; i < _rows.Count; i++)
            {
                float v = depth ? _rows[i].DepthCm : _rows[i].RateBpm;
                if (!depth && v <= 0.1f) continue;
                sum += v;
                n++;
            }
            if (n < 2) return 0f;

            double mean = sum / n;
            double sq = 0d;
            for (int i = 0; i < _rows.Count; i++)
            {
                float v = depth ? _rows[i].DepthCm : _rows[i].RateBpm;
                if (!depth && v <= 0.1f) continue;
                sq += (v - mean) * (v - mean);
            }
            return (float)Math.Sqrt(sq / (n - 1));
        }

        // ── CSV ──

        private const string SessionHeader =
            "timestamp,participant_id,condition,haptic_enabled,haptic_pulses," +
            "trial_index,mode,app_version,completed," +
            "guideline_depth_min,guideline_depth_max,guideline_rate_min,guideline_rate_max," +
            "interruption_threshold_s," +
            "compression_count,compression_target,compression_samples," +
            "depth_mean_cm,depth_sd_cm,depth_in_range_pct,too_shallow_n,too_deep_n," +
            "rate_samples,rate_mean_bpm,rate_sd_bpm,rate_in_range_pct,too_slow_n,too_fast_n," +
            "both_in_range_pct," +
            "full_recoil_pct,recoil_deficit_mean_cm,on_target_pct,offset_mean_cm," +
            "ccf_pct,hands_off_total_s,longest_interruption_s,interruption_count,best_streak," +
            "response_check_s,response_voice_heard,response_skipped," +
            "ventilation_cycles,breaths_total,longest_vent_pause_s," +
            "aed_pads_placed,aed_shock_advised,aed_shock_delivered,aed_rhythm," +
            "aed_total_s,aed_time_to_pads_s,aed_time_to_shock_s," +
            "quiz_correct,quiz_total,quiz_wrong_topics," +
            "overall_score,session_duration_s";

        private const string CompressionHeader =
            "participant_id,condition,trial_index,seq,t_since_cpr_start_s," +
            "depth_cm,rate_bpm,recoil_deficit_cm,recoil_incomplete," +
            "offset_cm,off_target,depth_ok,rate_ok";

        private static readonly string[] SessionColumns = SessionHeader.Split(',');

        private static int SessionColumnCount
        {
            get { return SessionColumns.Length; }
        }

        /// <summary>
        /// 列名から位置を引く。「末尾から 4 番目」のような相対位置で書いていると、
        /// 列を 1 本足した瞬間に知識テストの行だけ静かにズレて、
        /// 気づくのは解析のときになる。
        /// </summary>
        private static int ColumnIndex(string name)
        {
            for (int i = 0; i < SessionColumns.Length; i++)
            {
                if (SessionColumns[i] == name)
                    return i;
            }

            throw new ArgumentException("[Research] 列が見つからない: " + name);
        }

        /// <summary>
        /// 追記。Excel が日本語を化けさせないよう、新規作成時だけ UTF-8 BOM を付ける。
        /// 1 セッションごとに開いて閉じるので、途中で落ちても直前までは残る。
        /// </summary>
        private static void AppendLine(string path, string header, string line)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool isNew = !File.Exists(path) || new FileInfo(path).Length == 0;

            if (!isNew && !HeaderMatches(path, header))
            {
                // 列構成が変わった後のファイルに継ぎ足すと、古い行と新しい行が
                // 同じヘッダの下で 1 列ずれて並ぶ。見た目は CSV のままなので、
                // 解析を始めるまで誰も気づけない。古いほうを退避して作り直す。
                isNew = ArchiveMismatchedFile(path);
            }

            using (var w = new StreamWriter(path, true, new UTF8Encoding(isNew)))
            {
                if (isNew)
                    w.Write(header + "\n");
                w.Write(line + "\n");
            }
        }

        private static bool HeaderMatches(string path, string header)
        {
            try
            {
                using (var r = new StreamReader(path, Encoding.UTF8, true))
                {
                    string first = r.ReadLine();
                    if (first == null)
                        return false;

                    // UTF-8 BOM は StreamReader が食べてくれるが、別の経路で
                    // 書かれたファイルのために一応落としておく。
                    return first.TrimStart('﻿').Trim() == header;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Research] 既存ヘッダの確認に失敗: " + e.Message);
                return true;   // 判断できないときは触らない
            }
        }

        /// <summary>退避できたら true（＝ヘッダから書き直してよい）。</summary>
        private static bool ArchiveMismatchedFile(string path)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string archived = path + ".old-" + stamp;

            try
            {
                File.Move(path, archived);
                Debug.LogWarning("[Research] 列構成が変わったので退避した: " + archived);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Research] 古い CSV を退避できない（列がずれたまま追記される）: " + e.Message);
                return false;
            }
        }

        private static string Num(int v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private static string Num(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return "";
            return v.ToString("F2", CultureInfo.InvariantCulture);
        }

        private static string Pct(int hit, int total)
        {
            if (total <= 0) return "";
            return (100f * hit / total).ToString("F1", CultureInfo.InvariantCulture);
        }

        private static string Bool(bool v)
        {
            return v ? "1" : "0";
        }

        /// <summary>カンマや引用符を含む値をくるむ。自由記述が混ざっても列がずれない。</summary>
        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.IndexOf(',') < 0 && v.IndexOf('"') < 0 && v.IndexOf('\n') < 0)
                return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        [ContextMenu("出力先を Console に表示")]
        private void PrintPaths()
        {
            Debug.Log("[Research] sessions: " + SessionsPath + "\n[Research] compressions: " + CompressionsPath);
        }
    }
}
