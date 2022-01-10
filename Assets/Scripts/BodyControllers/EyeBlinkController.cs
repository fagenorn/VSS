using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Mediapipe;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.BodyControllers
{
    internal class EyeBlinkController : Live2DController
    {
        [SerializeField] private TMP_Text _labelLEye;
        [SerializeField] private TMP_Text _labelREye;

        [SerializeField] private float _minRatio = 1.5f;
        [SerializeField] private float _maxRatio = 2.5f;

        private const string _eyeLOpenId = "ParamEyeLOpen";
        private const string _eyeROpenId = "ParamEyeROpen";

        private CubismParameter _eyeLOpen;
        private CubismParameter _eyeROpen;

        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        protected override void LoadParams(CubismModel model)
        {
            _eyeLOpen = model.Parameters.FindById(_eyeLOpenId);
            _eyeROpen = model.Parameters.FindById(_eyeROpenId);
        }

        protected override void SyncNow()
        {
            isStale = false;

            var faceDict = this.MapFaceLandmarks(_currentFaceLandmarkList);

            var distanceLV = Vector3.Distance(faceDict[FacePart.Eye_Left_Inner_Top], faceDict[FacePart.Eye_Left_Inner_Bottom]);
            var distanceLH = Vector3.Distance(faceDict[FacePart.Eye_Left_Inner_Left], faceDict[FacePart.Eye_Left_Inner_Right]);
            var leftRatio = distanceLH / distanceLV;

            var distanceRV = Vector3.Distance(faceDict[FacePart.Eye_Right_Inner_Top], faceDict[FacePart.Eye_Right_Inner_Bottom]);
            var distanceRH = Vector3.Distance(faceDict[FacePart.Eye_Right_Inner_Right], faceDict[FacePart.Eye_Right_Inner_Left]);
            var rightRatio = distanceRH / distanceRV;

            var ratio = (leftRatio + rightRatio) / 2;
            var normalized = 1 - (ratio - _minRatio) / (_maxRatio - _minRatio);

            var normalizedL = 1 - (leftRatio - _minRatio) / (_maxRatio - _minRatio);
            var normalizedR = 1 - (rightRatio - _minRatio) / (_maxRatio - _minRatio);

            _eyeLOpen.BlendToValue(CubismParameterBlendMode.Override, normalized);
            _eyeROpen.BlendToValue(CubismParameterBlendMode.Override, normalized);

            if (_labelLEye != null) _labelLEye.text = $"LEye = {normalizedL:0.00}";
            if (_labelREye != null) _labelREye.text = $"REye = {normalizedR:0.00}";
        }

        public void UpdateFaceLandmarkList(NormalizedLandmarkList faceLandmarkList)
        {
            if (faceLandmarkList == null) return;
            UpdateCurrentTarget(faceLandmarkList.Landmark, ref _currentFaceLandmarkList);
        }
    }
}
