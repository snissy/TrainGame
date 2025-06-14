using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts
{
    public class TemperatureFunctions
    {
        private static float delta = 1e-16f;

        static float PolynomSlope(float x, float il)
        {
            if (il < delta)
                return 0f; // Avoid division by zero

            float value = Mathf.Max(0f, (il - x) / il);
            return Mathf.Pow(value, 3);
        }

        static float NormalizedSin(float x, float fv)
        {
            // starts at 1
            return (Mathf.Sin(x * fv + Mathf.PI / 2f) + 1f) * 0.5f;
        }

        internal static float TempSchedule(int iteration, float nIterations, float startTemp)
        {
            float x = iteration;
            return startTemp * PolynomSlope(x, nIterations);
        }

        internal static float KeepPropbabilty(float oldVale, float newValue, float temp)
        {
            if (newValue < oldVale)
                return 1f;

            if (temp < delta)
                return 0f;

            return Mathf.Exp(-(newValue - oldVale) / temp);
        }
    }
}