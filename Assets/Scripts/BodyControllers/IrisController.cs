using Assets.Scripts.Common;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Mediapipe;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.BodyControllers
{
    internal class IrisController : Live2DController
    {
        [SerializeField] private TMP_Text _labelEyeX;
        [SerializeField] private TMP_Text _labelEyeY;

        [SerializeField] private float _minAngle = -10f;
        [SerializeField] private float _maxAngle = 10f;

        private const string _eyeballXId = "ParamEyeBallX";
        private const string _eyeballYId = "ParamEyeBallY";

        private CubismParameter _eyeballX;
        private CubismParameter _eyeballY;

        private IList<NormalizedLandmark> _currentIrisLandmarkList;
        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        protected override void LoadParams(CubismModel model)
        {
            _eyeballX = model.Parameters.FindById(_eyeballXId);
            _eyeballY = model.Parameters.FindById(_eyeballYId);
        }

        protected override void SyncNow()
        {
            isStale = false;

            var irisDict = this.MapIrisLandmarks(_currentIrisLandmarkList);
            var faceDict = this.MapFaceLandmarks(_currentFaceLandmarkList);

            Fit.Plane(irisDict.Values.ToList(), out var _, out var normalIris, out var _, out var _, drawGizmos: false);
            Fit.Plane(faceDict.Values.ToList(), out var _, out var normalFace, out var _, out var _, drawGizmos: false);

            normalIris = new Vector3(Mathf.Abs(normalIris.x), Mathf.Abs(normalIris.y), Mathf.Abs(normalIris.z));
            normalFace = new Vector3(Mathf.Abs(normalFace.x), Mathf.Abs(normalFace.y), Mathf.Abs(normalFace.z));

            var normalIrisX = normalIris;
            normalIrisX.y = 0;

            var normalFaceX = normalFace;
            normalFaceX.y = 0;

            var normalIrisY = normalIris;
            normalIrisY.x = 0;

            var normalFaceY = normalFace;
            normalFaceY.x = 0;

            var angleX = Vector3.SignedAngle(normalIrisX, normalFaceX, Vector3.up); // Not accurate
            var angleY = Vector3.SignedAngle(normalIrisY, normalFaceY, Vector3.right);

            var normalizedX = (angleX - _minAngle) / (_maxAngle - _minAngle);
            var normalizedY = (angleY - _minAngle) / (_maxAngle - _minAngle);

            _eyeballX.BlendToValue(CubismParameterBlendMode.Override, normalizedX);
            _eyeballY.BlendToValue(CubismParameterBlendMode.Override, normalizedY);

            if (_labelEyeX != null) _labelEyeX.text = $"X: {normalizedX:0.00}";
            if (_labelEyeY != null) _labelEyeY.text = $"Y: {normalizedY:0.00}";
        }

        public void UpdateIrisLandmarkList(NormalizedLandmarkList faceLandmarkList)
        {
            if (faceLandmarkList == null) return;
            UpdateCurrentTarget(faceLandmarkList.Landmark.Take(468).ToList(), ref _currentFaceLandmarkList);

            if (faceLandmarkList.Landmark.Count != 478) return;
            UpdateCurrentTarget(faceLandmarkList.Landmark.Skip(468).ToList(), ref _currentIrisLandmarkList);
        }
    }
}
