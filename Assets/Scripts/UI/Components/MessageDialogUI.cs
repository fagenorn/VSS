using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
    public class MessageDialogUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;

        [SerializeField] private TMP_Text _message;

        [SerializeField] private Button _ok;

        public async UniTask ShowAsync(string message, string title)
        {
            _title.text = title;
            _message.text = message;

            await _ok.OnClickAsync();
        }
    }
}
