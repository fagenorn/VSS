using Assets.Scripts.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] private GameObject _modelPickerScreen;

        [SerializeField] private GameObject _dialogScreen;

        [SerializeField] private GameObject _settingsScreen;

        [SerializeField] private GameObject _settingsCameraScreen;

        [SerializeField] private GameObject _settingsParamScreen;

        [SerializeField] private GameObject _settingsAnimationScreen;

        private HistoryStack<Screen> _screenStack = new HistoryStack<Screen>(10);

        private Dictionary<Screen, GameObject[]> _screenDict;

        public static ScreenNavigator Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _screenDict = new Dictionary<Screen, GameObject[]>
            {
                { Screen.Model_Picker, new [] { _modelPickerScreen  }},
                { Screen.Dialog, new [] { _dialogScreen }},
                { Screen.Settings,  new [] { _settingsScreen }},
                { Screen.Main_All,  new [] { _modelPickerScreen, _settingsScreen }},
                { Screen.Settings_AllSub,  new [] { _settingsCameraScreen, _settingsParamScreen, _settingsAnimationScreen }},
                { Screen.Settings_Camera,  new [] { _settingsCameraScreen }},
                { Screen.Settings_Body_Params,  new [] { _settingsParamScreen }},
                { Screen.Settings_Animation_Params,  new [] { _settingsAnimationScreen }},
            };
        }

        public void HideScreen(Screen screen)
        {
            var screenObjs = _screenDict[screen];
            if (screenObjs == null) return;

            foreach (var obj in screenObjs)
            {
                obj.SetActive(false);
            }
        }

        public void HideScreen(int screenIndex)
        {
            HideScreen((Screen)screenIndex);
        }

        public void HideScreen(ScreenComponent screenComponenet)
        {
            HideScreen(screenComponenet.screen);
        }

        public void ShowScreen(Screen screen)
        {
            var screenObjs = _screenDict[screen];
            if (screenObjs == null) return;
            _screenStack.Push(screen);

            foreach (var obj in screenObjs)
            {
                obj.SetActive(true);
            }
        }

        public void ShowScreen(int screenIndex)
        {
            ShowScreen((Screen)screenIndex);
        }

        public void ShowScreen(ScreenComponent screenComponenet)
        {
            ShowScreen(screenComponenet.screen);
        }

        public void ShowPreviousScreen(int amount = 1)
        {
            var screen = Screen.Main;

            for (int i = 0; i < amount; i++)
            {
                if (_screenStack.Count == 0)
                {
                    break;
                }

                screen = _screenStack.Pop();
            }

            if (screen == Screen.Main)
            {
                HideScreen(Screen.Main_All);
            }
            else
            {
                ShowScreen(screen);
            }
        }

        public GameObject GetScreen(Screen screen)
        {
            return _screenDict[screen][0];
        }

        public GameObject GetScreen(int screenIndex)
        {
            return GetScreen((Screen)screenIndex);
        }

        public GameObject GetScreen(ScreenComponent screenComponenet)
        {
            return GetScreen(screenComponenet.screen);
        }
    }
}
