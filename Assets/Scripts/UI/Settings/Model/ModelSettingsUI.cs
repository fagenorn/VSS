using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Live2D;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Settings.Model
{
    public class ModelSettingsUI : MonoBehaviour
    {
        [SerializeField] private Button _resetBtn;

        [SerializeField] private Button _addBtn;

        [SerializeField] private BodyTrackerValueProviderUI _bodyTrackerValueProviderUI;

        [SerializeField] private Transform _bodyTrackerContent;

        private List<RectTransform> _rects = new List<RectTransform>();

        public async UniTaskVoid OnEnable()
        {
            if (GlobalStore.Instance.CurrentVSSModel.Value != null) return;

            await DialogManager.Instance.ShowMessageAsync("No model has been selected.", "Warning");

            ScreenNavigator.Instance.HideScreen(Screen.Main_All);
            ScreenNavigator.Instance.ShowScreen(Screen.Model_Picker);
        }

        public void Start()
        {
            GlobalStore.Instance.CurrentVSSModel.Subscribe(OnModelChange);
            _addBtn.OnClickAsAsyncEnumerable().Subscribe(AddNewBodyTrackerProvider);
            _resetBtn.OnClickAsAsyncEnumerable().Subscribe(ResetBodyTrackerProviders);
        }

        private async UniTaskVoid OnModelChange(VSSModel model)
        {
            if (model == null) return;

            foreach (var rect in _rects)
            {
                if (rect == null) continue;
                Destroy(rect.gameObject);
            }

            _rects.Clear();

            foreach (var item in model.VSSModelData.TrackerParameters)
            {
                var instance = Instantiate(_bodyTrackerValueProviderUI, _bodyTrackerContent);

                instance.Set(item);
                instance.ValueChanged.WithoutCurrent().Subscribe(_ => model.VSSModelData.SaveAsync());
                _rects.Add(instance.GetComponent<RectTransform>());

                await UniTask.WaitForEndOfFrame();
            }
        }

        private void AddNewBodyTrackerProvider(AsyncUnit unit)
        {
            if (GlobalStore.Instance.CurrentVSSModel == null) return;

            var model = GlobalStore.Instance.CurrentVSSModel.Value;
            var provider = new BodyTrackerValueProvider();
            // provider.Enabled = true;

            model.VSSModelData.TrackerParameters.Add(provider);

            var instance = Instantiate(_bodyTrackerValueProviderUI, _bodyTrackerContent);
            instance.Set(provider);
            instance.ValueChanged.WithoutCurrent().Subscribe(_ => model.VSSModelData.SaveAsync());
            _rects.Add(instance.GetComponent<RectTransform>());

            model.VSSModelData.SaveAsync().Forget();
        }

        private async UniTaskVoid ResetBodyTrackerProviders(AsyncUnit unit)
        {
            if (GlobalStore.Instance.CurrentVSSModel == null) return;

            if (await DialogManager.Instance.ShowMessageConfirmAsync("This action will delete all your existing body parameters.\nAre you sure you wish to continue?", "Confirmation") != Components.MessageConfirmDialogUI.DialogResult.Ok)
            {
                return;
            }

            var model = GlobalStore.Instance.CurrentVSSModel.Value;

            model.VSSModelData.TrackerParameters.Clear();
            foreach (var item in ValueProviderFactory.DefaultBodyTracker(GlobalStore.Instance.CurrentVSSModel.Value.BodyTracker, model))
            {
                model.VSSModelData.TrackerParameters.Add(item);
            }

            OnModelChange(model).Forget();
        }
    }
}
