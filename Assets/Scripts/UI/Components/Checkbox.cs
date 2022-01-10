using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
    public class Checkbox : MonoBehaviour
    {
        [SerializeField] private Image _checkImage;

        public AsyncReactiveProperty<bool> IsChecked = new AsyncReactiveProperty<bool>(false);

        public void Toggle()
        {
            IsChecked.Value = !IsChecked.Value;
            _checkImage.DOFillAmount(IsChecked.Value ? 1 : 0, .1f);
        }

        public void Check()
        {
            IsChecked.Value = true;
            _checkImage.DOFillAmount(1, .1f);
        }

        public void UnCheck()
        {
            IsChecked.Value = false;
            _checkImage.DOFillAmount(0, .1f);
        }
    }
}
