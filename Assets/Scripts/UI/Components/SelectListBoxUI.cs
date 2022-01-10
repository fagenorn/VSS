using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Components
{
    public class SelectListBoxUI : MonoBehaviour
    {
        [SerializeField] private SelectListBoxItemUI _selectListBoxItem;

        [SerializeField] private Button _cancle;

        [SerializeField] private Button _select;

        [SerializeField] private TMP_Text _title;

        [SerializeField] private Transform _container;

        private Image _parent;

        private RectTransform _rect;

        private object _selected;

        private List<(SelectListBoxItemUI Select, object Value)> _selectItems = new List<(SelectListBoxItemUI, object)>();

        private LinkedList<SelectListBoxItemUI> _cachedSelectItems = new LinkedList<SelectListBoxItemUI>();

        private List<IDisposable> _subscriptions = new List<IDisposable>();

        private void OnEnable()
        {
            _select.interactable = false;
            _selected = null;
        }

        public async UniTask<T> ShowAsync<T>(IEnumerable<T> items, string title)
        {
            _title.text = title;
            var finished = UniTask.WhenAny(_select.OnClickAsync(), _cancle.OnClickAsync());

            var itt = 0;
            foreach (var item in items)
            {
                if (!TryGetCached(out var instance))
                {
                    instance = Instantiate(_selectListBoxItem, _container);
                    instance.Checkbox.IsChecked.Where(x => x).Subscribe(x => OnCheck(instance, x));
                }

                instance.SetText(item.ToString());
                _selectItems.Add((instance, item));

                itt++;
                if (itt % 10 == 0) await UniTask.Yield();

                if (finished.Status == UniTaskStatus.Succeeded)
                {
                    break;
                }
            }

            var result = await finished;

            Cache();

            return result == 0 ? (T)_selected : default(T);
        }

        private void OnCheck(SelectListBoxItemUI instance, bool check)
        {
            foreach (var item in _selectItems)
            {
                if (item.Select == instance)
                {
                    _selected = item.Value;
                    continue;
                };

                item.Select.Checkbox.UnCheck();
            }

            _select.interactable = true;
        }

        private void Cache()
        {
            _selectItems.Reverse();

            foreach (var item in _selectItems)
            {
                item.Select.Checkbox.UnCheck();
                item.Select.gameObject.SetActive(false);
                _cachedSelectItems.AddLast(item.Select);
            }

            _selectItems.Clear();
        }

        private bool TryGetCached(out SelectListBoxItemUI selectItem)
        {
            selectItem = null;

            if (!_cachedSelectItems.Any()) return false;
            selectItem = _cachedSelectItems.LastOrDefault();
            selectItem.gameObject.SetActive(true);
            _cachedSelectItems.RemoveLast();

            return true;
        }
    }
}
