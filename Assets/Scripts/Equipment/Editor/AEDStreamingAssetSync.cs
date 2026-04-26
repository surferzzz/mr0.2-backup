using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace VRFirstAid.Equipment.Editor
{
    /// <summary>
    /// Keeps the AED runtime asset mirrored into StreamingAssets so the same
    /// model can be loaded in the Editor and in Android builds.
    /// </summary>
    [InitializeOnLoad]
    public static class AEDStreamingAssetSync
    {
        private const string SourceRoot = "Assets/aed-defibrilator";
        private const string SourceZip = "Assets/0.3/aed-defibrilator.zip";
        private const string TargetRoot = "Assets/StreamingAssets/aed-defibrilator";
        private const string RuntimeModelRelativePath = "source/AED_Defibrilator.glb";

        private static bool _syncedThisSession;

        static AEDStreamingAssetSync()
        {
            EditorApplication.delayCall += EnsureSyncedOnLoad;
        }

        private static void EnsureSyncedOnLoad()
        {
            if (_syncedThisSession)
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return;
            }

            string targetModel = Path.Combine(
                projectRoot,
                TargetRoot.Replace('/', Path.DirectorySeparatorChar),
                RuntimeModelRelativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (File.Exists(targetModel))
            {
                _syncedThisSession = true;
                return;
            }

            EnsureSynced();
            _syncedThisSession = true;
        }

        [MenuItem("Tools/CPR/Sync AED Runtime Asset")]
        public static void EnsureSynced()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return;
            }

            string sourceRoot = Path.Combine(projectRoot, SourceRoot.Replace('/', Path.DirectorySeparatorChar));
            string targetRoot = Path.Combine(projectRoot, TargetRoot.Replace('/', Path.DirectorySeparatorChar));
            bool extractedSource = EnsureExtractedSource(projectRoot, sourceRoot);

            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning("[AEDStreamingAssetSync] Source AED folder was not found. Skipping sync.");
                return;
            }

            bool copiedAnyFile = false;
            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.EndsWith(".meta"))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                string targetFile = Path.Combine(targetRoot, relativePath);
                copiedAnyFile |= CopyIfChanged(sourceFile, targetFile);
            }

            if (extractedSource || copiedAnyFile)
            {
                AssetDatabase.Refresh();
                Debug.Log("[AEDStreamingAssetSync] AED source and runtime assets are synchronized.");
            }
        }

        private static bool EnsureExtractedSource(string projectRoot, string sourceRoot)
        {
            if (Directory.Exists(sourceRoot))
            {
                return false;
            }

            string sourceZip = Path.Combine(projectRoot, SourceZip.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourceZip))
            {
                return false;
            }

            Directory.CreateDirectory(sourceRoot);
            ZipFile.ExtractToDirectory(sourceZip, sourceRoot, overwriteFiles: true);
            Debug.Log("[AEDStreamingAssetSync] Extracted AED source from Assets/0.3.");
            return true;
        }

        private static bool CopyIfChanged(string sourceFile, string targetFile)
        {
            if (!File.Exists(sourceFile))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? string.Empty);

            if (File.Exists(targetFile))
            {
                var sourceInfo = new FileInfo(sourceFile);
                var targetInfo = new FileInfo(targetFile);
                if (sourceInfo.Length == targetInfo.Length &&
                    sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc)
                {
                    return false;
                }
            }

            File.Copy(sourceFile, targetFile, overwrite: true);
            File.SetLastWriteTimeUtc(targetFile, File.GetLastWriteTimeUtc(sourceFile));
            return true;
        }
    }
}
