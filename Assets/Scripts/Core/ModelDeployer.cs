using System.IO;
using UnityEngine;
using ProjectLM.UI;

namespace ProjectLM.Core
{
    /// <summary>
    /// Handles model deployment at first launch.
    /// On Android, copies the GGUF from StreamingAssets (embedded in APK)
    /// to persistentDataPath (writable, private to the app).
    /// On PC, uses StreamingAssets directly.
    /// </summary>
    public class ModelDeployer : MonoBehaviour
    {
        [SerializeField] private string modelFileName = "gemma-4-E2B-it-qat-mobile-Q4_0.gguf";
        [SerializeField] private UIManager uiManager;

        /// <summary>
        /// Returns the writable path to the model file.
        /// On first launch on Android, copies from StreamingAssets.
        /// </summary>
        public string GetModelPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string persistentPath = Path.Combine(Application.persistentDataPath, "models", modelFileName);
            
            if (!File.Exists(persistentPath))
            {
                Debug.Log("[ModelDeployer] Model not found in persistent storage. Copying from APK...");
                CopyModelFromAPK(persistentPath);
            }
            else
            {
                Debug.Log($"[ModelDeployer] Model found at: {persistentPath}");
            }
            
            return persistentPath;
#else
            // PC/Editor: use StreamingAssets directly
            return Path.Combine(Application.streamingAssetsPath, "models", modelFileName);
#endif
        }

        /// <summary>
        /// Returns the source path where the model should be placed
        /// in the Unity project (for build inclusion).
        /// </summary>
        public string GetStreamingAssetsModelPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "models", modelFileName);
        }

        /// <summary>
        /// Returns true if the model file exists in StreamingAssets
        /// (i.e., it was included in the build).
        /// </summary>
        public bool IsModelEmbedded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, we can't check StreamingAssets directly with File.Exists
            // UnityWebRequest would be needed, but we just try to copy
            return true; // Assume it's there, the copy will fail gracefully if not
#else
            return File.Exists(GetStreamingAssetsModelPath());
#endif
        }

        private void CopyModelFromAPK(string destPath)
        {
            try
            {
                string sourcePath = Path.Combine(Application.streamingAssetsPath, "models", modelFileName);
                string destDir = Path.GetDirectoryName(destPath);
                
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                // On Android, StreamingAssets are inside the APK
                // and must be read via UnityWebRequest
                var request = UnityEngine.Networking.UnityWebRequest.Get(sourcePath);
                var operation = request.SendWebRequest();

                // Wait for completion (this runs at startup, blocking is acceptable)
                while (!operation.isDone) { }

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(destPath, request.downloadHandler.data);
                    Debug.Log($"[ModelDeployer] Model copied to: {destPath} ({request.downloadHandler.data.Length} bytes)");
                }
                else
                {
                    Debug.LogError($"[ModelDeployer] Failed to read model from APK: {request.error}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModelDeployer] Error copying model: {e.Message}");
            }
        }

        /// <summary>
        /// Check if model exists and is valid (non-zero size).
        /// </summary>
        public bool ValidateModel()
        {
            string path = GetModelPath();
            bool exists = File.Exists(path);
            if (exists)
            {
                var info = new FileInfo(path);
                bool valid = info.Length > 1024 * 1024; // At least 1 MB
                Debug.Log($"[ModelDeployer] Model validation: exists={exists}, size={info.Length / 1024 / 1024} MB, valid={valid}");
                return valid;
            }
            Debug.LogWarning("[ModelDeployer] Model not found");
            return false;
        }
    }
}
