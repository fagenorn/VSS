using System.Collections.Generic;

namespace Assets.Scripts.Live2D
{
    public interface ILive2DValueProvider
    {
        /// <summary>
        /// Lower means higher priority.
        /// </summary>
        public int Priority { get; }

        public bool Enabled { get; }

        /// <summary>
        /// Sets the values of the model for the provided live2d ids.
        /// </summary>
        /// <param name="live2dIds"></param>
        /// <returns>Whether was able to set the parameter</returns>
        public bool TrySetValues(string live2DId);

        public void EndLoop();
    }
}
