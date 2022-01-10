using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Model_Picker
{
    public class ModelItemUI : MonoBehaviour, IPointerClickHandler, IDisposable
    {
        [SerializeField] private TMP_Text _name;

        [SerializeField] private Image _icon;

        [SerializeField] private Sprite _defaultIcon;

        private VSSModelData _modelData;

        private Channel<VSSModelData> _onClick;

        IConnectableUniTaskAsyncEnumerable<VSSModelData> _multicastSource;

        IDisposable _connection;

        public async Task SetData(VSSModelData data)
        {
            _modelData = data;
            _name.text = data.Name;
            _icon.sprite = data.Files.Icon.Name == null ? _defaultIcon : (await IOHelper.LoadImage(data.GetFullPath(data.Files.Icon))).ToSprite();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_onClick != null) _onClick.Writer.TryWrite(_modelData);
        }

        public IUniTaskAsyncEnumerable<VSSModelData> OnClickAsync()
        {
            if (_onClick == null)
            {
                _onClick = Channel.CreateSingleConsumerUnbounded<VSSModelData>();
                _multicastSource = _onClick.Reader.ReadAllAsync().Publish();
                _connection = _multicastSource.Connect();
            }

            return _multicastSource;
        }

        public void Dispose()
        {
            _onClick.Writer.TryComplete();
            _connection.Dispose();
        }
    }
}
