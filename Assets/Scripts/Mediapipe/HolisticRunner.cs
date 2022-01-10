using Assets.Scripts.Barracuda;
using Assets.Scripts.BodyControllers;
using Assets.Scripts.BodyParameters;
using Cysharp.Threading.Tasks;
using Mediapipe;
using Mediapipe.Unity;
using RenderHeads.Media.AVProLiveCamera;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static RenderHeads.Media.AVProLiveCamera.AVProLiveCameraPlugin;
using Logger = Mediapipe.Logger;

namespace Assets.Scripts.Mediapipe
{
    public class HolisticRunner : MonoBehaviour
    {
        protected virtual string TAG => GetType().Name;

        protected bool isPaused;

        [SerializeField] private FaceDetector _fd;

        [SerializeField] private Bootstrap bootstrap;

        [SerializeField] private HolisticTracker _graphRunner;

        [SerializeField] private TextureFramePool _textureFramePool;

        [SerializeField] private AVProLiveCamera _camera;

        [SerializeField] private BodyTracker _bodyTracker;

        private CancellationTokenSource _cts;

        public HolisticTracker.ModelComplexity modelComplexity
        {
            get => _graphRunner.modelComplexity;
            set => _graphRunner.modelComplexity = value;
        }

        public bool smoothLandmarks
        {
            get => _graphRunner.smoothLandmarks;
            set => _graphRunner.smoothLandmarks = value;
        }

        public bool refineFaceLandmarks
        {
            get => _graphRunner.refineFaceLandmarks;
            set => _graphRunner.refineFaceLandmarks = value;
        }

        public long timeoutMillisec
        {
            get => _graphRunner.timeoutMillisec;
            set => _graphRunner.SetTimeoutMillisec(value);
        }

        private async UniTask Start()
        {
            if (bootstrap == null)
            {
                Logger.LogError(TAG, "Bootstrap is not found.");
                return;
            }

            await UniTask.WaitUntil(() => bootstrap.isFinished);
        }

        /// <summary>
        ///   Start the main program from the beginning.
        /// </summary>
        public virtual void Play()
        {
            if (_cts != null)
            {
                Stop();
            }

            isPaused = false;

            _cts = new CancellationTokenSource();
            Run(_cts.Token).Forget();
        }

        /// <summary>
        ///   Stops the main program.
        /// </summary>
        public virtual void Stop()
        {
            isPaused = true;
            _cts.Cancel();
            if (_camera.Device != null) _camera.Device.Stop();
            _graphRunner.Stop();
        }

        private async UniTaskVoid Run(CancellationToken ct)
        {
            _camera.Begin();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(500);

            await UniTask.WaitUntil(() => _camera.OutputTexture != null, cancellationToken: cts.Token);

            //if (!imageSource.isPrepared)
            //{
            //    Logger.LogError(TAG, "Failed to start ImageSource, exiting...");

            //    return;
            //}
            // NOTE: The _screen will be resized later, keeping the aspect ratio.
            // _screen.Initialize(imageSource);

            // _worldAnnotationArea.localEulerAngles = imageSource.rotation.Reverse().GetEulerAngles();

            Logger.LogInfo(TAG, $"Model Complexity = {modelComplexity}");
            Logger.LogInfo(TAG, $"Smooth Landmarks = {smoothLandmarks}");
            Logger.LogInfo(TAG, $"Refine Face Landmarks = {refineFaceLandmarks}");
            Logger.LogInfo(TAG, $"Timeout Millisec = {timeoutMillisec}");

            try
            {
                await _graphRunner.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError(TAG, ex);

                return;
            }

            _graphRunner.OnPoseDetectionOutput.AddListener(OnPoseDetectionOutput);
            _graphRunner.OnFaceLandmarksOutput.AddListener(OnFaceLandmarksOutput);
            _graphRunner.OnPoseLandmarksOutput.AddListener(OnPoseLandmarksOutput);
            _graphRunner.OnLeftHandLandmarksOutput.AddListener(OnLeftHandLandmarksOutput);
            _graphRunner.OnRightHandLandmarksOutput.AddListener(OnRightHandLandmarksOutput);
            _graphRunner.OnPoseWorldLandmarksOutput.AddListener(OnPoseWorldLandmarksOutput);
            _graphRunner.OnPoseRoiOutput.AddListener(OnPoseRoiOutput);

            _graphRunner.StartRun(_camera).AssertOk();

            TextureFormat textureFormat = TextureFormat.RGBA32;

            //if (Enum.TryParse<VideoFrameFormat>(_camera.Device.CurrentFormat, out var result))
            //{
            //    textureFormat = result switch
            //    {
            //        VideoFrameFormat.MPEG => TextureFormat.RGBA32,
            //        VideoFrameFormat.RAW_BGRA32 => TextureFormat.BGRA32,
            //        VideoFrameFormat.YUV_422_YUY2 => TextureFormat.YUY2,
            //        VideoFrameFormat.YUV_422_UYVY => TextureFormat.YUY2,
            //        VideoFrameFormat.YUV_422_YVYU => TextureFormat.YUY2,
            //        VideoFrameFormat.YUV_422_HDYC => TextureFormat.YUY2,
            //        VideoFrameFormat.YUV_420_PLANAR_YV12 => TextureFormat.YUY2,
            //        VideoFrameFormat.YUV_420_PLANAR_I420 => TextureFormat.YUY2,
            //        VideoFrameFormat.RAW_RGB24 => TextureFormat.RGBA32,
            //        VideoFrameFormat.RAW_MONO8 => TextureFormat.RGBA32,
            //        VideoFrameFormat.RGB_10BPP => TextureFormat.RGBA32,
            //        VideoFrameFormat.YUV_10BPP_V210 => TextureFormat.YUY2,
            //        _ => TextureFormat.RGBA32,
            //    };
            //}

            // Use RGBA32 as the input format.
            // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
            _textureFramePool.ResizeTexture(_camera.Device.CurrentWidth, _camera.Device.CurrentHeight, textureFormat);

            _bodyTracker.SetImageSourceInfo(_camera);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.WaitWhile(() => isPaused);

                    var textureFrame = await _textureFramePool.GetTextureFrameAsync(ct);
                    if (textureFrame == null) continue;

                    // Copy current image to TextureFrame
                    ReadFromImageSource(_camera.OutputTexture, textureFrame);
                    if (ct.IsCancellationRequested) break;
                    _graphRunner.AddTextureFrameToInputStream(textureFrame).AssertOk();

                    await UniTask.WaitForEndOfFrame(ct);
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(TAG, ex);

                Stop();
                Play();
            }
        }


        protected static void SetupLive2DController(Live2DController controller, Common.ImageSource imageSource, bool expectedToBeMirrored = false)
        {
            controller.IsMirrored = expectedToBeMirrored ^ imageSource.isHorizontallyFlipped;
            controller.RotationAngle = imageSource.rotation.Reverse();
        }

        protected static void SetupAnnotationController<T>(AnnotationController<T> annotationController, Common.ImageSource imageSource, bool expectedToBeMirrored = false) where T : HierarchicalAnnotation
        {
            annotationController.isMirrored = expectedToBeMirrored ^ imageSource.isHorizontallyFlipped;
            annotationController.rotationAngle = imageSource.rotation.Reverse();
        }

        protected static void ReadFromImageSource(Texture texture, TextureFrame textureFrame)
        {
            var sourceTexture = texture;

            // For some reason, when the image is coiped on GPU, latency tends to be high.
            // So even when OpenGL ES is available, use CPU to copy images.
            var textureType = sourceTexture.GetType();

            if (textureType == typeof(WebCamTexture))
            {
                textureFrame.ReadTextureFromOnCPU((WebCamTexture)sourceTexture);
            }
            else if (textureType == typeof(Texture2D))
            {
                textureFrame.ReadTextureFromOnCPU((Texture2D)sourceTexture);
            }
            else
            {
                textureFrame.ReadTextureFromOnCPU(sourceTexture);
            }
        }

        private void OnPoseDetectionOutput(Detection poseDetection)
        {
        }

        private void OnFaceLandmarksOutput(NormalizedLandmarkList faceLandmarks)
        {
            _bodyTracker.UpdateFaceLandmarkList(faceLandmarks);
        }

        private void OnPoseLandmarksOutput(NormalizedLandmarkList poseLandmarks)
        {
        }

        private void OnLeftHandLandmarksOutput(NormalizedLandmarkList leftHandLandmarks)
        {
            _bodyTracker.UpdateLeftLandmarkList(leftHandLandmarks);
        }

        private void OnRightHandLandmarksOutput(NormalizedLandmarkList rightHandLandmarks)
        {
            _bodyTracker.UpdateRightLandmarkList(rightHandLandmarks);
        }

        private void OnPoseWorldLandmarksOutput(LandmarkList poseWorldLandmarks)
        {
            _bodyTracker.UpdatePoseWorldLandmarkList(poseWorldLandmarks);
        }

        private void OnPoseRoiOutput(NormalizedRect roiFromLandmarks)
        {
        }
    }
}
