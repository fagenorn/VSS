using Assets.Scripts.UI.Components;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class DialogManager : MonoBehaviour
    {
        [SerializeField] private MessageConfirmDialogUI _messageConfirmDialog;

        [SerializeField] private MessageDialogUI _messageDialog;

        [SerializeField] private SelectListBoxUI _selectListDialog;

        [SerializeField] private Image _dialogWindow;

        public static DialogManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Init();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Init()
        {
            _messageConfirmDialog = Instantiate(_messageConfirmDialog);
            _messageConfirmDialog.GetComponent<RectTransform>().SetParent(_dialogWindow.transform, false);

            _messageDialog = Instantiate(_messageDialog);
            _messageDialog.GetComponent<RectTransform>().SetParent(_dialogWindow.transform, false);

            _selectListDialog = Instantiate(_selectListDialog);
            _selectListDialog.GetComponent<RectTransform>().SetParent(_dialogWindow.transform, false);

            _dialogWindow.gameObject.SetActive(false);
            _messageConfirmDialog.gameObject.SetActive(false);
            _messageDialog.gameObject.SetActive(false);
            _selectListDialog.gameObject.SetActive(false);
        }

        public async UniTask<MessageConfirmDialogUI.DialogResult> ShowMessageConfirmAsync(string message, string title)
        {
            Show(_messageConfirmDialog.gameObject);

            try
            {
                return await _messageConfirmDialog.ShowAsync(message, title);
            }
            finally
            {
                Close(_messageConfirmDialog.gameObject);
            }
        }

        public async UniTask ShowMessageAsync(string message, string title)
        {
            Show(_messageDialog.gameObject);

            try
            {
                await _messageDialog.ShowAsync(message, title);
            }
            finally
            {
                Close(_messageDialog.gameObject);
            }
        }

        public async UniTask<T> ShowItemsAsync<T>(IEnumerable<T> items, string title)
        {
            Show(_selectListDialog.gameObject);

            try
            {
                return await _selectListDialog.ShowAsync<T>(items, title);
            }
            finally
            {
                Close(_selectListDialog.gameObject);
            }
        }

        private void Show(GameObject dialog)
        {
            var rect = dialog.GetComponent<RectTransform>();
            _dialogWindow.gameObject.SetActive(true);
            dialog.SetActive(true);
            rect.localScale = Vector3.zero;
            rect.DOScale(Vector3.one, .2f);
        }

        private void Close(GameObject dialog)
        {
            var rect = dialog.GetComponent<RectTransform>();
            Sequence sequence = null;
            sequence = DOTween.Sequence().Append(_dialogWindow.DOFade(0, .2f))
                                                          .Join(rect.DOAnchorPosY(-1000, .2f))
                                                          .OnComplete(() =>
                                                          {
                                                              sequence.Rewind();
                                                              rect.gameObject.SetActive(false);
                                                              _dialogWindow.gameObject.SetActive(false);
                                                          });

            sequence.Play();
        }
    }
}
