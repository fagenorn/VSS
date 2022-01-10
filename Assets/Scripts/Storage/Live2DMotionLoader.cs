using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.MotionFade;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assets.Scripts.Storage
{
    public class Live2DMotionLoader
    {
        private string _motionSearchPattern = @"(?i)^.*\.motion3\.json$";

        public List<VSSModelData> CurrentModels { get; private set; }

        public async UniTask<List<VSSMotionData>> LoadMotionsDataAsync(VSSModelData model)
        {
            var fullPath = model.GetFullPath(model.Files.Model);
            var folderPath = Path.GetDirectoryName(fullPath);
            var files = IOHelper.GetFilesRegexSearch(folderPath, _motionSearchPattern, SearchOption.AllDirectories);

            return (await files.Select(async f => await LoadMotionDataAsync(f.FullName))).ToList();
        }

        public CubismFadeMotionData LoadFadeMotionData(VSSMotionData motionData)
        {
            return CubismFadeMotionData.CreateInstance(motionData.CubismMotion3Json, motionData.Name, motionData.CubismMotion3Json.Meta.Duration);
        }

        private async UniTask<VSSMotionData> LoadMotionDataAsync(string path)
        {
            using (var fileStream = File.OpenRead(path))
            using (var reader = new StreamReader(fileStream))
            {
                var json = await reader.ReadToEndAsync();
                var motion3Json = CubismMotion3Json.LoadFrom(json);

                var motionData = new VSSMotionData
                {
                    Name = Path.GetFileNameWithoutExtension(path).Replace(".motion3", string.Empty),
                    Clip = motion3Json.ToAnimationClip(),
                    CubismMotion3Json = motion3Json,
                };

                motionData.CubismFadeMotionData = LoadFadeMotionData(motionData);

                return motionData;
            }
        }
    }
}