using Assets.Scripts.Common;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Logger = Mediapipe.Logger;

namespace Assets.Scripts.Mediapipe
{
    public class TextureFramePool : MonoBehaviour
    {
        private const string _TAG = nameof(TextureFramePool);

        [SerializeField] private int _poolSize = 10;

        private readonly object _formatLock = new object();
        private int _textureWidth = 0;
        private int _textureHeight = 0;
        private TextureFormat _format = TextureFormat.RGBA32;

        private Queue<TextureFrame> _availableTextureFrames;
        /// <remarks>
        ///   key: TextureFrame's instance ID
        /// </remarks>
        private Dictionary<Guid, TextureFrame> _textureFramesInUse;

        /// <returns>
        ///   The total number of texture frames in the pool.
        /// </returns>
        public int frameCount
        {
            get
            {
                var availableTextureFramesCount = _availableTextureFrames == null ? 0 : _availableTextureFrames.Count;
                var textureFramesInUseCount = _textureFramesInUse == null ? 0 : _textureFramesInUse.Count;

                return availableTextureFramesCount + textureFramesInUseCount;
            }
        }

        private void Start()
        {
            _availableTextureFrames = new Queue<TextureFrame>(_poolSize);
            _textureFramesInUse = new Dictionary<Guid, TextureFrame>();
        }

        private void OnDestroy()
        {
            lock (((ICollection)_availableTextureFrames).SyncRoot)
            {
                _availableTextureFrames.Clear();
                _availableTextureFrames = null;
            }

            lock (((ICollection)_textureFramesInUse).SyncRoot)
            {
                foreach (var textureFrame in _textureFramesInUse.Values)
                {
                    textureFrame.OnRelease.RemoveListener(OnTextureFrameRelease);
                }
                _textureFramesInUse.Clear();
                _textureFramesInUse = null;
            }
        }

        public void ResizeTexture(int textureWidth, int textureHeight, TextureFormat format)
        {
            lock (_formatLock)
            {
                _textureWidth = textureWidth;
                _textureHeight = textureHeight;
                _format = format;
            }
        }

        public void ResizeTexture(int textureWidth, int textureHeight)
        {
            ResizeTexture(textureWidth, textureHeight, _format);
        }

        private void OnTextureFrameRelease(TextureFrame textureFrame)
        {
            lock (((ICollection)_textureFramesInUse).SyncRoot)
            {
                if (!_textureFramesInUse.Remove(textureFrame.GetInstanceID()))
                {
                    // won't be run
                    Logger.LogWarning(_TAG, "The released texture does not belong to the pool");
                    return;
                }

                if (frameCount > _poolSize || IsStale(textureFrame))
                {
                    return;
                }
                _availableTextureFrames.Enqueue(textureFrame);
            }
        }

        private bool IsStale(TextureFrame textureFrame)
        {
            lock (_formatLock)
            {
                return textureFrame.width != _textureWidth || textureFrame.height != _textureHeight;
            }
        }

        private TextureFrame CreateNewTextureFrame()
        {
            var textureFrame = new TextureFrame(_textureWidth, _textureHeight, _format);
            textureFrame.OnRelease.AddListener(OnTextureFrameRelease);

            return textureFrame;
        }

        public async UniTask<TextureFrame> GetTextureFrameAsync(CancellationToken ct)
        {
            TextureFrame nextFrame = null;


            await UniTask.WaitUntil(() => _poolSize > frameCount || _availableTextureFrames.Count > 0, cancellationToken: ct);
            if(ct.IsCancellationRequested || _availableTextureFrames == null) return nextFrame;

            lock (((ICollection)_availableTextureFrames).SyncRoot)
            {
                if (_poolSize <= frameCount)
                {
                    while (_availableTextureFrames.Count > 0)
                    {
                        var textureFrame = _availableTextureFrames.Dequeue();

                        if (!IsStale(textureFrame))
                        {
                            nextFrame = textureFrame;
                            break;
                        }
                    }
                }

                if (nextFrame == null)
                {
                    nextFrame = CreateNewTextureFrame();
                }
            }

            lock (((ICollection)_textureFramesInUse).SyncRoot)
            {
                _textureFramesInUse.Add(nextFrame.GetInstanceID(), nextFrame);
            }

            nextFrame.WaitUntilReleased();

            return nextFrame;
        }
    }

}
