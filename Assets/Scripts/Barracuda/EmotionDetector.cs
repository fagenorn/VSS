using Cysharp.Threading.Tasks;
using Mediapipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

namespace Assets.Scripts.Barracuda
{
    public class EmotionDetector : MonoBehaviour
    {
        public enum Emotions
        {
            Neutral = 0,
            Happy = 1,
            Supprised = 2,
            Frowning = 3,
        }

        [SerializeField] public NNModel _model;

        private const int _batch = 3;

        private int _currentBatch;

        private Model _runtimeModel;

        private IWorker _worker;

        private Dictionary<Emotions, float> _currentEmotionsMappings = new Dictionary<Emotions, float>
        {
            { Emotions.Neutral, 1f },
            { Emotions.Happy, 0f },
            { Emotions.Supprised, 0f },
            { Emotions.Frowning, 0f },
        };

        private float[] _currentLandmarksVectorised = new float[1872 * _batch];

        private void Start()
        {
            _runtimeModel = ModelLoader.Load(_model, verbose: false);
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel, verbose: false);
        }

        private void OnDestroy()
        {
            _worker?.Dispose();
        }

        public Dictionary<Emotions, float> UpdateEmotions(IList<NormalizedLandmark> landmarks)
        {
            if (_currentBatch == _batch)
            {
                var input = GetTensor();
                _worker.Execute(input);
                var output = _worker.CopyOutput();
                var result = output.ToReadOnlyArray();

                var count = _currentEmotionsMappings.Count;
                for (int i = 0; i < count; i++)
                {
                    _currentEmotionsMappings[(Emotions)i] = result.Where((x, index) => index % count == i).Average();
                }

                input.Dispose();
                output.Dispose();
                _currentBatch = 0;
            }
            else
            {
                PrepareTensor(_currentBatch, landmarks);
                _currentBatch++;
            }

            return _currentEmotionsMappings;
        }

        private Tensor GetTensor()
        {
            return new Tensor(_batch, 1872, _currentLandmarksVectorised);
        }

        private void PrepareTensor(int batch, IList<NormalizedLandmark> landmarks)
        {
            var xMean = 0f;
            var yMean = 0f;

            foreach (var item in landmarks.Take(468))
            {
                var x = item.X;
                var y = item.Y;
                xMean += x;
                yMean += y;
            }

            xMean /= 468;
            yMean /= 468;
            var mean = new Vector2(xMean, yMean);

            var index = batch * 1872;
            foreach (var item in landmarks.Take(468))
            {
                var x = item.X - xMean;
                var y = item.Y - yMean;
                var w = item.X;
                var z = item.Y;

                var coor = new Vector2(w, z);
                var diff = coor - mean;
                var dist = Mathf.Sqrt(Vector2.Dot(diff, diff));
                var angle = (Mathf.Atan2(y, x) * 360) / (2 * Mathf.PI);

                _currentLandmarksVectorised[index] = w;
                _currentLandmarksVectorised[index + 1] = z;
                _currentLandmarksVectorised[index + 2] = dist;
                _currentLandmarksVectorised[index + 3] = angle;

                index += 4;
            }
        }
    }
}
