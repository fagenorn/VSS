using UnityEngine;

namespace Assets.Scripts.Room
{
    public class CameraColorSetter : MonoBehaviour
    {
        [SerializeField] private FlexibleColorPicker _fcp;

        private void Awake()
        {
            _fcp.onColorChange.AddListener(SetColor);
            _fcp.gameObject.SetActive(false);
            ColorUtility.TryParseHtmlString("#F0D9FF", out var color);
            Camera.main.backgroundColor = color;
        }

        private void SetColor(Color color)
        {
            Camera.main.backgroundColor = color;
        }

        public void Toggle()
        {
            _fcp.SetColor(Camera.main.backgroundColor);
            _fcp.gameObject.SetActive(!_fcp.gameObject.activeSelf);
        }
    }
}
