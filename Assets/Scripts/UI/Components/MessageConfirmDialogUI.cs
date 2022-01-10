using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
    public class MessageConfirmDialogUI : MonoBehaviour
    {
        public enum DialogResult
        {
            Ok = 0,
            Cancel = 1,
        }

        [SerializeField] private TMP_Text _title;

        [SerializeField] private TMP_Text _message;

        [SerializeField] private Button _cancel;

        [SerializeField] private Button _ok;

        public async UniTask<DialogResult> ShowAsync(string message, string title)
        {
            _title.text = title;
            _message.text = message;

            var result = await UniTask.WhenAny(_ok.OnClickAsync(), _cancel.OnClickAsync());

            return (DialogResult)result;
        }
    }
}
