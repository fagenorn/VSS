using UnityEngine;
using Mediapipe;
using Mediapipe.Unity;

using Logger = Mediapipe.Logger;
using System.IO;

namespace Assets.Scripts.Mediapipe
{
    public class Bootstrap : MonoBehaviour
    {
        private const string _TAG = nameof(Bootstrap);

        [SerializeField] private bool _enableGlog = true;

        public bool isFinished { get; private set; }
        private bool _isGlogInitialized;

        private void Start()
        {
            Logger.SetLogger(new Common.MemoizedLogger(100));
            Logger.minLogLevel = Logger.LogLevel.Debug;

            Logger.LogInfo(_TAG, "Setting global flags...");
            Common.GlobalConfigManager.SetFlags();

            if (_enableGlog)
            {
                if (Glog.LogDir != null)
                {
                    if (!Directory.Exists(Glog.LogDir))
                    {
                        Directory.CreateDirectory(Glog.LogDir);
                    }
                    Logger.LogVerbose(_TAG, $"Glog will output files under {Glog.LogDir}");
                }
                Glog.Initialize("MediaPipeUnityPlugin");
                _isGlogInitialized = true;
            }

            Logger.LogInfo(_TAG, "Initializing AssetLoader...");
            Common.AssetLoader.Provide(new StreamingAssetsResourceManager("Mediapipe"));

            DontDestroyOnLoad(gameObject);
            isFinished = true;
        }

        private void OnApplicationQuit()
        {
            if (_isGlogInitialized)
            {
                Glog.Shutdown();
            }

            Logger.SetLogger(null);
        }
    }
}
