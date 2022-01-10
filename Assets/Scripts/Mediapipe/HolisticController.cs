using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Unity.CoordinateSystem;
using Mediapipe.Unity;

namespace Assets.Scripts.Mediapipe
{
    public class HolisticController : MonoBehaviour
    {
        [SerializeField] private GameObject _face;
        [SerializeField] private GameObject _leftIris;
        [SerializeField] private GameObject _rightIris;
        [SerializeField] private bool _isMirrored;

        private bool _isStale = false;

        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        private const int _FaceLandmarkCount = 468;
        private const int _IrisLandmarkCount = 5;
        private RectTransform _rect;

        private void Start()
        {
            _rect = gameObject.AddComponent<RectTransform>();

            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.anchoredPosition3D = Vector3.zero;
            _rect.sizeDelta = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_isStale)
            {
                Draw();
            }
        }

        protected virtual void OnDestroy()
        {
            _isStale = false;
        }

        public void DrawFaceLandmarkListLater(IList<NormalizedLandmark> faceLandmarkList)
        {
            UpdateCurrentTarget(faceLandmarkList, ref _currentFaceLandmarkList);
        }

        public void DrawFaceLandmarkListLater(NormalizedLandmarkList faceLandmarkList)
        {
            DrawFaceLandmarkListLater(faceLandmarkList?.Landmark);
        }

        private bool IsTargetChanged<TValue>(TValue newTarget, TValue currentTarget)
        {
            // It's assumed that target has not changed iff previous target and new target are both null.
            return currentTarget != null || newTarget != null;
        }

        private void UpdateCurrentTarget<TValue>(TValue newTarget, ref TValue currentTarget)
        {
            if (IsTargetChanged(newTarget, currentTarget))
            {
                currentTarget = newTarget;
                _isStale = true;
            }
        }

        private void Draw(bool visualizeZ = false, int circleVertices = 128)
        {
            _isStale = false;

            var landmarks = PartitionFaceLandmarkList(_currentFaceLandmarkList);

            if (ActivateFor(_face, landmarks.face))
            {
                var position = _rect.GetLocalPosition(landmarks.face[0], RotationAngle.Rotation0, _isMirrored);

                if (!visualizeZ)
                {
                    position.z = 0.0f;
                }

                _face.transform.localPosition = position;
            }

            if (ActivateFor(_leftIris, landmarks.irisLeft))
            {
                var position = _rect.GetLocalPosition(landmarks.irisLeft[0], RotationAngle.Rotation0, _isMirrored);

                if (!visualizeZ)
                {
                    position.z = 0.0f;
                }

                _leftIris.transform.localPosition = position;
            }
        }

        private static (IList<NormalizedLandmark> face, IList<NormalizedLandmark> irisLeft, IList<NormalizedLandmark> irisRight) PartitionFaceLandmarkList(IList<NormalizedLandmark> landmarks)
        {
            if (landmarks == null)
            {
                return (null, null, null);
            }

            var enumerator = landmarks.GetEnumerator();
            var faceLandmarks = new List<NormalizedLandmark>(_FaceLandmarkCount);
            for (var i = 0; i < _FaceLandmarkCount; i++)
            {
                if (enumerator.MoveNext())
                {
                    faceLandmarks.Add(enumerator.Current);
                }
            }

            if (faceLandmarks.Count < _FaceLandmarkCount)
            {
                return (null, null, null);
            }

            var leftIrisLandmarks = new List<NormalizedLandmark>(_IrisLandmarkCount);
            for (var i = 0; i < _IrisLandmarkCount; i++)
            {
                if (enumerator.MoveNext())
                {
                    leftIrisLandmarks.Add(enumerator.Current);
                }
            }

            if (leftIrisLandmarks.Count < _IrisLandmarkCount)
            {
                return (faceLandmarks, null, null);
            }

            var rightIrisLandmarks = new List<NormalizedLandmark>(_IrisLandmarkCount);
            for (var i = 0; i < _IrisLandmarkCount; i++)
            {
                if (enumerator.MoveNext())
                {
                    rightIrisLandmarks.Add(enumerator.Current);
                }
            }

            return rightIrisLandmarks.Count < _IrisLandmarkCount ? (faceLandmarks, leftIrisLandmarks, null) : (faceLandmarks, leftIrisLandmarks, rightIrisLandmarks);
        }

        public void SetActive(GameObject obj, bool isActive)
        {
            if (obj.activeSelf != isActive)
            {
                // obj.SetActive(isActive);
            }
        }

        private bool ActivateFor<T>(GameObject obj, T target)
        {
            if (target == null)
            {
                SetActive(obj, false);
                return false;
            }

            SetActive(obj, true);
            return true;
        }
    }
}
