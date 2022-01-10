using Assets.Scripts.Common;
using Mediapipe;
using Mediapipe.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Logger = Mediapipe.Logger;
using CoordinateSystem = Mediapipe.Unity.CoordinateSystem;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProLiveCamera;

namespace Assets.Scripts.Mediapipe
{
    public class HolisticTracker : MonoBehaviour
    {
        public enum ModelComplexity
        {
            Lite = 0,
            Full = 1,
            Heavy = 2,
        }

        public enum DetectionType
        {
            FaceAndPoseAndHand,
            FaceAndPose,
            Face,
        }

        protected string TAG => GetType().Name;

        [SerializeField] private TextAsset _cpuConfig = null;
        [SerializeField] private long _timeoutMicrosec = 0;
        [SerializeField] private DetectionType _detectionType = DetectionType.FaceAndPose;

        public long timeoutMicrosec
        {
            get => _timeoutMicrosec;
            private set => _timeoutMicrosec = value;
        }

        public long timeoutMillisec => timeoutMicrosec / 1000;

        public bool refineFaceLandmarks = false;
        public ModelComplexity modelComplexity = ModelComplexity.Lite;
        public bool usePrevLandmarks = false;
        public bool smoothLandmarks = true;
        public float minDetectionConfidence = 0.5f;
        public float minTrackingConfidence = 0.5f;

        public RotationAngle rotation { get; private set; } = 0;

        private static readonly Common.GlobalInstanceTable<int, HolisticTracker> _InstanceTable = new Common.GlobalInstanceTable<int, HolisticTracker>(5);
        private static readonly Dictionary<IntPtr, int> _NameTable = new Dictionary<IntPtr, int>();

        private Stopwatch _stopwatch;
        protected CalculatorGraph calculatorGraph { get; private set; }
        protected Timestamp currentTimestamp;

        [HideInInspector] public UnityEvent<Detection> OnPoseDetectionOutput = new UnityEvent<Detection>();
        [HideInInspector] public UnityEvent<NormalizedLandmarkList> OnPoseLandmarksOutput = new UnityEvent<NormalizedLandmarkList>();
        [HideInInspector] public UnityEvent<NormalizedLandmarkList> OnFaceLandmarksOutput = new UnityEvent<NormalizedLandmarkList>();
        [HideInInspector] public UnityEvent<NormalizedLandmarkList> OnLeftHandLandmarksOutput = new UnityEvent<NormalizedLandmarkList>();
        [HideInInspector] public UnityEvent<NormalizedLandmarkList> OnRightHandLandmarksOutput = new UnityEvent<NormalizedLandmarkList>();
        [HideInInspector] public UnityEvent<LandmarkList> OnPoseWorldLandmarksOutput = new UnityEvent<LandmarkList>();
        [HideInInspector] public UnityEvent<NormalizedRect> OnPoseRoiOutput = new UnityEvent<NormalizedRect>();

        private const string _InputStreamName = "input_video";

        private const string _PoseDetectionStreamName = "pose_detection";
        private const string _PoseLandmarksStreamName = "pose_landmarks";
        private const string _FaceLandmarksStreamName = "face_landmarks";
        private const string _LeftHandLandmarksStreamName = "left_hand_landmarks";
        private const string _RightHandLandmarksStreamName = "right_hand_landmarks";
        private const string _PoseWorldLandmarksStreamName = "pose_world_landmarks";
        private const string _PoseRoiStreamName = "pose_landmarks_roi";

        private OutputStream<DetectionPacket, Detection> _poseDetectionStream;
        private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _poseLandmarksStream;
        private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _faceLandmarksStream;
        private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _leftHandLandmarksStream;
        private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> _rightHandLandmarksStream;
        private OutputStream<LandmarkListPacket, LandmarkList> _poseWorldLandmarksStream;
        private OutputStream<NormalizedRectPacket, NormalizedRect> _poseRoiStream;

        protected long prevPoseDetectionMicrosec = 0;
        protected long prevPoseLandmarksMicrosec = 0;
        protected long prevFaceLandmarksMicrosec = 0;
        protected long prevLeftHandLandmarksMicrosec = 0;
        protected long prevRightHandLandmarksMicrosec = 0;
        protected long prevPoseWorldLandmarksMicrosec = 0;
        protected long prevPoseRoiMicrosec = 0;

        public Status StartRun(AVProLiveCamera camera)
        {
            InitializeOutputStreams();

            switch (_detectionType)
            {
                case DetectionType.FaceAndPoseAndHand:
                    _leftHandLandmarksStream.AddListener(LeftHandLandmarksCallback, true).AssertOk();
                    _rightHandLandmarksStream.AddListener(RightHandLandmarksCallback, true).AssertOk();
                    _faceLandmarksStream.AddListener(FaceLandmarksCallback, true).AssertOk();
                    _poseWorldLandmarksStream.AddListener(PoseWorldLandmarksCallback, true).AssertOk();
                    break;
                case DetectionType.FaceAndPose:
                    _faceLandmarksStream.AddListener(FaceLandmarksCallback, true).AssertOk();
                    _poseWorldLandmarksStream.AddListener(PoseWorldLandmarksCallback, true).AssertOk();
                    break;
                case DetectionType.Face:
                    _faceLandmarksStream.AddListener(FaceLandmarksCallback, true).AssertOk();
                    break;
            }

            return calculatorGraph.StartRun(BuildSidePacket(camera));
        }

        private void Start()
        {
            _InstanceTable.Add(GetInstanceID(), this);
        }

        private void OnDestroy()
        {
            Stop();
        }

        public void SetTimeoutMicrosec(long timeoutMicrosec)
        {
            this.timeoutMicrosec = (long)Mathf.Max(0, timeoutMicrosec);
        }

        public void SetTimeoutMillisec(long timeoutMillisec)
        {
            SetTimeoutMicrosec(1000 * timeoutMillisec);
        }

        public virtual async UniTask Initialize()
        {
            InitializeCalculatorGraph().AssertOk();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            Logger.LogInfo(TAG, "Loading dependent assets...");
            var assetRequests = RequestDependentAssets();
            await UniTask.WaitWhile(() => assetRequests.Any((request) => request.keepWaiting));

            var errors = assetRequests.Where((request) => request.isError).Select((request) => request.error).ToList();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Logger.LogError(TAG, error);
                }

                throw new InternalException("Failed to prepare dependent assets");
            }
        }

        public void Stop()
        {
            if (calculatorGraph == null) { return; }

            using (var status = calculatorGraph.CloseAllPacketSources())
            {

                if (!status.Ok())
                {
                    Logger.LogError(TAG, status.ToString());
                }
            }

            using (var status = calculatorGraph.WaitUntilDone())
            {
                if (!status.Ok())
                {
                    Logger.LogError(TAG, status.ToString());
                }
            }

            var _ = _NameTable.Remove(calculatorGraph.mpPtr);
            calculatorGraph.Dispose();
            calculatorGraph = null;

            OnPoseDetectionOutput.RemoveAllListeners();
            OnPoseLandmarksOutput.RemoveAllListeners();
            OnFaceLandmarksOutput.RemoveAllListeners();
            OnLeftHandLandmarksOutput.RemoveAllListeners();
            OnRightHandLandmarksOutput.RemoveAllListeners();
            OnPoseWorldLandmarksOutput.RemoveAllListeners();
            OnPoseRoiOutput.RemoveAllListeners();
        }

        protected void InitializeOutputStreams()
        {
            _poseDetectionStream = new OutputStream<DetectionPacket, Detection>(calculatorGraph, _PoseDetectionStreamName);
            _poseLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, _PoseLandmarksStreamName);
            _faceLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, _FaceLandmarksStreamName);
            _leftHandLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, _LeftHandLandmarksStreamName);
            _rightHandLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(calculatorGraph, _RightHandLandmarksStreamName);
            _poseWorldLandmarksStream = new OutputStream<LandmarkListPacket, LandmarkList>(calculatorGraph, _PoseWorldLandmarksStreamName);
            _poseRoiStream = new OutputStream<NormalizedRectPacket, NormalizedRect>(calculatorGraph, _PoseRoiStreamName);
        }

        protected Status InitializeCalculatorGraph()
        {
            calculatorGraph = new CalculatorGraph();
            _NameTable.Add(calculatorGraph.mpPtr, GetInstanceID());

            // NOTE: There's a simpler way to initialize CalculatorGraph.
            //
            //     calculatorGraph = new CalculatorGraph(config.text);
            //
            //   However, if the config format is invalid, this code does not initialize CalculatorGraph and does not throw exceptions either.
            //   The problem is that if you call ObserveStreamOutput in this state, the program will crash.
            //   The following code is not very efficient, but it will return Non-OK status when an invalid configuration is given.
            try
            {
                var calculatorGraphConfig = GetCalculatorGraphConfig();

                //var calculatorName = "ThresholdingCalculator";
                //var extension = ThresholdingCalculatorOptions.Extensions.Ext;
                //var field = new ThresholdingCalculatorOptions { Threshold = minTrackingConfidence };
                //var node = calculatorGraphConfig.Node.LastOrDefault((node) => node.Calculator == calculatorName);
                //if (node != null) node.Options.SetExtension(extension, field);

                //var calculatorName2 = "TensorsToDetectionsCalculator";
                //var extension2 = TensorsToDetectionsCalculatorOptions.Extensions.Ext;
                // var field2 = new TensorsToDetectionsCalculatorOptions { Threshold = minTrackingConfidence };
                //var node2 = calculatorGraphConfig.Node.LastOrDefault((node) => node.Calculator == calculatorName2);
                // if (node2 != null) node2.Options.SetExtension(extension, field);

                var status = calculatorGraph.Initialize(calculatorGraphConfig);

                return status;
            }
            catch (Exception e)
            {
                return Status.FailedPrecondition(e.ToString());
            }
        }

        protected Common.WaitForResult WaitForAsset(string assetName, string uniqueKey, bool overwrite = false)
        {
            return new Common.WaitForResult(this, Common.AssetLoader.PrepareAssetAsync(assetName, uniqueKey, overwrite));
        }

        protected Common.WaitForResult WaitForAsset(string assetName, bool overwrite = false)
        {
            return WaitForAsset(assetName, assetName, overwrite);
        }

        private Common.WaitForResult WaitForPoseLandmarkModel()
        {
            return modelComplexity switch
            {
                ModelComplexity.Lite => WaitForAsset("pose_landmark_lite.bytes"),
                ModelComplexity.Full => WaitForAsset("pose_landmark_full.bytes"),
                ModelComplexity.Heavy => WaitForAsset("pose_landmark_heavy.bytes"),
                _ => throw new InternalException($"Invalid model complexity: {modelComplexity}"),
            };
        }

        public Status AddTextureFrameToInputStream(TextureFrame textureFrame)
        {
            return AddTextureFrameToInputStream(_InputStreamName, textureFrame);
        }

        public Status AddTextureFrameToInputStream(string streamName, TextureFrame textureFrame)
        {
            currentTimestamp = GetCurrentTimestamp();

            var imageFrame = textureFrame.BuildImageFrame();
            textureFrame.Release();

            return AddPacketToInputStream(streamName, new ImageFramePacket(imageFrame, currentTimestamp));
        }

        public Status AddPacketToInputStream<T>(string streamName, Packet<T> packet)
        {
            return calculatorGraph.AddPacketToInputStream(streamName, packet);
        }

        protected Timestamp GetCurrentTimestamp()
        {
            if (_stopwatch == null || !_stopwatch.IsRunning)
            {
                return Timestamp.Unset();
            }
            var microseconds = _stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);
            return new Timestamp(microseconds);
        }

        protected IList<Common.WaitForResult> RequestDependentAssets()
        {
            return new List<Common.WaitForResult>
            {
                WaitForAsset("face_detection_short_range.bytes"),
                WaitForAsset(refineFaceLandmarks ? "face_landmark_with_attention.bytes" : "face_landmark.bytes"),
                WaitForAsset("iris_landmark.bytes"),
                WaitForAsset("hand_landmark_full.bytes"),
                WaitForAsset("hand_recrop.bytes"),
                WaitForAsset("handedness.txt"),
                WaitForAsset("palm_detection_full.bytes"),
                WaitForAsset("pose_detection.bytes"),
                WaitForPoseLandmarkModel(),
            };
        }

        protected virtual CalculatorGraphConfig GetCalculatorGraphConfig()
        {
            return CalculatorGraphConfig.Parser.ParseFromTextFormat(_cpuConfig.text);
        }

        #region Callbacks

        protected bool TryGetPacketValue<T>(Packet<T> packet, ref long prevMicrosec, out T value) where T : class
        {
            long currentMicrosec = 0;
            using (var timestamp = packet.Timestamp())
            {
                currentMicrosec = timestamp.Microseconds();
            }

            if (!packet.IsEmpty())
            {
                prevMicrosec = currentMicrosec;
                value = packet.Get();
                return true;
            }

            value = null;
            return currentMicrosec - prevMicrosec > _timeoutMicrosec;
        }

        protected static bool TryGetGraphRunner(IntPtr graphPtr, out HolisticTracker graphRunner)
        {
            var isInstanceIdFound = _NameTable.TryGetValue(graphPtr, out var instanceId);

            if (isInstanceIdFound)
            {
                return _InstanceTable.TryGetValue(instanceId, out graphRunner);
            }

            graphRunner = null;
            return false;
        }

        protected static Status InvokeIfGraphRunnerFound<T>(IntPtr graphPtr, IntPtr packetPtr, Action<T, IntPtr> action) where T : HolisticTracker
        {
            try
            {
                var isFound = TryGetGraphRunner(graphPtr, out var graphRunner);
                if (!isFound)
                {
                    return Status.FailedPrecondition("Graph runner is not found");
                }
                var graph = (T)graphRunner;
                action(graph, packetPtr);
                return Status.Ok();
            }
            catch (Exception e)
            {
                return Status.FailedPrecondition(e.ToString());
            }
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr PoseDetectionCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new DetectionPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevPoseDetectionMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnPoseDetectionOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }


        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr PoseLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new NormalizedLandmarkListPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevPoseLandmarksMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnPoseLandmarksOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr FaceLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new NormalizedLandmarkListPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevFaceLandmarksMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnFaceLandmarksOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr LeftHandLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new NormalizedLandmarkListPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevLeftHandLandmarksMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnLeftHandLandmarksOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr RightHandLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new NormalizedLandmarkListPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevRightHandLandmarksMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnRightHandLandmarksOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr PoseWorldLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new LandmarkListPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevPoseWorldLandmarksMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnPoseWorldLandmarksOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
        private static IntPtr PoseRoiCallback(IntPtr graphPtr, IntPtr packetPtr)
        {
            return InvokeIfGraphRunnerFound<HolisticTracker>(graphPtr, packetPtr, (holisticTrackingGraph, ptr) =>
            {
                using (var packet = new NormalizedRectPacket(ptr, false))
                {
                    if (holisticTrackingGraph.TryGetPacketValue(packet, ref holisticTrackingGraph.prevPoseRoiMicrosec, out var value))
                    {
                        holisticTrackingGraph.OnPoseRoiOutput.Invoke(value);
                    }
                }
            }).mpPtr;
        }

        #endregion

        protected void SetImageTransformationOptions(SidePacket sidePacket, AVProLiveCamera camera, bool expectedToBeMirrored = false)
        {
            // NOTE: The origin is left-bottom corner in Unity, and right-top corner in MediaPipe.
            var rotation = RotationAngle.Rotation0.Reverse();
            var inputRotation = rotation;
            var isInverted = CoordinateSystem.ImageCoordinate.IsInverted(rotation);
            var shouldBeMirrored = camera._flipX ^ expectedToBeMirrored;
            var inputHorizontallyFlipped = isInverted ^ shouldBeMirrored;
            var inputVerticallyFlipped = !isInverted;

            if ((inputHorizontallyFlipped && inputVerticallyFlipped) || rotation == RotationAngle.Rotation180)
            {
                inputRotation = inputRotation.Add(RotationAngle.Rotation180);
                inputHorizontallyFlipped = !inputHorizontallyFlipped;
                inputVerticallyFlipped = !inputVerticallyFlipped;
            }

            Logger.LogDebug($"input_rotation = {inputRotation}, input_horizontally_flipped = {inputHorizontallyFlipped}, input_vertically_flipped = {inputVerticallyFlipped}");

            sidePacket.Emplace("input_rotation", new IntPacket((int)inputRotation));
            sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(inputHorizontallyFlipped));
            sidePacket.Emplace("input_vertically_flipped", new BoolPacket(inputVerticallyFlipped));
        }

        private SidePacket BuildSidePacket(AVProLiveCamera camera)
        {
            var sidePacket = new SidePacket();

            SetImageTransformationOptions(sidePacket, camera);
            sidePacket.Emplace("refine_face_landmarks", new BoolPacket(refineFaceLandmarks));
            sidePacket.Emplace("model_complexity", new IntPacket((int)modelComplexity));
            sidePacket.Emplace("smooth_landmarks", new BoolPacket(smoothLandmarks));
            sidePacket.Emplace("use_prev_landmarks", new BoolPacket(usePrevLandmarks));

            // sidePacket.Emplace("min_detection_confidence", new FloatPacket(0.75f));

            return sidePacket;
        }
    }
}