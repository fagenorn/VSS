
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Components
{
    public class Slider : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider _slider;

        [SerializeField] private TMP_Text _label;

        [SerializeField] private TMP_Text _labelValue;

        [SerializeField] private float _interval = 0.1f;

        public UnityEngine.UI.Slider CurrentSlider => _slider;

        private void Awake()
        {
            _slider.OnValueChangedAsAsyncEnumerable().Subscribe(x =>
            {
                _slider.value = Mathf.Round(_slider.value / _interval) * _interval;
                _labelValue.text = _slider.value.ToString("0.0");
            });
        }

        public void SetLabel(string label)
        {
            _label.text = label;
        }

        public void SetMinMax(float min, float max)
        {
            _slider.minValue = min;
            _slider.maxValue = max;
        }

        public void SetValue(float value)
        {
            _slider.value = value;
            _labelValue.text = value.ToString("0.0");
        }
    }
}
