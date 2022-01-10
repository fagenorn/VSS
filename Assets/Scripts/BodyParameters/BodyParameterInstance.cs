using System.Diagnostics;

namespace Assets.Scripts.BodyParameters
{
    public class BodyParameterInstance
    {
        private float _value = 0.0f;

        private Stopwatch _stopwatch = new Stopwatch();

        private const long _maxElapsedMilliseconds = 200;

        public BodyParameterInstance(float defaultMin, float defaultMax)
        {
            DefaultMin = defaultMin;
            DefaultMax = defaultMax;

            _stopwatch.Start();
        }

        public float DefaultMin { get; } = 0.0f;

        public float DefaultMax { get; } = 1.0f;

        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                _stopwatch.Restart();
            }
        }

        public override string ToString()
        {
            return Value.ToString("0.00");
        }

        public bool IsTracking()
        {
            return _stopwatch.ElapsedMilliseconds < _maxElapsedMilliseconds;
        }
    }
}
