using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
    internal class Toggle : MonoBehaviour
    {
        private IDisposable _disposableReactive;

        private IDisposable _disposableButton;

        [SerializeField] private float _speed = .1f;

        [SerializeField] private Button _button;

        [SerializeField] private RectTransform _dot;

        public AsyncReactiveProperty<bool> IsChecked = new AsyncReactiveProperty<bool>(false);

        private void Start()
        {
            _disposableReactive = IsChecked.Subscribe(OnToggle);
            _disposableButton = _button.OnClickAsAsyncEnumerable().Subscribe(x => IsChecked.Value = !IsChecked.Value);
        }

        private void OnDestroy()
        {
            _disposableReactive?.Dispose();
            _disposableButton?.Dispose();
        }

        private void OnToggle(bool toggled)
        {
            if (toggled)
            {
                _dot.DOAnchorMin(new Vector2(1, 0), _speed);
                _dot.DOAnchorMax(new Vector2(1, 1), _speed);
                _dot.DOPivot(new Vector2(1, 0.5f), _speed);
            }
            else
            {
                _dot.DOAnchorMin(new Vector2(0, 0), _speed);
                _dot.DOAnchorMax(new Vector2(0, 1), _speed);
                _dot.DOPivot(new Vector2(0, 0.5f), _speed);
            }
        }
    }
}
