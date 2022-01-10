using Assets.Scripts.Common;
using Assets.Scripts.Live2D;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Storage
{
    public class Live2DModelLoader
    {
        private string _modelConfigSearchPattern = @"(?i)^.*\.vss\.json$";

        private string _iconSearchPattern = @"(?i)^icon\.(jpg|png)$";

        private string _modelSearchPattern = @"(?i)^.*\.model3\.json$";

        public static string ModelsFolderName = "Models";

        public static string ModelsFolderPath => Path.Combine(Application.streamingAssetsPath, ModelsFolderName);

        public List<VSSModelData> CurrentModels { get; private set; }

        public async UniTask GetModelsDataAsync()
        {
            InitializeDirectories();
            await InitializeDetectNewModelsAsync();
            CurrentModels = await GetAvailableModelsAsync();
        }

        public async UniTask<CubismModel> LoadModelAsync(VSSModelData model)
        {
            var path = model.GetFullPath(model.Files.Model);
            var model3Json = await CubismModel3Json.LoadAtPathAsync(path, BuiltinLoadAssetAtPath);

            return await model3Json.ToModelAsync();
        }

        private static async UniTask<object> BuiltinLoadAssetAtPath(Type assetType, string absolutePath)
        {
            await UniTask.SwitchToThreadPool();

            if (assetType == typeof(byte[]))
            {
                using (var fileStream = File.OpenRead(absolutePath))
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms);
                    await UniTask.SwitchToMainThread();

                    return ms.ToArray();
                }
            }
            else if (assetType == typeof(string))
            {
                using (var fileStream = File.OpenRead(absolutePath))
                using (var reader = new StreamReader(fileStream))
                {
                    var read = await reader.ReadToEndAsync();
                    await UniTask.SwitchToMainThread();

                    return read;
                }
            }
            else if (assetType == typeof(Texture2D))
            {

                using (var fileStream = File.OpenRead(absolutePath))
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms);
                    await UniTask.SwitchToMainThread();

                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(ms.ToArray());

                    return texture;
                }
            }

            throw new NotSupportedException();
        }

        private void InitializeDirectories()
        {
            Directory.CreateDirectory(ModelsFolderPath);
        }

        private async UniTask InitializeDetectNewModelsAsync()
        {

            var folders = Directory.GetDirectories(ModelsFolderPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var folder in folders)
            {
                string path = IOHelper.GetFilesRegexSearch(folder, _modelConfigSearchPattern, SearchOption.TopDirectoryOnly).FirstOrDefault()?.Name;
                if (path != null) continue;

                await CreateNewModelConfigAsync(folder);
            }
        }

        private async UniTask<List<VSSModelData>> GetAvailableModelsAsync()
        {
            var models = new List<VSSModelData>();

            var files = IOHelper.GetFilesRegexSearch(ModelsFolderPath, _modelConfigSearchPattern, SearchOption.AllDirectories);
            if (files.Count == 0) return models;

            foreach (var file in files)
            {
                using (var fileStream = file.OpenRead())
                using (var reader = new StreamReader(fileStream))
                {
                    var json = await reader.ReadToEndAsync();
                    var model = VSSModelData.FromJson(json);

                    model.SetCurrentPath(file.FullName);
                    models.Add(model);
                }
            }

            ModelPreCheck(models);

            return models;
        }

        private void ModelPreCheck(List<VSSModelData> models)
        {
            foreach (var item in models)
            {
                if (item.Transform == null)
                {
                    item.Transform = new TransformData
                    {
                        Position = Vector3.zero,
                        Scale = Vector3.one,
                        Rotation = Quaternion.identity,
                    };
                }

                if(item.AnimationParameters == null)
                {
                    item.AnimationParameters = new ObservableCollection<Live2DAnimationProvider>();
                }
            }
        }

        private async UniTask CreateNewModelConfigAsync(string path)
        {
            var iconFile = IOHelper.GetFilesRegexSearch(path, _iconSearchPattern).FirstOrDefault()?.Name;
            var modelFile = IOHelper.GetFilesRegexSearch(path, _modelSearchPattern).FirstOrDefault()?.Name;
            if (modelFile == null) return;

            var modelName = Path.GetFileName(modelFile).Split('.').FirstOrDefault() ?? modelFile;

            var model = new VSSModelData
            {
                Id = Guid.NewGuid(),
                Name = modelName,
                ImportedDate = DateTimeOffset.UtcNow,
                Files = new ModelFiles
                {
                    Icon = new ModelFile { Name = iconFile },
                    Model = new ModelFile { Name = modelFile }
                },
                TrackerParameters = new ObservableCollection<BodyTrackerValueProvider>(),
                AnimationParameters = new ObservableCollection<Live2DAnimationProvider>(),
                Transform = new TransformData
                {
                    Position = Vector3.zero,
                    Scale = Vector3.one,
                    Rotation = Quaternion.identity,
                }
            };

            using (var outputFile = new StreamWriter(Path.Combine(path, $"{modelName}.vss.json")))
            {
                await outputFile.WriteAsync(model.ToJson());
            }
        }
    }
}