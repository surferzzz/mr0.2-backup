using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace CPRSimulation.Editor
{
    public class TranslateUIToJapanese
    {
        [MenuItem("Tools/CPR/UI を日本語に翻訳", false, 5)]
        public static void Translate()
        {
            int count = 0;
            var allTexts = Object.FindObjectsOfType<Text>(true);

            foreach (var t in allTexts)
            {
                string original = t.text;

                t.text = original
                    .Replace("CPR 心肺复苏训练", "CPR 心肺蘇生トレーニング")
                    .Replace("CPR心肺复苏训练", "CPR心肺蘇生トレーニング")
                    .Replace("CPR 训练系统", "CPR トレーニングシステム")
                    .Replace("CPR训练系统", "CPRトレーニングシステム")
                    .Replace("心肺复苏虚拟现实训练", "心肺蘇生バーチャルリアリティ訓練")
                    .Replace("心肺复苏训练", "心肺蘇生トレーニング")
                    .Replace("开 始 训 练", "トレーニング開始")
                    .Replace("开始训练", "トレーニング開始")
                    .Replace("开始情景", "シナリオ開始")
                    .Replace("开始CPR", "CPR開始")
                    .Replace("退 出", "終 了")
                    .Replace("退出", "終了")
                    .Replace("重新开始", "やり直す")
                    .Replace("准备开始", "準備開始")
                    .Replace("CPR进行中", "CPR実施中")
                    .Replace("CPR 进行中", "CPR実施中")
                    .Replace("训练完成", "トレーニング完了")
                    .Replace("Quest 3 + 手部追踪", "Quest 3 + ハンドトラッキング")
                    .Replace("开始按压探测", "圧迫検出開始")
                    .Replace("开始按压检测", "圧迫検出開始")
                    .Replace("按压次数", "圧迫回数")
                    .Replace("按压深度", "圧迫深度")
                    .Replace("按压频率", "圧迫テンポ")
                    .Replace("按压速度", "圧迫テンポ")
                    .Replace("目标", "目標")
                    .Replace("深度", "深さ")
                    .Replace("频率", "テンポ")
                    .Replace("次/分钟", "回/分")
                    .Replace("次/分", "回/分")
                    .Replace("按压", "圧迫")
                    .Replace("开始", "開始")
                    .Replace("准备", "準備");

                if (original != t.text)
                {
                    Undo.RecordObject(t, "Translate to Japanese");
                    EditorUtility.SetDirty(t);
                    count++;
                    Debug.Log($"[翻訳] '{original}' → '{t.text}'");
                }
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("翻訳完了",
                    $"{count} 個のテキストを日本語に変換しました。\n\nCtrl+S でシーンを保存してください。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("翻訳",
                    "変更が必要なテキストは見つかりませんでした。\n（既に日本語化済みか、該当テキストが存在しません）",
                    "OK");
            }
        }
    }
}
