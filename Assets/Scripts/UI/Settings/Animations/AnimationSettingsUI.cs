using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Live2D;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Settings.Animations
{
    public class AnimationSettingsUI : MonoBehaviour
    {
        [SerializeField] private Button _resetBtn;

        [SerializeField] private Button _addBtn;

        [SerializeField] private Live2DAnimationProviderUI _live2DAnimationProviderUI;

        [SerializeField] private Transform _animationContent;

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
            _addBtn.OnClickAsAsyncEnumerable().Subscribe(AddAnimationProvider);
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

            foreach (var item in model.VSSModelData.AnimationParameters)
            {
                var instance = Instantiate(_live2DAnimationProviderUI, _animationContent);

                instance.Set(item);
                instance.ValueChanged.WithoutCurrent().Subscribe(_ => model.VSSModelData.SaveAsync());
                _rects.Add(instance.GetComponent<RectTransform>());

                await UniTask.WaitForEndOfFrame();
            }
        }

        private void AddAnimationProvider(AsyncUnit unit)
        {
            if (GlobalStore.Instance.CurrentVSSModel == null) return;

            var model = GlobalStore.Instance.CurrentVSSModel.Value;
            var provider = new Live2DAnimationProvider();
            provider.Enabled = true;
            provider.Priority = (int) Priorities.HotkeyAnimation;
            provider.AnimationType = AnimationType.OneShot;

            model.VSSModelData.AnimationParameters.Add(provider);

            var instance = Instantiate(_live2DAnimationProviderUI, _animationContent);
            instance.Set(provider);
            instance.ValueChanged.WithoutCurrent().Subscribe(_ => model.VSSModelData.SaveAsync());
            _rects.Add(instance.GetComponent<RectTransform>());

            model.VSSModelData.SaveAsync().Forget();
        }

        private async UniTaskVoid ResetBodyTrackerProviders(AsyncUnit unit)
        {
            if (GlobalStore.Instance.CurrentVSSModel == null) return;

            if (await DialogManager.Instance.ShowMessageConfirmAsync("This action will delete all your existing animations.\nAre you sure you wish to continue?", "Confirmation") != Components.MessageConfirmDialogUI.DialogResult.Ok)
            {
                return;
            }

            var model = GlobalStore.Instance.CurrentVSSModel.Value;

            model.VSSModelData.AnimationParameters.Clear();
            foreach (var item in ValueProviderFactory.DefaultAnimation(model))
            {
                model.VSSModelData.AnimationParameters.Add(item);
            }

            OnModelChange(model).Forget();
        }
    }
}
