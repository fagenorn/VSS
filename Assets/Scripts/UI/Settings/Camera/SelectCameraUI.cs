using Assets.Scripts.Mediapipe;
using Assets.Scripts.UI.Components;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RenderHeads.Media.AVProLiveCamera;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Settings.Camera
{
    public class SelectCameraUI : MonoBehaviour
    {
        [SerializeField] private AVProLiveCamera _camera;

        [SerializeField] private Button _selectCamera;

        [SerializeField] private Button _startCamera;

        [SerializeField] private Button _stopCamera;

        [SerializeField] private TMP_Text _selectedCamera;

        [SerializeField] private TMP_Text _statusCamera;

        [SerializeField] private HolisticRunner _runner;

        private void Start()
        {
            _selectedCamera.text = _camera.Device.Name ?? "N/A";
            _statusCamera.text = "Idle";
            _startCamera.interactable = true;
            _stopCamera.interactable = false;

            _selectCamera.OnClickAsAsyncEnumerable().Subscribe(SelectCamera);
            _startCamera.OnClickAsAsyncEnumerable().Subscribe(StartRunner);
            _stopCamera.OnClickAsAsyncEnumerable().Subscribe(StopRunner);
        }

        private async UniTaskVoid SelectCamera(AsyncUnit unit)
        {
            var devicesAvailable = AVProLiveCameraManager.Instance.NumDevices;
            var listDevices = new Dictionary<string, int>();
            for (int i = 0; i < devicesAvailable; i++)
            {
                var device = AVProLiveCameraManager.Instance.GetDevice(i);
                listDevices[device.Name] = i;
            }

            var selected = await DialogManager.Instance.ShowItemsAsync(listDevices.Keys, "Select A Camera");

            if(selected == null || !listDevices.TryGetValue(selected, out var selectedIndex)) return;
            _camera._deviceSelection = AVProLiveCamera.SelectDeviceBy.Index;
            _camera._desiredDeviceIndex = selectedIndex;
            _selectedCamera.text = selected;
        }

        private void StartRunner(AsyncUnit unit)
        {
            _statusCamera.text = "Tracking";
            _selectCamera.interactable = false;
            _startCamera.interactable = false;
            _stopCamera.interactable = true;
            _runner.Play();
        }

        private void StopRunner(AsyncUnit unit)
        {
            _statusCamera.text = "Idle";
            _selectCamera.interactable = true;
            _startCamera.interactable = true;
            _stopCamera.interactable = false;
            _runner.Stop();
        }
    }
}
