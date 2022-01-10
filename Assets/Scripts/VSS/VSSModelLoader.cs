using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Assets.Scripts.Room;
using Assets.Scripts.Storage;
using Cysharp.Threading.Tasks;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.VSS
{
    public class VSSModelLoader
    {
        private Live2DModelLoader _modelLoader = new Live2DModelLoader();

        private Live2DMotionLoader _motionLoader = new Live2DMotionLoader();

        public void PreLoad()
        {
            _modelLoader.GetModelsDataAsync().Forget();
        }

        public async UniTask RefreshModels()
        {
            await _modelLoader.GetModelsDataAsync();
        }

        public List<VSSModelData> GetCurrentVSSModelData() => _modelLoader.CurrentModels;

        public async UniTask<VSSModel> LoadVSSModelAsync(VSSModelData modelData, BodyTracker bodyTracker)
        {
            var vssModel = new VSSModel
            {
                VSSModelData = modelData,
                Live2DModel = await LoadLive2DModelAsync(modelData),
                BodyTracker = bodyTracker,
        };

            await UniTask.Yield();

            vssModel.VSSMotionDataDict = await LoadMotionsDataAsync(vssModel);
            vssModel.Live2DParamDict = vssModel.Live2DModel.Parameters.ToDictionary(x => x.Id, x => x);
            vssModel.Live2DPartDict = vssModel.Live2DModel.Parts.ToDictionary(x => x.Id, x => x);

            GlobalStore.Instance.CurrentVSSModel.Value = vssModel;

            return vssModel;
        }

        private async UniTask<CubismModel> LoadLive2DModelAsync(VSSModelData modelData)
        {
            var live2DModel = await _modelLoader.LoadModelAsync(modelData);

            // rotate/move/zoom
            live2DModel.gameObject.AddComponent<MouseTransform>();

            // Hitdetection for transforms
            var collider = live2DModel.gameObject.AddComponent<CapsuleCollider2D>();
            collider.size = GetBBox(live2DModel);

            live2DModel.gameObject.SetActive(true);

            return live2DModel;
        }

        private async UniTask<Dictionary<string, VSSMotionData>> LoadMotionsDataAsync(VSSModel model)
        {
            var animations = await _motionLoader.LoadMotionsDataAsync(model.VSSModelData);

            var cubismFadeController = model.Live2DModel.gameObject.AddComponent<CubismFadeController>();
            cubismFadeController.CubismFadeMotionList = ScriptableObject.CreateInstance<CubismFadeMotionList>();
            cubismFadeController.CubismFadeMotionList.CubismFadeMotionObjects = animations.Select(x => x.CubismFadeMotionData).ToArray();
            cubismFadeController.CubismFadeMotionList.MotionInstanceIds = animations.Select(x => x.Clip.GetInstanceID()).ToArray();

            var cubismMotionController = model.Live2DModel.gameObject.AddComponent<CubismMotionController>();
            model.CubismMotionController = cubismMotionController;
            model.CubismFadeController = cubismFadeController;

            return animations.ToDictionary(x => x.Name, x => x);
        }

        private Vector2 GetBBox(CubismModel model)
        {
            float maxX = 0;
            float maxY = 0;
            float minX = 0;
            float minY = 0;

            // Get Bouding box for 2d collider
            foreach (var item in model.Drawables)
            {
                foreach (var item2 in item.VertexPositions)
                {
                    if (item2.x > maxX)
                    {
                        maxX = item2.x;
                    }

                    if (item2.x < minX)
                    {
                        minX = item2.x;
                    }

                    if (item2.y > maxY)
                    {
                        maxY = item2.y;
                    }

                    if (item2.y < minY)
                    {
                        minY = item2.y;
                    }
                }
            }

            return new Vector2(Mathf.Max(Mathf.Abs(maxX), Mathf.Abs(minX)) * 2, Mathf.Max(Mathf.Abs(maxY), Mathf.Abs(minY)) * 2);
        }
    }
}
