using Live2D.Cubism.Core;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.CoordinateSystem;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.BodyControllers
{
    public abstract class Live2DController : MonoBehaviour
    {
        protected enum FacePart
        {
            Eye_Right_Inner_Right,
            Eye_Right_Inner_Left,
            Eye_Right_Inner_Top,
            Eye_Right_Inner_Bottom,
            Eye_Left_Inner_Right,
            Eye_Left_Inner_Left,
            Eye_Left_Inner_Top,
            Eye_Left_Inner_Bottom,
            Mouth_Right,
            Mouth_Left,
            Mouth_Inner_Top,
            Mouth_Inner_Bottom,
            Chin,
            Nose,
        }

        private Dictionary<FacePart, int> _facePartDict = new Dictionary<FacePart, int>
        {
            { FacePart.Nose, 1 },
            { FacePart.Eye_Right_Inner_Right, 33 },
            { FacePart.Eye_Right_Inner_Left, 133 },
            { FacePart.Eye_Right_Inner_Top, 159 },
            { FacePart.Eye_Right_Inner_Bottom, 145 },
            { FacePart.Eye_Left_Inner_Right, 362 },
            { FacePart.Eye_Left_Inner_Left, 263 },
            { FacePart.Eye_Left_Inner_Top, 386 },
            { FacePart.Eye_Left_Inner_Bottom, 374 },
            { FacePart.Mouth_Right, 61 },
            { FacePart.Mouth_Left, 291 },
            { FacePart.Mouth_Inner_Top, 13 },
            { FacePart.Mouth_Inner_Bottom, 14 },
            { FacePart.Chin, 199 },
        };

        protected enum IrisPart
        {
            L_1,
            L_2,
            L_3,
            L_4,
            L_C,
            R_1,
            R_2,
            R_3,
            R_4,
            R_C,
        }

        private Dictionary<IrisPart, int> _irisPartDict = new Dictionary<IrisPart, int>
        {
            { IrisPart.L_C, 0 },
            { IrisPart.L_1, 1 },
            { IrisPart.L_2, 2 },
            { IrisPart.L_3, 3 },
            { IrisPart.L_4, 4 },
            { IrisPart.R_C, 5 },
            { IrisPart.R_1, 6 },
            { IrisPart.R_2, 7 },
            { IrisPart.R_3, 8 },
            { IrisPart.R_4, 9 }
        };

        private RectTransform _rect;

        [SerializeField] private CubismModel _model;

        [SerializeField] private bool _enabled = true;

        protected bool isStale = false;

        public bool IsMirrored { get; set; }

        public RotationAngle RotationAngle { get; set; }

        private void Start()
        {
            _model = FindObjectOfType<CubismModel>();
            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();

            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.anchoredPosition3D = Vector3.zero;
            _rect.sizeDelta = new Vector2(1f, 1f);

            LoadParams(_model);
        }

        private void LateUpdate()
        {
            if (isStale && _enabled)
            {
                SyncNow();
            }
        }

        private void OnDestroy()
        {
            isStale = false;
        }

        protected abstract void LoadParams(CubismModel model);

        protected abstract void SyncNow();

        protected Dictionary<FacePart, Vector3> MapFaceLandmarks(IList<NormalizedLandmark> faceLandmarks)
        {
            var dict = new Dictionary<FacePart, Vector3>();
            foreach (var item in _facePartDict)
            {
                var landmark = faceLandmarks[item.Value];
                var point = _rect.GetLocalPosition(landmark, RotationAngle, IsMirrored);

                dict.Add(item.Key, point);
            }

            return dict;
        }

        protected Dictionary<IrisPart, Vector3> MapIrisLandmarks(IList<NormalizedLandmark> irisLandmarks)
        {
            var dict = new Dictionary<IrisPart, Vector3>();
            foreach (var item in _irisPartDict)
            {
                var landmark = irisLandmarks[item.Value];
                var point = _rect.GetLocalPosition(landmark, RotationAngle, IsMirrored);

                dict.Add(item.Key, point);
            }

            return dict;
        }

        protected void UpdateCurrentTarget<TValue>(TValue newTarget, ref TValue currentTarget)
        {
            if (IsTargetChanged(newTarget, currentTarget))
            {
                currentTarget = newTarget;
                isStale = true;
            }
        }

        protected bool IsTargetChanged<TValue>(TValue newTarget, TValue currentTarget)
        {
            // It's assumed that target has not changed if previous target and new target are both null.
            return currentTarget != null || newTarget != null;
        }
    }
}
