using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Room
{
    public class MouseTransform : MonoBehaviour
    {
        public float _zoomMinSize = .1f;

        public float _zoomMaxSize = 4f;

        public float _zoomIncrementSize = .1f;

        private Vector2 _anchor;

        private float _currentZoom;

        private bool _isDragging;

        private bool _isZooming;

        public float _zoomSpeed = .25f;

        public float _dragSmoothingSpeed = 20.0f;

        private VSSModelData _modelData;

        private void Awake()
        {
            GlobalStore.Instance.CurrentVSSModel.WithoutCurrent().Subscribe(OnModelChange, this.GetCancellationTokenOnDestroy());
        }

        private void OnModelChange(VSSModel model)
        {
            _currentZoom = model.VSSModelData.Transform.Scale.X;
            _modelData = model.VSSModelData;

            transform.DOLocalMove(model.VSSModelData.Transform.Position, 1f).SetEase(Ease.OutQuad);
            transform.DOScale(model.VSSModelData.Transform.Scale, 1f).SetEase(Ease.OutQuad);
            transform.rotation = model.VSSModelData.Transform.Rotation;
        }

        private void UpdateModel()
        {
            var modelData = _modelData;
            if (modelData == null) return;

            modelData.Transform.Position = transform.localPosition;
            modelData.Transform.Scale = transform.localScale;
            modelData.Transform.Rotation = transform.rotation;

            modelData.SaveAsync().Forget();
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            var transformPosition = transform.localPosition;
            var mouseScreenPos = Input.mousePosition;
            var mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            var ray = Camera.main.ScreenPointToRay(mouseScreenPos);
            var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            var mouseOnGameObject = hit.collider != null && hit.collider.transform == this.transform && !EventSystem.current.IsPointerOverGameObject();
            Vector2 zoomAnchor = Vector2.one;
            if (mouseOnGameObject)
            {
                _isZooming = true;
                zoomAnchor = mouseWorldPos - transformPosition;
            }
            else
            {
                _isZooming = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (mouseOnGameObject)
                {
                    _anchor = mouseWorldPos - transformPosition;
                    _isDragging = true;
                }
                else
                {
                    _isDragging = false;
                }
            }

            if (_isDragging)
            {
                Vector2 point = mouseWorldPos - transformPosition;
                var viewPortTransform = Camera.main.WorldToViewportPoint(new Vector3(transformPosition.x + point.x, transformPosition.y + point.y, transformPosition.z));

                if (viewPortTransform.x >= 0 && viewPortTransform.x <= 1 && viewPortTransform.y >= 0 && viewPortTransform.y <= 1)
                {
                    transform.Translate((point - _anchor) * Time.deltaTime * _dragSmoothingSpeed);
                }

                UpdateModel();
            }

            if (_isZooming)
            {
                float mousescroll = Input.GetAxis("Mouse ScrollWheel");
                if (mousescroll > 0f && _currentZoom + _zoomIncrementSize <= _zoomMaxSize)
                {
                    _currentZoom += _zoomIncrementSize;
                    transform.DOBlendableScaleBy(_zoomIncrementSize * Vector3.one, _zoomSpeed);
                }
                else if (mousescroll < 0f && _currentZoom - _zoomIncrementSize >= _zoomMinSize)
                {
                    _currentZoom -= _zoomIncrementSize;
                    transform.DOBlendableScaleBy(-_zoomIncrementSize * Vector3.one, _zoomSpeed);
                }

                UpdateModel();
            }
        }
    }
}
