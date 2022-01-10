using Mediapipe;

namespace Assets.Scripts.Mediapipe
{
    internal class OutputStream<TPacket, TValue> where TPacket : Packet<TValue>, new()
    {
        private readonly CalculatorGraph _calculatorGraph;

        private readonly string _streamName;
        private OutputStreamPoller<TValue> _poller;
        private TPacket _outputPacket;

        private string _presenceStreamName;
        private OutputStreamPoller<bool> _presencePoller;
        private BoolPacket _presencePacket;

        private bool canFreeze => _presenceStreamName != null;

        public OutputStream(CalculatorGraph calculatorGraph, string streamName)
        {
            _calculatorGraph = calculatorGraph;
            _streamName = streamName;
        }

        public Status AddListener(CalculatorGraph.NativePacketCallback callback, bool observeTimestampBounds = false)
        {
            return _calculatorGraph.ObserveOutputStream(_streamName, callback, observeTimestampBounds);
        }

        public bool TryGetNext(out TValue value)
        {
            if (HasNextValue())
            {
                value = _outputPacket.Get();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetLatest(out TValue value)
        {
            if (HasNextValue())
            {
                var queueSize = _poller.QueueSize();

                // Assume that queue size will not be reduced from another thread.
                while (queueSize-- > 0)
                {
                    if (!Next())
                    {
                        value = default;
                        return false;
                    }
                }
                value = _outputPacket.Get();
                return true;
            }
            value = default;
            return false;
        }

        private bool HasNextValue()
        {
            if (canFreeze)
            {
                if (!NextPresence() || _presencePacket.IsEmpty() || !_presencePacket.Get())
                {
                    // NOTE: IsEmpty() should always return false
                    return false;
                }
            }
            return Next() && !_outputPacket.IsEmpty();
        }

        private bool NextPresence()
        {
            return Next(_presencePoller, _presencePacket, _presenceStreamName);
        }

        private bool Next()
        {
            return Next(_poller, _outputPacket, _streamName);
        }

        private static bool Next<T>(OutputStreamPoller<T> poller, Packet<T> packet, string streamName)
        {
            if (!poller.Next(packet))
            {
                Logger.LogWarning($"Failed to get next value from {streamName}, so there may be errors inside the calculatorGraph. See logs for more details");
                return false;
            }
            return true;
        }
    }
}
