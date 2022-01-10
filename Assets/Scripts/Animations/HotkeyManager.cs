using Cysharp.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using UnityRawInput;

namespace Assets.Scripts.Animations
{
    public class HotkeyManager : MonoBehaviour
    {
        private bool _isGettingCombo = false;

        [SerializeField] private int _maxComboTimeout = 500;

        private Stopwatch _stopwatch = new Stopwatch();

        private KeyCombo _currentCombo = new KeyCombo();

        public AsyncReactiveProperty<KeyCombo> HotKey = new AsyncReactiveProperty<KeyCombo>(default);

        public static HotkeyManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Init();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Init()
        {

#if UNITY_EDITOR
            RawKeyInput.Start(false);
#else
            RawKeyInput.Start(true);
#endif

            RawKeyInput.OnKeyDown += RawKeyInput_OnKeyDown;

            _stopwatch.Start();
        }

        private void OnApplicationQuit()
        {
            RawKeyInput.Stop();
        }

        private void OnDestroy()
        {
            RawKeyInput.Stop();
        }

        private void RawKeyInput_OnKeyDown(RawKey obj)
        {
            var time = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            _isGettingCombo = true;

            if (time > _maxComboTimeout)
            {
                _currentCombo.Reset();
            }

            _currentCombo.SetNextCombo(obj);
        }

        private void Update()
        {
            if (_stopwatch.ElapsedMilliseconds > _maxComboTimeout)
            {
                _isGettingCombo = false;
            }

            if (_isGettingCombo) return;

            if (_currentCombo.HasCombo())
            {
                HotKey.Value = _currentCombo;
                _currentCombo.Reset();
            }
        }

        public struct KeyCombo
        {
            public RawKey? Key1;

            public RawKey? Key2;

            public RawKey? Key3;

            public override bool Equals(object obj) => obj is KeyCombo other && this.Equals(other);

            public bool Equals(KeyCombo p) => Key2 != null && p.Key2 != null && Key1 == p.Key1 && Key2 == p.Key2 && Key3 == p.Key3;

            public override int GetHashCode() => (Key1, Key2, Key3).GetHashCode();

            public static bool operator ==(KeyCombo lhs, KeyCombo rhs) => lhs.Equals(rhs);

            public static bool operator !=(KeyCombo lhs, KeyCombo rhs) => !(lhs == rhs);

            public override string ToString()
            {
                if (Key3 != null) return $"{Key1} + {Key2} + {Key3}";
                if (Key2 != null) return $"{Key1} + {Key2}";
                return "-";
            }

            public void SetNextCombo(RawKey key)
            {
                if (Key1 == null)
                {
                    Key1 = key;
                    return;
                }

                if (Key2 == null)
                {
                    Key2 = key;
                    return;
                }

                Key3 = key;
            }

            public bool HasCombo() { return Key2 != null; }

            public void Reset()
            {
                Key1 = null;
                Key2 = null;
                Key3 = null;
            }
        }
    }
}
