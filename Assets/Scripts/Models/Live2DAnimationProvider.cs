using static Assets.Scripts.Animations.HotkeyManager;
using Newtonsoft.Json;

namespace Assets.Scripts.Live2D
{
    using J = JsonPropertyAttribute;

    public enum AnimationType
    {
        OneShot = 0,
        Looping = 1,
        Hold = 2,
        PingPong = 3,
    }

    public partial class Live2DAnimationProvider
    {
        private KeyCombo keyCombo;
        private string motionId;
        private AnimationType animationType;
        private bool hasHotkey;

        [J("_name")] public string Name { get; set; }

        [J("_enabled")] public bool Enabled { get; set; }

        [J("_priority")] public int Priority { get; set; }

        [J("_hasCombo")]
        public bool HasHotkey
        {
            get => hasHotkey; set
            {
                hasHotkey = value;
                TypeUpdated();
            }
        }

        [J("_motionId")]
        public string MotionId
        {
            get => motionId;
            set
            {
                motionId = value;
                AnimationUpdated();
            }
        }

        [J("_keyCombo")]
        public KeyCombo KeyCombo
        {
            get => keyCombo;
            set
            {
                keyCombo = value;
                HotkeyUpdated();
            }
        }

        [J("_type")]
        public AnimationType AnimationType
        {
            get => animationType;
            set
            {
                animationType = value;
                TypeUpdated();
            }
        }
    }
}
