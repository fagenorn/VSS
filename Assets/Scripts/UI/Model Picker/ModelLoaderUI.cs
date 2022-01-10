using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Assets.Scripts.Storage;
using Assets.Scripts.UI.Components;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using SimpleFileBrowser;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Model_Picker
{
    public class ModelLoaderUI : MonoBehaviour
    {
        [SerializeField] private ModelItemUI _preFab;

        [SerializeField] private BodyTracker _bodyTracker;

        [SerializeField] private Transform _content;

        [SerializeField] private Button _loadModel;

        private bool _loading = false;

        private async UniTaskVoid Start()
        {
            await UniTask.WaitUntil(() => GlobalStore.Instance.VSSModelLoader.GetCurrentVSSModelData() != null);
            await RefreshUIAsync();

            _loadModel.OnClickAsAsyncEnumerable().Subscribe(LoadModelDialog);
        }

        private async UniTask RefreshUIAsync()
        {
            foreach (Transform item in _content)
            {
                Destroy(item.gameObject);
            }

            foreach (var model in GlobalStore.Instance.VSSModelLoader.GetCurrentVSSModelData())
            {
                var modelItem = Instantiate(_preFab, _content);

                await modelItem.SetData(model);

                modelItem.OnClickAsync()
                               .Subscribe(LoadModelAsync);

                await UniTask.Yield();
            }
        }

        private async UniTaskVoid LoadModelAsync(VSSModelData modelData)
        {
            if (_loading) return;
            _loading = true;

            GameObject oldObj = null;
            if (GlobalStore.Instance.CurrentVSSModel.Value != null)
            {
                oldObj = GlobalStore.Instance.CurrentVSSModel.Value.Live2DModel.gameObject;
            }

            await GlobalStore.Instance.VSSModelLoader.LoadVSSModelAsync(modelData, _bodyTracker);

            if (oldObj != null) Destroy(oldObj);

            ScreenNavigator.Instance.HideScreen(Screen.Model_Picker);

            _loading = false;
        }

        private async UniTaskVoid LoadModelDialog(AsyncUnit unity)
        {
            _loadModel.interactable = false;

            try
            {
                FileBrowser.AddQuickLink("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), null);
                FileBrowser.AddQuickLink("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null);
                FileBrowser.AddQuickLink("Current Models", Live2DModelLoader.ModelsFolderPath, null);

                await FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), null, "Load Model", "Load").ToUniTask();

                if (!FileBrowser.Success || FileBrowser.Result.Length != 1) return;

                IOHelper.CopyFilesRecursively(FileBrowser.Result[0], Path.Combine(Live2DModelLoader.ModelsFolderPath, Path.GetFileName(FileBrowser.Result[0])));

                await GlobalStore.Instance.VSSModelLoader.RefreshModels();
                await RefreshUIAsync();
            }
            catch
            {
                await DialogManager.Instance.ShowMessageAsync("Failed to load model.", "Error");
            }
            finally
            {
                _loadModel.interactable = true;
            }
        }
    }
}
