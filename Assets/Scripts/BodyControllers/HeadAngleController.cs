using Assets.Scripts.Common;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Mediapipe;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.BodyControllers
{
    internal class HeadAngleController : Live2DController
    {
        [SerializeField] private TMP_Text _labelX;
        [SerializeField] private TMP_Text _labelY;
        [SerializeField] private TMP_Text _labelZ;

        private const string _angleXId = "ParamAngleX";
        private const string _angleYId = "ParamAngleY";
        private const string _angleZId = "ParamAngleZ";

        private CubismParameter _angleX;
        private CubismParameter _angleY;
        private CubismParameter _angleZ;

        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        protected override void LoadParams(CubismModel model)
        {
            _angleX = model.Parameters.FindById(_angleXId);
            _angleY = model.Parameters.FindById(_angleYId);
            _angleZ = model.Parameters.FindById(_angleZId);
        }

        protected override void SyncNow()
        {
            isStale = false;

            var faceDict = this.MapFaceLandmarks(_currentFaceLandmarkList);

            Fit.Plane(faceDict.Values.ToList(), out var positionFace, out var normalFace, out var hAxis, out var vAxis, drawGizmos: false);

            var angleX = Vector3.SignedAngle(new Vector3(normalFace.x, 0, normalFace.z), Vector3.forward, Vector3.up);
            var angleY = Vector3.SignedAngle(new Vector3(0, normalFace.y, normalFace.z), Vector3.forward, Vector3.left);
            var angleZ = Vector3.SignedAngle(new Vector3(vAxis.x, vAxis.y, 0), Vector3.right, Vector3.forward) * 3;

            _angleX.BlendToValue(CubismParameterBlendMode.Override, angleX);
            _angleY.BlendToValue(CubismParameterBlendMode.Override, angleY);
            _angleZ.BlendToValue(CubismParameterBlendMode.Override, angleZ);

            if (_labelX != null) _labelX.text = $"X = {angleX:00}";
            if (_labelX != null) _labelY.text = $"Y = {angleY:00}";
            if (_labelX != null) _labelZ.text = $"Z = {angleZ:00}";
        }

        public void UpdateFaceLandmarkList(NormalizedLandmarkList faceLandmarkList)
        {
            if (faceLandmarkList == null) return;
            UpdateCurrentTarget(faceLandmarkList.Landmark, ref _currentFaceLandmarkList);
        }
    }
}
