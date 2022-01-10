using Assets.Scripts.Animations;
using Assets.Scripts.Common;
using Assets.Scripts.Live2D;
using Assets.Scripts.UI.Components;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Assets.Scripts.UI.Components.Toggle;

namespace Assets.Scripts.UI.Settings.Animations
{
    internal class Live2DAnimationProviderUI : MonoBehaviour
    {
        private enum AnimationPriorities
        {
            Normal = 0,
            VeryHigh = 1,
            ExtremlyHigh = 2,
            Highest = 3,
        }

        [SerializeField] private Button _delete;

        [SerializeField] private TMP_Text _selectedAnimation;

        [SerializeField] private Button _selectAnimation;

        [SerializeField] private Toggle _hotkeyToggle;

        [SerializeField] private TMP_Dropdown _priorityDropdown;

        [SerializeField] private TMP_Dropdown _modeDropdown;

        [SerializeField] private TMP_Text _selectedHotkey;

        [SerializeField] private Button _selectHotkey;

        [SerializeField] private Transform _settingsExpander;

        private Live2DAnimationProvider _animationProvider;

        private RectTransform _rect;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public AsyncReactiveProperty<AsyncUnit> ValueChanged = new AsyncReactiveProperty<AsyncUnit>(AsyncUnit.Default);

        private void Start()
        {
            _rect = GetComponent<RectTransform>();
            _verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            _hotkeyToggle.IsChecked.Subscribe(x => HotkeyToggle(x));
        }

        private void HotkeyToggle(bool isChecked, bool animate = true)
        {
            if (!isChecked)
            {
                _settingsExpander.DOScale(new Vector3(1, 0, 1), animate ? 0.15f : 0)
                                          .OnUpdate(() => { _verticalLayoutGroup.enabled = false; _verticalLayoutGroup.enabled = true; })
                                          .OnComplete(() => _settingsExpander.gameObject.SetActive(false));
            }
            else
            {
                _settingsExpander.gameObject.SetActive(true);
                _settingsExpander.DOScale(new Vector3(1, 1, 1), animate ? 0.15f : 0);
            }

            _animationProvider.HasHotkey = isChecked;
        }

        private async UniTaskVoid UpdateAnimationAsync(AsyncUnit unit)
        {
            var selected = await DialogManager.Instance.ShowItemsAsync(GlobalStore.Instance.CurrentVSSModel.Value.VSSMotionDataDict.Keys, "Select Animation");
            if (string.IsNullOrEmpty(selected)) return;
            if (!GlobalStore.Instance.CurrentVSSModel.Value.VSSMotionDataDict.ContainsKey(selected)) return;

            _animationProvider.MotionId = selected;
            _selectedAnimation.text = selected;

            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdatePriority(int option)
        {
            _animationProvider.Priority = ((AnimationPriorities)option) switch
            {
                AnimationPriorities.Normal => (int)Priorities.IdleAnimation,
                AnimationPriorities.VeryHigh => (int)Priorities.HotkeyAnimation,
                AnimationPriorities.ExtremlyHigh => (int)Priorities.HotkeyAnimation - 1,
                AnimationPriorities.Highest => (int)Priorities.HotkeyAnimation - 2,
                _ => throw new System.NotImplementedException(),
            };

            ValueChanged.Value = AsyncUnit.Default;
        }

        private void UpdateMode(int option)
        {
            _animationProvider.AnimationType = (AnimationType)option;

            ValueChanged.Value = AsyncUnit.Default;
        }

        private void SetPriority()
        {
            _priorityDropdown.value = _animationProvider.Priority switch
            {
                (int)Priorities.IdleAnimation => (int)AnimationPriorities.Normal,
                (int)Priorities.HotkeyAnimation => (int)AnimationPriorities.VeryHigh,
                (int)Priorities.HotkeyAnimation - 1 => (int)AnimationPriorities.ExtremlyHigh,
                (int)Priorities.HotkeyAnimation - 2 => (int)AnimationPriorities.Highest,
                _ => (int)AnimationPriorities.Normal,
            };

            ValueChanged.Value = AsyncUnit.Default;
        }

        private async UniTaskVoid UpdateHotkeyAsync(AsyncUnit unit)
        {
            _selectHotkey.interactable = false;
            var image = _selectHotkey.GetComponent<Image>();
            var text = _selectHotkey.GetComponentInChildren<TMP_Text>();
            var oldColor = image.color;
            var oldText = text.text;

            ColorUtility.TryParseHtmlString("#FAA0A0", out var red);

            image.color = red;
            text.text = "recording...";

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            try
            {
                var combo = await HotkeyManager.Instance.HotKey.WithoutCurrent().FirstAsync(cts.Token);

                await UniTask.SwitchToMainThread();

                _selectedHotkey.text = combo.ToString();
                _animationProvider.KeyCombo = combo;
            }
            catch
            {
                // ignored
            }
            finally
            {
                await UniTask.SwitchToMainThread();

                _selectHotkey.interactable = true;
                image.color = oldColor;
                text.text = oldText;
                cts.Dispose();
            }
        }

        private void DeleteParamAsync(AsyncUnit unit)
        {
            GlobalStore.Instance.CurrentVSSModel.Value.VSSModelData.AnimationParameters.Remove(_animationProvider);
            _rect.DOScale(0, .25f)
                   .OnComplete(() => Destroy(gameObject));

            ValueChanged.Value = AsyncUnit.Default;
        }

        public void Set(Live2DAnimationProvider live2DAnimationProvider)
        {
            _animationProvider = live2DAnimationProvider;

            _selectedAnimation.text = _animationProvider.MotionId ?? "-";
            _selectedHotkey.text = _animationProvider.KeyCombo.ToString();

            _hotkeyToggle.IsChecked.Value = _animationProvider.HasHotkey;
            _modeDropdown.value = (int)_animationProvider.AnimationType;

            _selectAnimation.OnClickAsAsyncEnumerable().Subscribe(UpdateAnimationAsync);
            _selectHotkey.OnClickAsAsyncEnumerable().Subscribe(UpdateHotkeyAsync);
            _delete.OnClickAsAsyncEnumerable().Subscribe(DeleteParamAsync);
            _priorityDropdown.onValueChanged.AddListener(UpdatePriority);
            _modeDropdown.onValueChanged.AddListener(UpdateMode);

            SetPriority();
            HotkeyToggle(_animationProvider.HasHotkey, false);
        }
    }
}
