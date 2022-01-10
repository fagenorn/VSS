using Assets.Scripts.Common;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/SliderScrollRect")]
    public class SliderScrollRect : MonoBehaviour
    {
        //[SerializeField] private Slider _slider;

        //private bool ScrollingNeeded
        //{
        //    get
        //    {
        //        if (Application.isPlaying)
        //        {
        //            return m_ContentBounds.size.y > _viewBounds.size.y + 0.01f;
        //        }

        //        return true;
        //    }
        //}

        //private Bounds _viewBounds;

        //private void UpdateScrollingVisibility()
        //{
        //    if (_slider.gameObject.activeSelf != ScrollingNeeded)
        //    {
        //        _slider.gameObject.SetActive(ScrollingNeeded);
        //    }
        //}

        //public void OnScroll(float amount)
        //{
        //    _viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
        //    _slider.OnValueChangedAsAsyncEnumerable().Subscribe(OnScroll);
        //    UpdateScrollingVisibility();

        //    verticalNormalizedPosition = MathHelper.Normalize(amount, 0, 1, 1, 0);
        //}

        //public void OnScroll(PointerEventData eventData)
        //{
        //    if (!eventData.IsScrolling()) return;

        //    _slider.value += eventData.scrollDelta.y <= 0 ? 0.05f : -0.05f;
        //}

        //override protected void LateUpdate()
        //{

        //    base.LateUpdate();

        //    if (this.horizontalScrollbar)
        //    {

        //        this.horizontalScrollbar.size = 0;
        //    }
        //}

        //override public void Rebuild(CanvasUpdate executing)
        //{

        //    base.Rebuild(executing);

        //    if (this.horizontalScrollbar)
        //    {

        //        this.horizontalScrollbar.size = 0;
        //    }
        //}
    }
}
