using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Live2D;
using Assets.Scripts.UI.Components;
using Assets.Scripts.UI.Extensions;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using DG.Tweening;
using Live2D.Cubism.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Settings.Model
{
    internal class BodyTrackerValueProviderUI : MonoBehaviour
    {
        [SerializeField] private Button _delete;

        [SerializeField] private Button _selectBodyParam;

        [SerializeField] private Button _selectLive2DParam;

        [SerializeField] private Button _expandSettings;

        [SerializeField] private Transform _settingsExpander;

        [SerializeField] private TMP_Text _selectedBodyParam;

        [SerializeField] private TMP_Text _selectedLive2DParam;

        [SerializeField] private TMP_Text _currentBodyParam;

        [SerializeField] private TMP_Text _currentLive2DParam;

        [SerializeField] private Components.Slider _smoothingSlider;

        [SerializeField] private Components.Slider _sensitivitySlider;

        [SerializeField] private Components.Slider _minSlider;

        [SerializeField] private Components.Slider _maxSlider;

        private BodyTrackerValueProvider _bodyParameterProvider;

        private RectTransform _rect;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public AsyncReactiveProperty<AsyncUnit> ValueChanged = new AsyncReactiveProperty<AsyncUnit>(AsyncUnit.Default);

        private void Start()
        {
            _rect = GetComponent<RectTransform>();
            _verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            _expandSettings.OnClickAsAsyncEnumerable().Subscribe(ToggleExpander);
        }

        private void ToggleExpander(AsyncUnit unit)
        {
            if (_settingsExpander.gameObject.activeSelf)
            {
                _settingsExpander.DOScale(new Vector3(1, 0, 1), 0.15f)
                                          .OnUpdate(() => { _verticalLayoutGroup.enabled = false; _verticalLayoutGroup.enabled = true; })
                                          .OnComplete(() => _settingsExpander.gameObject.SetActive(false));
                _expandSettings.transform.DOBlendableRotateBy(new Vector3(0, 0, 180), 0.25f);
            }
            else
            {
                _settingsExpander.gameObject.SetActive(true);
                _settingsExpander.DOScale(new Vector3(1, 1, 1), 0.15f);
                _expandSettings.transform.DOBlendableRotateBy(new Vector3(0, 0, -180), 0.25f);
            }
        }

        private void UpdateSmoothing(float value)
        {
            _bodyParameterProvider.SmoothTime = MathHelper.Normalize(value, 0, 10, 0, 1);
            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdateSensitivity(float value)
        {
            _bodyParameterProvider.Sensitivity = value < 0 ? MathHelper.Normalize(value, -10, 0, -0.001f, -1) : MathHelper.Normalize(value, 0, 10, 1, 0.001f);
            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdateMin(float value)
        {
            if (_bodyParameterProvider.Live2DParameter == null) return;
            _bodyParameterProvider.OutputMin = MathHelper.Normalize(value, 0, 10, _bodyParameterProvider.Live2DParameter.MinimumValue, _bodyParameterProvider.Live2DParameter.MaximumValue);
            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdateMax(float value)
        {
            if (_bodyParameterProvider.Live2DParameter == null) return;
            _bodyParameterProvider.OutputMax = MathHelper.Normalize(value, 0, 10, _bodyParameterProvider.Live2DParameter.MinimumValue, _bodyParameterProvider.Live2DParameter.MaximumValue);
            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdateMinMax()
        {
            _minSlider.SetValue(_bodyParameterProvider.Live2DParameter == null ? 0 : MathHelper.Normalize(_bodyParameterProvider.OutputMin, _bodyParameterProvider.Live2DParameter.MinimumValue, _bodyParameterProvider.Live2DParameter.MaximumValue, 0, 10));
            _maxSlider.SetValue(_bodyParameterProvider.Live2DParameter == null ? 10 : MathHelper.Normalize(_bodyParameterProvider.OutputMax, _bodyParameterProvider.Live2DParameter.MinimumValue, _bodyParameterProvider.Live2DParameter.MaximumValue, 0, 10));
        }

        private async UniTaskVoid UpdateBodyParamAsync(AsyncUnit unit)
        {
            var selected = await DialogManager.Instance.ShowItemsAsync(Enum.GetNames(typeof(BodyParam)), "Select Body Param");
            if (string.IsNullOrEmpty(selected)) return;
            if (!Enum.TryParse(selected, out BodyParam selectedEnum)) return;

            _bodyParameterProvider.BodyParameterType = selectedEnum;
            _bodyParameterProvider.InputMin = _bodyParameterProvider.BodyParameter?.DefaultMin ?? 0;
            _bodyParameterProvider.InputMax = _bodyParameterProvider.BodyParameter?.DefaultMax ?? 1;
            _selectedBodyParam.text = selectedEnum.ToString();

            ValueChanged.Value = AsyncUnit.Default;
        }

        private async UniTaskVoid UpdateLive2DParamAsync(AsyncUnit unit)
        {
            var selected = await DialogManager.Instance.ShowItemsAsync(GlobalStore.Instance.CurrentVSSModel.Value.Live2DParamDict.Keys, "Select Live2D Param");
            if (string.IsNullOrEmpty(selected)) return;
            if (!GlobalStore.Instance.CurrentVSSModel.Value.Live2DParamDict.ContainsKey(selected)) return;

            _bodyParameterProvider.Live2DParameterId = selected;
            _bodyParameterProvider.OutputMin = _bodyParameterProvider.Live2DParameter.MinimumValue;
            _bodyParameterProvider.OutputMax = _bodyParameterProvider.Live2DParameter.MaximumValue;
            _selectedLive2DParam.text = selected;

            UpdateMinMax();

            ValueChanged.Value = AsyncUnit.Default;
        }

        private void DeleteParamAsync(AsyncUnit unit)
        {
            GlobalStore.Instance.CurrentVSSModel.Value.VSSModelData.TrackerParameters.Remove(_bodyParameterProvider);
            _rect.DOScale(0, .25f)
                   .OnComplete(() => Destroy(gameObject));

            ValueChanged.Value = AsyncUnit.Default;
        }

        public void Set(BodyTrackerValueProvider bodyTrackerValueProvider)
        {
            _bodyParameterProvider = bodyTrackerValueProvider;

            _selectedBodyParam.text = bodyTrackerValueProvider.BodyParameterType.ToString();
            _selectedLive2DParam.text = bodyTrackerValueProvider.Live2DParameterId ?? "None";

            _smoothingSlider.SetValue(MathHelper.Normalize(_bodyParameterProvider.SmoothTime, 0, 1, 0, 10));
            _sensitivitySlider.SetValue(_bodyParameterProvider.Sensitivity < 0 ? MathHelper.Normalize(_bodyParameterProvider.Sensitivity, -1, -0.001f, 0, -10) : MathHelper.Normalize(_bodyParameterProvider.Sensitivity, 0.001f, 1, 10, 0));

            UpdateMinMax();

            _smoothingSlider.SetLabel("Smoothing");
            _sensitivitySlider.SetLabel("Sensitivity");
            _minSlider.SetLabel("Minimum");
            _maxSlider.SetLabel("Maximum");

            _smoothingSlider.SetMinMax(0, 10);
            _sensitivitySlider.SetMinMax(-10, 10);
            _minSlider.SetMinMax(0, 10);
            _maxSlider.SetMinMax(0, 10);

            _smoothingSlider.CurrentSlider.OnValueChangedAsAsyncEnumerable().Subscribe(UpdateSmoothing);
            _sensitivitySlider.CurrentSlider.OnValueChangedAsAsyncEnumerable().Subscribe(UpdateSensitivity);
            _minSlider.CurrentSlider.OnValueChangedAsAsyncEnumerable().Subscribe(UpdateMin);
            _maxSlider.CurrentSlider.OnValueChangedAsAsyncEnumerable().Subscribe(UpdateMax);

            _selectBodyParam.OnClickAsAsyncEnumerable().Subscribe(UpdateBodyParamAsync);
            _selectLive2DParam.OnClickAsAsyncEnumerable().Subscribe(UpdateLive2DParamAsync);
            _delete.OnClickAsAsyncEnumerable().Subscribe(DeleteParamAsync);
        }

        private void Update()
        {
            if (_bodyParameterProvider == null) return;
            if (!_rect.IsVisibleFrom(UnityEngine.Camera.main)) return;

            _currentBodyParam.text = _bodyParameterProvider.BodyParameterType == BodyParam.None ? "0.0" : _bodyParameterProvider.BodyParameter.Value.ToString("0.0");
            _currentLive2DParam.text = _bodyParameterProvider.Live2DParameterId == null ? "0.0" : _bodyParameterProvider.Live2DParameter.Value.ToString("0.0");
        }
    }
}
