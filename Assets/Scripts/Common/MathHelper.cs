namespace Assets.Scripts.Common
{
    public static class MathHelper
    {
        public static float Normalize(float value, float min, float max, float normalizedMin, float normalizedMax)
        {
            return ((value - min) / (max - min) * (normalizedMax - normalizedMin)) + normalizedMin;
        }
    }
}
