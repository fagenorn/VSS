using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Mediapipe;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.BodyControllers
{
    internal class MouthOpenController : Live2DController
    {
        [SerializeField] private TMP_Text _labelY;
        [SerializeField] private float _maxHeight = 0.02f;

        private const string _mouthYId = "ParamMouthOpenY";

        private CubismParameter _mouthY;

        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        protected override void LoadParams(CubismModel model)
        {
            _mouthY = model.Parameters.FindById(_mouthYId);
        }

        protected override void SyncNow()
        {
            isStale = false;

            var faceDict = this.MapFaceLandmarks(_currentFaceLandmarkList);
            var distance = faceDict[FacePart.Mouth_Inner_Top] - faceDict[FacePart.Mouth_Inner_Bottom];
            var normalized = distance.y / _maxHeight;

            _mouthY.BlendToValue(CubismParameterBlendMode.Override, normalized);

            if (_labelY != null) _labelY.text = $"Y = {normalized:0.00}";
        }

        public void UpdateFaceLandmarkList(NormalizedLandmarkList faceLandmarkList)
        {
            if (faceLandmarkList == null) return;
            UpdateCurrentTarget(faceLandmarkList.Landmark, ref _currentFaceLandmarkList);
        }
    }
}
