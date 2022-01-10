using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI.Components
{
    public class SelectListBoxItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text _text;

        [SerializeField] public Checkbox Checkbox;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            Checkbox.Check();
        }

        public void SetText(string text)
        {
            _text.text = text;
        }
    }
}
