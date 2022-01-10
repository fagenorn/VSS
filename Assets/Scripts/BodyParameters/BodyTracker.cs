using Assets.Scripts.Barracuda;
using Assets.Scripts.Common;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.CoordinateSystem;
using RenderHeads.Media.AVProLiveCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.BodyParameters
{
    public class BodyTracker : MonoBehaviour
    {
        private struct PointVisibility
        {
            public Vector3 point;

            public PointVisibility(Vector3 point, float visiblity) : this()
            {
                this.point = point;
                this.visiblity = visiblity;
            }

            public float visiblity;
        }

        [SerializeField] private bool _enabled = true;

        [SerializeField] private EmotionDetector _emotionDetector;

        private RotationAngle _rotationAngle = RotationAngle.Rotation0;

        private bool _isMirrored = false;

        private bool _isFaceStale = false;

        private bool _isPoseStale = false;

        private bool _isLeftHandStale = false;

        private bool _isRightHandStale = false;

        private RectTransform _rect;

        private IList<NormalizedLandmark> _currentFaceLandmarkList;

        private IList<Landmark> _currentPoseWorldLandmarkList;

        private IList<NormalizedLandmark> _currentLeftHandLandmarkList;

        private IList<NormalizedLandmark> _currentRightHandLandmarkList;

        private Dictionary<FacePart, Vector3> _currentFaceMappings = new Dictionary<FacePart, Vector3>();

        private Dictionary<IrisPart, Vector3> _currentIrisMappings = new Dictionary<IrisPart, Vector3>();

        private Dictionary<PosePart, PointVisibility> _currentPoseMappings = new Dictionary<PosePart, PointVisibility>();

        private Dictionary<HandPart, Vector3> _currentLeftHandMappings = new Dictionary<HandPart, Vector3>();

        private Dictionary<HandPart, Vector3> _currentRightHandMappings = new Dictionary<HandPart, Vector3>();

        private float _minBrowY = -1.4f;

        private float _maxBrowY = -.5f;

        private float _minEyeLRatio = .15f;

        private float _maxEyeLRatio = .5f;

        private float _minEyeRRatio = .15f;

        private float _maxEyeRRatio = .5f;

        private float _minMouthRatio = .2f;

        private float _maxMouthRatio = 1.5f;

        #region Face Params

        public BodyParameterInstance FaceX { get; private set; }

        public BodyParameterInstance FaceY { get; private set; }

        public BodyParameterInstance FaceZ { get; private set; }

        public BodyParameterInstance FaceAngleX { get; private set; }

        public BodyParameterInstance FaceAngleY { get; private set; }

        public BodyParameterInstance FaceAngleZ { get; private set; }

        public BodyParameterInstance EyeLOpen { get; private set; }

        public BodyParameterInstance EyeROpen { get; private set; }

        public BodyParameterInstance EyeLX { get; private set; }

        public BodyParameterInstance EyeLY { get; private set; }

        public BodyParameterInstance EyeRX { get; private set; }

        public BodyParameterInstance EyeRY { get; private set; }

        public BodyParameterInstance MouthOpen { get; private set; }

        public BodyParameterInstance BrowLY { get; private set; }

        public BodyParameterInstance BrowRY { get; private set; }

        #region Emotions

        public BodyParameterInstance Neutral { get; private set; }

        public BodyParameterInstance Happy { get; private set; }

        public BodyParameterInstance Supprised { get; private set; }

        public BodyParameterInstance Frowning { get; private set; }

        #endregion

        #endregion

        #region Pose Params

        public BodyParameterInstance BodyAngleX { get; private set; }

        public BodyParameterInstance BodyAngleY { get; private set; }

        public BodyParameterInstance BodyAngleZ { get; private set; }

        public BodyParameterInstance ArmLAngle1 { get; private set; }

        public BodyParameterInstance ArmLAngle2 { get; private set; }

        public BodyParameterInstance ArmRAngle1 { get; private set; }

        public BodyParameterInstance ArmRAngle2 { get; private set; }

        #endregion

        #region Hand Params

        public BodyParameterInstance WristLAngle { get; private set; }

        public BodyParameterInstance WristRAngle { get; private set; }

        #endregion

        public BodyParameterInstance GetBodyParameter(BodyParam bodyParam)
        {
            return bodyParam switch
            {
                BodyParam.FaceX => FaceX,
                BodyParam.FaceY => FaceY,
                BodyParam.FaceZ => FaceZ,
                BodyParam.FaceAngleX => FaceAngleX,
                BodyParam.FaceAngleY => FaceAngleY,
                BodyParam.FaceAngleZ => FaceAngleZ,
                BodyParam.EyeLOpen => EyeLOpen,
                BodyParam.EyeROpen => EyeROpen,
                BodyParam.EyeLX => EyeLX,
                BodyParam.EyeLY => EyeLY,
                BodyParam.EyeRX => EyeRX,
                BodyParam.EyeRY => EyeRY,
                BodyParam.MouthOpen => MouthOpen,
                BodyParam.BodyAngleX => BodyAngleX,
                BodyParam.BodyAngleY => BodyAngleY,
                BodyParam.BodyAngleZ => BodyAngleZ,
                BodyParam.ArmLAngle1 => ArmLAngle1,
                BodyParam.ArmLAngle2 => ArmLAngle2,
                BodyParam.ArmRAngle1 => ArmRAngle1,
                BodyParam.ArmRAngle2 => ArmRAngle2,
                BodyParam.WristLAngle => WristLAngle,
                BodyParam.WristRAngle => WristRAngle,
                BodyParam.NeutralEmotion => Neutral,
                BodyParam.HappyEmotion => Happy,
                BodyParam.SupprisedEmotion => Supprised,
                BodyParam.FrowningEmotion => Frowning,
                BodyParam.BrowLY => BrowLY,
                BodyParam.BrowRY => BrowRY,
                _ => null,
            };
        }

        public void SetImageSourceInfo(AVProLiveCamera camera)
        {
            _isMirrored = camera._flipX;
            _rotationAngle = RotationAngle.Rotation0.Reverse();
        }

        public void UpdateFaceLandmarkList(NormalizedLandmarkList faceLandmarkList)
        {
            if (faceLandmarkList == null) return;

            if (IsTargetChanged(faceLandmarkList.Landmark, _currentFaceLandmarkList))
            {
                _currentFaceLandmarkList = faceLandmarkList.Landmark;
                _isFaceStale = true;
            }
        }

        public void UpdatePoseWorldLandmarkList(LandmarkList poseWorldLandmarks)
        {
            if (poseWorldLandmarks == null) return;

            if (IsTargetChanged(poseWorldLandmarks.Landmark, _currentPoseWorldLandmarkList))
            {
                _currentPoseWorldLandmarkList = poseWorldLandmarks.Landmark;
                _isPoseStale = true;
            }
        }

        public void UpdateLeftLandmarkList(NormalizedLandmarkList leftHandLandmarks)
        {
            if (leftHandLandmarks == null) return;

            if (IsTargetChanged(leftHandLandmarks.Landmark, _currentLeftHandLandmarkList))
            {
                _currentLeftHandLandmarkList = leftHandLandmarks.Landmark;
                _isLeftHandStale = true;
            }
        }

        public void UpdateRightLandmarkList(NormalizedLandmarkList rightHandLandmarks)
        {
            if (rightHandLandmarks == null) return;

            if (IsTargetChanged(rightHandLandmarks.Landmark, _currentRightHandLandmarkList))
            {
                _currentRightHandLandmarkList = rightHandLandmarks.Landmark;
                _isRightHandStale = true;
            }
        }

        #region Landmark Mapping

        private enum FacePart
        {
            Eye_Left_Inner_Right,
            Eye_Left_Inner_Left,
            Eye_Left_Inner_Top1,
            Eye_Left_Inner_Top2,
            Eye_Left_Inner_Bottom1,
            Eye_Left_Inner_Bottom2,
            Eye_Right_Inner_Right,
            Eye_Right_Inner_Left,
            Eye_Right_Inner_Top1,
            Eye_Right_Inner_Top2,
            Eye_Right_Inner_Bottom1,
            Eye_Right_Inner_Bottom2,
            Mouth_Left_Outer,
            Mouth_Left_Inner,
            Mouth_Right_Outer,
            Mouth_Right_Inner,
            Mouth_Outer_Top1,
            Mouth_Outer_Top2,
            Mouth_Outer_Top3,
            Mouth_Inner_Top1,
            Mouth_Inner_Top2,
            Mouth_Inner_Top3,
            Mouth_Outer_Bottom1,
            Mouth_Outer_Bottom2,
            Mouth_Outer_Bottom3,
            Mouth_Inner_Bottom1,
            Mouth_Inner_Bottom2,
            Mouth_Inner_Bottom3,
            Chin,
            Nose,
            Head_Top,
            Head_Left,
            Head_Right,
            Brow_Left_Y,
            Brow_Right_Y,
            Forehead_Left,
            Forehead_Right,
            Eye_Left_Upper,
            Eye_Right_Upper,
        }

        private Dictionary<FacePart, int> _facePartDict = new Dictionary<FacePart, int>
        {
            { FacePart.Nose, 1 },
            { FacePart.Eye_Left_Inner_Right, 33 },
            { FacePart.Eye_Left_Inner_Left, 133 },
            { FacePart.Eye_Left_Inner_Top1, 159 },
            { FacePart.Eye_Left_Inner_Top2, 158 },
            { FacePart.Eye_Left_Inner_Bottom1, 145 },
            { FacePart.Eye_Left_Inner_Bottom2, 153 },
            { FacePart.Eye_Right_Inner_Right, 362 },
            { FacePart.Eye_Right_Inner_Left, 263 },
            { FacePart.Eye_Right_Inner_Top1, 386 },
            { FacePart.Eye_Right_Inner_Top2, 385 },
            { FacePart.Eye_Right_Inner_Bottom1, 374 },
            { FacePart.Eye_Right_Inner_Bottom2, 380 },
            { FacePart.Mouth_Left_Outer, 61 },
            { FacePart.Mouth_Left_Inner, 78 },
            { FacePart.Mouth_Right_Outer, 291 },
            { FacePart.Mouth_Right_Inner, 308 },
            { FacePart.Mouth_Outer_Top1, 12 },
            { FacePart.Mouth_Outer_Top2, 268 },
            { FacePart.Mouth_Outer_Top3, 38 },
            { FacePart.Mouth_Inner_Top1, 13 },
            { FacePart.Mouth_Inner_Top2, 312 },
            { FacePart.Mouth_Inner_Top3, 82 },
            { FacePart.Mouth_Outer_Bottom1, 15 },
            { FacePart.Mouth_Outer_Bottom2, 316 },
            { FacePart.Mouth_Outer_Bottom3, 86 },
            { FacePart.Mouth_Inner_Bottom1, 14 },
            { FacePart.Mouth_Inner_Bottom2, 317 },
            { FacePart.Mouth_Inner_Bottom3, 87 },
            { FacePart.Chin, 199 },
            { FacePart.Head_Top, 10 },
            { FacePart.Head_Left, 234 },
            { FacePart.Head_Right, 454 },
            { FacePart.Brow_Left_Y, 52 },
            { FacePart.Brow_Right_Y, 282 },
            { FacePart.Forehead_Left, 104 },
            { FacePart.Forehead_Right, 333 },
            { FacePart.Eye_Left_Upper, 223 },
            { FacePart.Eye_Right_Upper, 443 },
        };

        private enum IrisPart
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
            { IrisPart.L_C, 468 + 0 },
            { IrisPart.L_1, 468 + 1 },
            { IrisPart.L_2, 468 + 2 },
            { IrisPart.L_3, 468+ 3 },
            { IrisPart.L_4, 468+ 4 },
            { IrisPart.R_C, 468 + 5 },
            { IrisPart.R_1, 468 + 6 },
            { IrisPart.R_2, 468 + 7 },
            { IrisPart.R_3, 468 + 8 },
            { IrisPart.R_4, 468 + 9 }
        };

        private enum PosePart
        {
            Shoulder_Left,
            Shoulder_Right,
            Hip_Left,
            Hip_Right,
            Elbow_Left,
            Elbow_Right,
            Wrist_Left,
            Wrist_Right,
        }

        private Dictionary<PosePart, int> _posePartDict = new Dictionary<PosePart, int>
        {
            { PosePart.Shoulder_Left, 11 },
            { PosePart.Shoulder_Right, 12 },
            { PosePart.Hip_Left, 23 },
            { PosePart.Hip_Right, 24 },
            { PosePart.Elbow_Left, 13 },
            { PosePart.Elbow_Right, 14 },
            { PosePart.Wrist_Left, 15 },
            { PosePart.Wrist_Right, 16 },
        };

        private enum HandPart
        {
            Wrist,
            Index_Mcp
        }

        private Dictionary<HandPart, int> _handPartDict = new Dictionary<HandPart, int>
        {
            { HandPart.Wrist, 0 },
            { HandPart.Index_Mcp, 5 },
        };

        private void UpdateFaceMappings()
        {
            if (_currentFaceLandmarkList == null) return;

            foreach (var item in _facePartDict)
            {
                var landmark = _currentFaceLandmarkList[item.Value];
                var point = _rect.GetLocalPosition(landmark, _rotationAngle, _isMirrored);

                _currentFaceMappings[item.Key] = point;
            }

            if (_currentFaceLandmarkList.Count != 478) return;

            foreach (var item in _irisPartDict)
            {
                var landmark = _currentFaceLandmarkList[item.Value];
                var point = _rect.GetLocalPosition(landmark, _rotationAngle, _isMirrored);

                _currentIrisMappings[item.Key] = point;
            }
        }

        private void UpdatePoseMappings()
        {
            if (_currentPoseWorldLandmarkList == null) return;

            foreach (var item in _posePartDict)
            {
                var landmark = _currentPoseWorldLandmarkList[item.Value];
                var point = _rect.GetLocalPosition(landmark, Vector3.one, _rotationAngle, _isMirrored);

                _currentPoseMappings[item.Key] = new PointVisibility(point, landmark.Visibility);
            }
        }

        private void UpdateLeftHandMappings()
        {
            if (_currentLeftHandLandmarkList == null) return;

            foreach (var item in _handPartDict)
            {
                var landmark = _currentLeftHandLandmarkList[item.Value];
                var point = _rect.GetLocalPosition(landmark, _rotationAngle, _isMirrored);

                _currentLeftHandMappings[item.Key] = point;
            }
        }

        private void UpdateRightHandMappings()
        {
            if (_currentRightHandLandmarkList == null) return;

            foreach (var item in _handPartDict)
            {
                var landmark = _currentRightHandLandmarkList[item.Value];
                var point = _rect.GetLocalPosition(landmark, _rotationAngle, _isMirrored);

                _currentRightHandMappings[item.Key] = point;
            }
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            FaceX = new BodyParameterInstance(-30, 30);
            FaceY = new BodyParameterInstance(-30, 30);
            FaceZ = new BodyParameterInstance(-30, 30);

            FaceAngleX = new BodyParameterInstance(-30, 30);
            FaceAngleY = new BodyParameterInstance(-30, 30);
            FaceAngleZ = new BodyParameterInstance(-30, 30);

            EyeLOpen = new BodyParameterInstance(0, 1);
            EyeROpen = new BodyParameterInstance(0, 1);

            EyeLX = new BodyParameterInstance(-1, 1);
            EyeLY = new BodyParameterInstance(-1, 1);
            EyeRX = new BodyParameterInstance(-1, 1);
            EyeRY = new BodyParameterInstance(-1, 1);

            MouthOpen = new BodyParameterInstance(0, 1);

            BrowLY = new BodyParameterInstance(-1, 1);
            BrowRY = new BodyParameterInstance(-1, 1);

            Neutral = new BodyParameterInstance(0, 1);
            Happy = new BodyParameterInstance(0, 1);
            Supprised = new BodyParameterInstance(0, 1);
            Frowning = new BodyParameterInstance(0, 1);

            BodyAngleX = new BodyParameterInstance(-60, 60);
            BodyAngleY = new BodyParameterInstance(-60, 60);
            BodyAngleZ = new BodyParameterInstance(-60, 60);

            ArmLAngle1 = new BodyParameterInstance(0, 180);
            ArmLAngle2 = new BodyParameterInstance(0, 180);

            ArmRAngle1 = new BodyParameterInstance(0, 180);
            ArmRAngle2 = new BodyParameterInstance(0, 180);

            WristLAngle = new BodyParameterInstance(0, 180);
            WristRAngle = new BodyParameterInstance(0, 180);
        }

        private void Start()
        {
            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();

            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.anchoredPosition3D = Vector3.zero;
            _rect.sizeDelta = new Vector2(1f, 1f);
        }

        private void OnDrawGizmos()
        {
            if (_currentFaceLandmarkList == null) return;

            foreach (var item in _currentFaceLandmarkList)
            {
                Gizmos.color = Color.black;
                var point = _rect.GetLocalPosition(item, _rotationAngle, _isMirrored);
                Gizmos.DrawSphere(point, 0.002f);
            }

            if (_currentFaceMappings == null) return;

            foreach (var item in _currentFaceMappings)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(item.Value, 0.002f);
            }

            if (_currentIrisMappings == null) return;

            foreach (var item in _currentIrisMappings)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(item.Value, 0.002f);
            }

            // if (_currentPoseWorldLandmarkList == null) return;

            // foreach (var item in _currentPoseWorldLandmarkList)
            // {
            //     Gizmos.color = Color.black;
            //     var point = _rect.GetLocalPosition(item, Vector3.one, _rotationAngle, _isMirrored);
            //     Gizmos.DrawSphere(point, 0.01f);
            // }

            // if (_currentPoseMappings == null) return;

            // foreach (var item in _currentPoseMappings)
            // {
            //     Gizmos.color = Color.red;
            //     Gizmos.DrawSphere(item.Value.point, 0.01f);
            // }

            if (_currentLeftHandLandmarkList == null) return;

            foreach (var item in _currentLeftHandLandmarkList)
            {
                Gizmos.color = Color.black;
                var point = _rect.GetLocalPosition(item, _rotationAngle, _isMirrored);
                Gizmos.DrawSphere(point, 0.002f);
            }

            if (_currentLeftHandMappings == null) return;

            foreach (var item in _currentLeftHandMappings)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(item.Value, 0.002f);
            }

            if (_currentRightHandLandmarkList == null) return;

            foreach (var item in _currentRightHandLandmarkList)
            {
                Gizmos.color = Color.black;
                var point = _rect.GetLocalPosition(item, _rotationAngle, _isMirrored);
                Gizmos.DrawSphere(point, 0.002f);
            }

            if (_currentRightHandMappings == null) return;

            foreach (var item in _currentRightHandMappings)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(item.Value, 0.002f);
            }
        }

        private void LateUpdate()
        {
            if (!_enabled) return;

            if (_isFaceStale)
            {
                _isFaceStale = false;

                UpdateFaceMappings();

                UpdateFacePosition();
                UpdateFaceAngle();
                UpdateIrisPosition();
                UpdateEyeOpen();
                UpdateBrows();
                UpdateMouth();
                UpdateEmotions();
            }

            if (_isPoseStale)
            {
                _isPoseStale = false;

                UpdatePoseMappings();

                UpdateBodyAngle();
                UpdateArms();
            }

            if (_isLeftHandStale)
            {
                _isLeftHandStale = false;

                UpdateLeftHandMappings();

                UpdateLeftWristAngle();
            }

            if (_isRightHandStale)
            {
                _isRightHandStale = false;

                UpdateRightHandMappings();

                UpdateRightWristAngle();
            }
        }

        #region Update Face Params

        private void UpdateFacePosition()
        {
            var position = _currentFaceMappings[FacePart.Nose];
            var x = MathHelper.Normalize(position.x, -.5f, .5f, -1, 1);
            var y = MathHelper.Normalize(position.y, -.5f, .5f, -1, 1);
            var z = MathHelper.Normalize(position.z, 0, -.1f, -1, 1);

            FaceX.Value = x;
            FaceY.Value = y;
            FaceZ.Value = z;
        }

        private void UpdateFaceAngle()
        {
            var tilt = _currentFaceMappings[FacePart.Head_Top] - _currentFaceMappings[FacePart.Chin];
            Fit.Plane(new[] { _currentFaceMappings[FacePart.Head_Top], _currentFaceMappings[FacePart.Head_Right], _currentFaceMappings[FacePart.Head_Left], _currentFaceMappings[FacePart.Chin] }, out var _, out var normalFace, out var _, out var _, 25);

            FaceAngleX.Value = Vector3.SignedAngle(new Vector3(normalFace.x, 0, normalFace.z), Vector3.forward, Vector3.up);
            FaceAngleY.Value = Vector3.SignedAngle(new Vector3(0, normalFace.y, normalFace.z), Vector3.forward, Vector3.left);
            FaceAngleZ.Value = Vector3.SignedAngle(new Vector3(tilt.x, tilt.y, 0), Vector3.up, Vector3.forward);
        }

        private void UpdateBrows()
        {
            var leftCenter = _currentFaceMappings[FacePart.Brow_Left_Y];
            var leftUpper = _currentFaceMappings[FacePart.Forehead_Left];
            var leftDown = _currentFaceMappings[FacePart.Eye_Left_Upper];

            var rightCenter = _currentFaceMappings[FacePart.Brow_Right_Y];
            var rightUpper = _currentFaceMappings[FacePart.Forehead_Right];
            var rightDown = _currentFaceMappings[FacePart.Eye_Right_Upper];

            var left = GetDistance(leftCenter, leftUpper, leftDown);
            var right = GetDistance(rightCenter, rightUpper, rightDown);

            BrowLY.Value = MathHelper.Normalize(left, _minBrowY, _maxBrowY, -1, 1);
            BrowRY.Value = MathHelper.Normalize(right, _minBrowY, _maxBrowY, -1, 1);
        }

        private void UpdateEyeOpen()
        {
            // Avoid tracking eyes if out of frame
            // if (FaceAngleX.Value > 30 || FaceAngleX.Value < -30 || FaceAngleY.Value > 30 | FaceAngleY.Value < -10) return;

            var leftRight = _currentFaceMappings[FacePart.Eye_Left_Inner_Right];
            var leftLeft = _currentFaceMappings[FacePart.Eye_Left_Inner_Left];
            var leftTop1 = _currentFaceMappings[FacePart.Eye_Left_Inner_Top1];
            var leftBottom1 = _currentFaceMappings[FacePart.Eye_Left_Inner_Bottom1];
            var leftTop2 = _currentFaceMappings[FacePart.Eye_Left_Inner_Top2];
            var leftBottom2 = _currentFaceMappings[FacePart.Eye_Left_Inner_Bottom2];

            var rightRight = _currentFaceMappings[FacePart.Eye_Right_Inner_Right];
            var rightLeft = _currentFaceMappings[FacePart.Eye_Right_Inner_Left];
            var rightTop1 = _currentFaceMappings[FacePart.Eye_Right_Inner_Top1];
            var rightBottom1 = _currentFaceMappings[FacePart.Eye_Right_Inner_Bottom1];
            var rightTop2 = _currentFaceMappings[FacePart.Eye_Right_Inner_Top2];
            var rightBottom2 = _currentFaceMappings[FacePart.Eye_Right_Inner_Bottom2];


            var leftRatio = GetDistanceRatio(new[] { leftRight, leftLeft, leftRight, leftLeft }, new[] { leftTop1, leftBottom1, leftTop2, leftBottom2 });
            var rightRatio = GetDistanceRatio(new[] { rightRight, rightLeft, rightRight, rightLeft }, new[] { rightTop1, rightBottom1, rightTop2, rightBottom2 });

            if (FaceAngleX.Value > 20)
            {
                leftRatio = rightRatio;
            }

            if (FaceAngleX.Value < -20)
            {
                rightRatio = leftRatio;
            }

            EyeLOpen.Value = MathHelper.Normalize(leftRatio, _minEyeLRatio, _maxEyeLRatio, 0, 1);
            EyeROpen.Value = MathHelper.Normalize(rightRatio, _minEyeRRatio, _maxEyeRRatio, 0, 1);
        }

        private void UpdateIrisPosition()
        {
            // Avoid tracking eyes if out of frame
            //if (FaceAngleX.Value > 30 || FaceAngleX.Value < -30 || FaceAngleY.Value > 30 | FaceAngleY.Value < -10) return;

            var leftCenter = _currentIrisMappings[IrisPart.L_C];
            var leftRight = _currentFaceMappings[FacePart.Eye_Left_Inner_Right];
            var leftLeft = _currentFaceMappings[FacePart.Eye_Left_Inner_Left];
            var leftTop = _currentFaceMappings[FacePart.Eye_Left_Inner_Top1];
            var leftBottom = _currentFaceMappings[FacePart.Eye_Left_Inner_Bottom1];

            var rightCenter = _currentIrisMappings[IrisPart.R_C];
            var rightRight = _currentFaceMappings[FacePart.Eye_Right_Inner_Right];
            var rightLeft = _currentFaceMappings[FacePart.Eye_Right_Inner_Left];
            var rightTop = _currentFaceMappings[FacePart.Eye_Right_Inner_Top1];
            var rightBottom = _currentFaceMappings[FacePart.Eye_Right_Inner_Bottom1];

            var lIrisX = GetDistance(leftCenter, leftRight, leftLeft);
            var lIrisY = GetDistance(leftCenter, leftTop, leftBottom);
            var rIrisX = GetDistance(rightCenter, rightRight, rightLeft);
            var rIrisY = GetDistance(rightCenter, rightTop, rightBottom);

            var x = lIrisX < 0 ? Mathf.Min(lIrisX, rIrisX) : Mathf.Max(lIrisX, rIrisX);
            var y = lIrisY < 0 ? Mathf.Min(lIrisY, rIrisY) : Mathf.Max(lIrisY, rIrisY);

            (EyeLX.Value, EyeLY.Value) = (x, y);
            (EyeRX.Value, EyeRY.Value) = (x, y);
        }

        private float GetDistance(Vector3 center, Vector3 right, Vector3 left)
        {
            center.z = 0;
            right.z = 0;
            left.z = 0;

            var distanceTotal = Vector3.Distance(right, left);
            var distanceRight = Vector3.Distance(right, center);
            var distanceLeft = Vector3.Distance(center, left);

            var max = Mathf.Max(distanceRight, distanceLeft);
            var min = Mathf.Min(distanceRight, distanceLeft);

            var result = MathHelper.Normalize(max - min, 0, distanceTotal / 2, 0, 1);

            return distanceRight > distanceLeft ? -result : result;
        }

        private void UpdateMouth()
        {
            var right = _currentFaceMappings[FacePart.Mouth_Right_Inner];
            var left = _currentFaceMappings[FacePart.Mouth_Left_Inner];
            var top1 = _currentFaceMappings[FacePart.Mouth_Outer_Top1];
            var bottom1 = _currentFaceMappings[FacePart.Mouth_Outer_Bottom1];
            var top2 = _currentFaceMappings[FacePart.Mouth_Outer_Top2];
            var bottom2 = _currentFaceMappings[FacePart.Mouth_Outer_Bottom2];
            var top3 = _currentFaceMappings[FacePart.Mouth_Outer_Top3];
            var bottom3 = _currentFaceMappings[FacePart.Mouth_Outer_Bottom3];

            var ratio = GetDistanceRatio(new[] { right, left, right, left, right, left }, new[] { top1, bottom1, top2, bottom2, top3, bottom3 });

            MouthOpen.Value = MathHelper.Normalize(ratio, _minMouthRatio, _maxMouthRatio, 0, 1);
        }

        private float GetDistanceRatio(Vector3[] horizontalPoints, Vector3[] verticalPoints)
        {
            var hDistance = 0f;
            for (int i = 1; i < horizontalPoints.Length; i += 2)
            {
                var first = horizontalPoints[i - 1];
                var second = horizontalPoints[i];
                first.z = 0;
                second.z = 0;
                hDistance += Vector3.Distance(first, second);
            }

            var vDistance = 0f;
            for (int i = 1; i < verticalPoints.Length; i += 2)
            {
                var first = verticalPoints[i - 1];
                var second = verticalPoints[i];
                first.z = 0;
                second.z = 0;
                vDistance += Vector3.Distance(first, second);
            }

            return vDistance / hDistance;
        }

        private void UpdateEmotions()
        {
            var result = _emotionDetector.UpdateEmotions(_currentFaceLandmarkList);

            Neutral.Value = result[EmotionDetector.Emotions.Neutral];
            Happy.Value = result[EmotionDetector.Emotions.Happy];
            Supprised.Value = result[EmotionDetector.Emotions.Supprised];
            Frowning.Value = result[EmotionDetector.Emotions.Frowning];
        }

        #endregion

        #region Update Pose Params

        private void UpdateBodyAngle()
        {
            var shoulderDirection = _currentPoseMappings[PosePart.Shoulder_Right].point - _currentPoseMappings[PosePart.Shoulder_Left].point;
            var leftMiddle = _currentPoseMappings[PosePart.Shoulder_Left].point + (_currentPoseMappings[PosePart.Hip_Left].point - _currentPoseMappings[PosePart.Shoulder_Left].point) / 2;
            var rightMiddle = _currentPoseMappings[PosePart.Shoulder_Right].point + (_currentPoseMappings[PosePart.Hip_Right].point - _currentPoseMappings[PosePart.Shoulder_Right].point) / 2;

            var tilt = rightMiddle - leftMiddle;

            BodyAngleX.Value = Vector3.SignedAngle(new Vector3(shoulderDirection.x, 0, shoulderDirection.z), Vector3.right, Vector3.up);
            // BodyAngleY.Value = Vector3.SignedAngle(new Vector3(0, normalFace.y, normalFace.z), Vector3.forward, Vector3.left); // Should Set this via face params instead

            // Don't update tilt if twisted too far from camera
            if (BodyAngleX.Value > 75 || BodyAngleX.Value < -75) return;

            BodyAngleZ.Value = Vector3.SignedAngle(new Vector3(tilt.x, tilt.y, 0), Vector3.right, Vector3.forward);
        }

        private void UpdateArms()
        {
            var armL1Direction = _currentPoseMappings[PosePart.Shoulder_Left].point - _currentPoseMappings[PosePart.Elbow_Left].point;
            var angleL1 = Vector3.SignedAngle(new Vector3(armL1Direction.x, armL1Direction.y, 0), Vector3.up, Vector3.right);

            var armL2Direction = _currentPoseMappings[PosePart.Elbow_Left].point - _currentPoseMappings[PosePart.Wrist_Left].point;
            var angleL2 = Vector3.SignedAngle(new Vector3(armL2Direction.x, armL2Direction.y, 0), Vector3.up, Vector3.right);

            var armR1Direction = _currentPoseMappings[PosePart.Shoulder_Right].point - _currentPoseMappings[PosePart.Elbow_Right].point;
            var angleR1 = Vector3.SignedAngle(new Vector3(armR1Direction.x, armR1Direction.y, 0), Vector3.up, Vector3.right);

            var armR2Direction = _currentPoseMappings[PosePart.Elbow_Right].point - _currentPoseMappings[PosePart.Wrist_Right].point;
            var angleR2 = Vector3.SignedAngle(new Vector3(armR2Direction.x, armR2Direction.y, 0), Vector3.up, Vector3.right);

            ArmLAngle1.Value = angleL1;
            ArmLAngle2.Value = angleL2;

            ArmRAngle1.Value = angleR1;
            ArmRAngle2.Value = angleR2;
        }

        #endregion

        #region Update Hand Params

        private void UpdateLeftWristAngle()
        {
            WristLAngle.Value = UpdateWristAngle(_currentLeftHandMappings);
        }

        private void UpdateRightWristAngle()
        {
            WristRAngle.Value = UpdateWristAngle(_currentRightHandMappings);
        }

        private float UpdateWristAngle(Dictionary<HandPart, Vector3> handMappings)
        {
            var handDirection = handMappings[HandPart.Wrist] - handMappings[HandPart.Index_Mcp];
            return Vector3.SignedAngle(new Vector3(handDirection.x, handDirection.y, 0), Vector3.up, Vector3.right);
        }

        #endregion

        private void OnDestroy()
        {
            _isFaceStale = false;
        }

        //private float Normalize(float value, float min, float max, float normalizedMin, float normalizedMax)
        //{
        //    return ((value - min) / (max - min)) * (normalizedMax - normalizedMin) + normalizedMin;
        //}

        private bool IsTargetChanged<TValue>(TValue newTarget, TValue currentTarget)
        {
            // It's assumed that target has not changed if previous target and new target are both null.
            return currentTarget != null || newTarget != null;
        }

        #endregion
    }
}
