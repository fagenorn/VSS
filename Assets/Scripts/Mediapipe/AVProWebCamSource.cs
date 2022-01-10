using Cysharp.Threading.Tasks;
using Mediapipe.Unity;
using RenderHeads.Media.AVProLiveCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ImageSource = Assets.Scripts.Common.ImageSource;

namespace Assets.Scripts.Mediapipe
{
    public class AVProWebCamSource : ImageSource
    {
        [SerializeField] private bool _updateDeviceSettings = false;

        private List<AVProLiveCameraDevice> _availableDevices = new List<AVProLiveCameraDevice>();

        private AVProLiveCameraDevice _selectedDevice;

        public override SourceType type => SourceType.Camera;

        public override string sourceName => _selectedDevice?.Name;

        public override string[] sourceCandidateNames => _availableDevices.Select(device => device.Name).ToArray();

        public override bool isPrepared => _selectedDevice != null;

        public override bool isPlaying => _selectedDevice.IsRunning;

        public override int textureWidth => _selectedDevice.CurrentWidth;

        public override int textureHeight => _selectedDevice.CurrentHeight;

        public override double frameRate => _selectedDevice.CurrentFrameRate;

        public override bool isVerticallyFlipped { get => _selectedDevice.FlipX; set => _selectedDevice.FlipX = value; }

        public override bool isHorizontallyFlipped { get => _selectedDevice.FlipY; set => _selectedDevice.FlipY = value; }

        public override RotationAngle rotation => RotationAngle.Rotation0;

        public override Texture GetCurrentTexture()
        {
            return _selectedDevice.OutputTexture;
        }

        public override void Pause()
        {
            _selectedDevice.Pause();
        }

        public override async UniTask Play()
        {
            _selectedDevice.Start();
            //_selectedDevice.Update(false);
            //_selectedDevice.Render();
            await WaitForTextureAsync();
        }

        public override async UniTask Resume()
        {
            _selectedDevice.Play();
            await WaitForTextureAsync();
        }

        public override void SelectSource(int sourceId)
        {
            if (sourceId < 0 || sourceId >= _availableDevices.Count)
            {
                throw new ArgumentException($"Invalid source ID: {sourceId}");
            }

            _selectedDevice = _availableDevices[sourceId];
        }

        public override void Stop()
        {
            _selectedDevice.Close();
        }

        private void Start()
        {
            // EnumerateDevices(true);
            UpdateCameras();
        }

        private void EnumerateDevices(bool logDevices)
        {
            // Enumerate all cameras
            int numDevices = AVProLiveCameraManager.Instance.NumDevices;
            if (logDevices)
            {
                print("num devices: " + numDevices);
            }
            for (int i = 0; i < numDevices; i++)
            {
                AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);

                if (logDevices)
                {
                    // Enumerate video inputs (only for devices with multiple analog input sources, eg TV cards)
                    print("device " + i + ": " + device.Name + " (" + device.GUID + ") has " + device.NumVideoInputs + " videoInputs");
                    for (int j = 0; j < device.NumVideoInputs; j++)
                    {
                        print("  videoInput " + j + ": " + device.GetVideoInputName(j));
                    }

                    // Enumerate camera modes
                    print("device " + i + ": " + device.Name + " (" + device.GUID + ") has " + device.NumModes + " modes");
                    for (int j = 0; j < device.NumModes; j++)
                    {
                        AVProLiveCameraDeviceMode mode = device.GetMode(j);
                        print("  mode " + j + ": " + mode.Width + "x" + mode.Height + " @" + mode.FPS.ToString("F2") + "fps [" + mode.Format + "]");
                    }

                    // Enumerate camera settings
                    print("device " + i + ": " + device.Name + " (" + device.GUID + ") has " + device.NumSettings + " video settings");
                    for (int j = 0; j < device.NumSettings; j++)
                    {
                        AVProLiveCameraSettingBase settingBase = device.GetVideoSettingByIndex(j);
                        switch (settingBase.DataTypeValue)
                        {
                            case AVProLiveCameraSettingBase.DataType.Boolean:
                                {
                                    AVProLiveCameraSettingBoolean settingBool = (AVProLiveCameraSettingBoolean)settingBase;
                                    print(string.Format("  setting {0}: {1}({2}) value:{3} default:{4} canAuto:{5} isAuto:{6}", j, settingBase.Name, settingBase.PropertyIndex, settingBool.CurrentValue, settingBool.DefaultValue, settingBase.CanAutomatic, settingBase.IsAutomatic));
                                }
                                break;
                            case AVProLiveCameraSettingBase.DataType.Float:
                                {
                                    AVProLiveCameraSettingFloat settingFloat = (AVProLiveCameraSettingFloat)settingBase;
                                    print(string.Format("  setting {0}: {1}({2}) value:{3} default:{4} range:{5}-{6} canAuto:{7} isAuto:{8}", j, settingBase.Name, settingBase.PropertyIndex, settingFloat.CurrentValue, settingFloat.DefaultValue, settingFloat.MinValue, settingFloat.MaxValue, settingBase.CanAutomatic, settingBase.IsAutomatic));
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void UpdateCameras()
        {
            //GL.IssuePluginEvent(AVProLiveCameraPlugin.GetRenderEventFunc(), AVProLiveCameraPlugin.PluginID | (int)AVProLiveCameraPlugin.PluginEvent.UpdateAllTextures);

            _availableDevices.Clear();

            // Update all cameras
            int numDevices = AVProLiveCameraManager.Instance.NumDevices;
            for (int i = 0; i < numDevices; i++)
            {
                AVProLiveCameraDevice device = AVProLiveCameraManager.Instance.GetDevice(i);

                //device.UpdateHotSwap = AVProLiveCameraManager.Instance._supportHotSwapping;
                //device.UpdateFrameRates = true;
                //device.UpdateSettings = _updateDeviceSettings;

                //// Update the actual image
                //device.Update(false);
                //device.Render();

                _availableDevices.Add(device);
            }
        }

        private async UniTask WaitForTextureAsync()
        {
            const int timeoutFrame = 500;
            var count = 0;
            await UniTask.WaitUntil(() => count++ > timeoutFrame || (_selectedDevice != null && _selectedDevice.OutputTexture != null));

            if (_selectedDevice.OutputTexture == null)
            {
                throw new TimeoutException("Failed to start WebCam");
            }
        }
    }
}
