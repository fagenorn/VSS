using System.Collections;
using Mediapipe;

namespace Assets.Scripts.Common
{
    public static class AssetLoader
    {
        private static ResourceManager _ResourceManager;

        public static void Provide(ResourceManager manager)
        {
            _ResourceManager = manager;
        }

        public static IEnumerator PrepareAssetAsync(string name, string uniqueKey, bool overwrite = false)
        {
            return _ResourceManager.PrepareAssetAsync(name, uniqueKey, overwrite);
        }

        public static IEnumerator PrepareAssetAsync(string name, bool overwrite = false)
        {
            return PrepareAssetAsync(name, name, overwrite);
        }
    }
}
