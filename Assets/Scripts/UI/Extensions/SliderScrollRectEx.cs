using Assets.Scripts.Common;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/SliderScrollRectEx")]
    public class SliderScrollRectEx : ScrollRect
    {
        public Slider Slider;

        private bool _scrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                {
                    return m_ContentBounds.size.y > new Bounds(viewRect.rect.center, viewRect.rect.size).size.y + 0.01f;
                }

                return true;
            }
        }

        protected override void Start()
        {
            Slider.value = 0;
            Slider.OnValueChangedAsAsyncEnumerable().Subscribe(OnScroll);
            this.OnValueChangedAsAsyncEnumerable().Subscribe(OnScroll);
        }

        public void OnScroll(float amount)
        {
            verticalNormalizedPosition = MathHelper.Normalize(amount, 0, 1, 1, 0);
        }

        public void OnScroll(Vector2 amount)
        {
            Slider.SetValueWithoutNotify(MathHelper.Normalize(amount.y, 0, 1, 1, 0));
        }

        public override void OnScroll(PointerEventData eventData)
        {
            Slider.value += eventData.scrollDelta.y <= 0 ? 0.05f : -0.05f;
            base.OnScroll(eventData);
        }

        override protected void LateUpdate()
        {
            UpdateScrollbarVisibility();
            base.LateUpdate();
        }

        private void UpdateScrollbarVisibility()
        {
            if (!Slider)
            {
                return;
            }

            var scrollingNeeded = _scrollingNeeded;

            if (Slider.gameObject.activeSelf != scrollingNeeded)
            {
                Slider.gameObject.SetActive(scrollingNeeded);
            }
        }
    }
}
