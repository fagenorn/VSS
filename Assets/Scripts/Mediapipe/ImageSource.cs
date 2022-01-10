using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Mediapipe.Unity;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Common
{
    public abstract class ImageSource : MonoBehaviour
    {
        [Serializable]
        public struct ResolutionStruct
        {
            public int width;
            public int height;
            public double frameRate;

            public ResolutionStruct(int width, int height, double frameRate)
            {
                this.width = width;
                this.height = height;
                this.frameRate = frameRate;
            }

            public ResolutionStruct(Resolution resolution)
            {
                width = resolution.width;
                height = resolution.height;
                frameRate = resolution.refreshRate;
            }

            public Resolution ToResolution()
            {
                return new Resolution() { width = width, height = height, refreshRate = (int)frameRate };
            }

            public override string ToString()
            {
                var aspectRatio = $"{width}x{height}";
                var frameRateStr = frameRate.ToString("#.##");
                return frameRate > 0 ? $"{aspectRatio} ({frameRateStr}Hz)" : aspectRatio;
            }
        }

        public enum SourceType
        {
            Camera = 0,
            Image = 1,
            Video = 2,
        }

        /// <remarks>
        ///   To call this method, the image source must be prepared.
        /// </remarks>
        /// <returns>
        ///   <see cref="TextureFormat" /> that is compatible with the current texture.
        /// </returns>
        public TextureFormat textureFormat => isPrepared ? TextureFormatFor(GetCurrentTexture()) : throw new InvalidOperationException("ImageSource is not prepared");

        public abstract int textureWidth { get;}

        public abstract int textureHeight { get; }

        /// <remarks>
        ///   If <see cref="type" /> does not support frame rate, it returns zero.
        /// </remarks>
        public abstract double frameRate { get;}

        public float focalLengthPx { get; } = 2.0f; // TODO: calculate at runtime

        public abstract bool isHorizontallyFlipped { get; set; }

        public abstract bool isVerticallyFlipped { get; set; }

        public abstract RotationAngle rotation { get; }

        public abstract SourceType type { get; }

        public abstract string sourceName { get; }

        public abstract string[] sourceCandidateNames { get; }

        /// <remarks>
        ///   Once <see cref="Play" /> finishes successfully, it will become true.
        /// </remarks>
        /// <returns>
        ///   Returns if the image source is prepared.
        /// </returns>
        public abstract bool isPrepared { get; }

        public abstract bool isPlaying { get; }

        /// <summary>
        ///   Choose the source from <see cref="sourceCandidateNames" />.
        /// </summary>
        /// <remarks>
        ///   You need to call <see cref="Play" /> for the change to take effect.
        /// </remarks>
        /// <param name="sourceId">The index of <see cref="sourceCandidateNames" /></param>
        public abstract void SelectSource(int sourceId);

        /// <summary>
        ///   Prepare image source.
        ///   If <see cref="isPlaying" /> is true, it will reset the image source.
        /// </summary>
        /// <remarks>
        ///   When it finishes successfully, <see cref="isPrepared" /> will return true.
        /// </remarks>
        /// <exception cref="InvalidOperation" />
        public abstract UniTask Play();

        /// <summary>
        ///   Resume image source.
        ///   If <see cref="isPlaying" /> is true, it will do nothing.
        /// </summary>
        /// <exception cref="InvalidOperation">
        ///   The image source has not been played.
        /// </exception>
        public abstract UniTask Resume();

        /// <summary>
        ///   Pause image source.
        ///   If <see cref="isPlaying" /> is false, it will do nothing.
        /// </summary>
        public abstract void Pause();

        /// <summary>
        ///   Stop image source.
        /// </summary>
        /// <remarks>
        ///   When it finishes successfully, <see cref="isPrepared" /> will return false.
        /// </remarks>
        public abstract void Stop();

        /// <remarks>
        ///   To call this method, the image source must be prepared.
        /// </remarks>
        /// <returns>
        ///   <see cref="Texture" /> that contains the current image.
        /// </returns>
        public abstract Texture GetCurrentTexture();

        protected static TextureFormat TextureFormatFor(Texture texture)
        {
            return GraphicsFormatUtility.GetTextureFormat(texture.graphicsFormat);
        }
    }
}
