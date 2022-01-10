using Assets.Scripts.Models;
using Assets.Scripts.VSS;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Common
{
    internal class GlobalStore : MonoBehaviour
    {
        public static GlobalStore Instance;

        public AsyncReactiveProperty<VSSModel> CurrentVSSModel { get; } = new AsyncReactiveProperty<VSSModel>(null);

        public VSSModelLoader VSSModelLoader { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Init();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Init()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            VSSModelLoader = new VSSModelLoader();
            VSSModelLoader.PreLoad();
        }
    }
}
