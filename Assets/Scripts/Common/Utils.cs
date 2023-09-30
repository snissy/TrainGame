using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Common
{
    public static class Utils
    {
        private static int seed = 0;
        private static Random _random = new(seed);
        
        public static T Choice<T>(List<T> list) {
            int randomInt = _random.Next(0, list.Count - 1);
            return list[randomInt];
        }
        
        public static Color ColorFromRange(this Gradient drawColor, int t, int start, int max) {
            int rangeLenght = Mathf.Abs(max - start);
            int distToStart = Mathf.Abs(t - start);
            return drawColor.Evaluate(distToStart/(float)rangeLenght);
        }
        
        public class LinSpace {
            public float[] array;
            public float step;
            public int numberItems;
            
            public LinSpace(float start, float stop, int num = 10) {
                numberItems = num;
                
                if (num <= 0) {
                    array = Array.Empty<float>();
                    step = 0.0f;
                    Debug.LogWarning("Empty linspace");
                }

                if (num == 1) {
                    array = new[] { start };
                    step = 0;
                }

                array = new float[num];
                step = (stop - start) / (num - 1);

                for (int i = 0; i < num; i++) {
                    array[i] = start + i * step;
                }
        
                array[num - 1] = stop; // Ensure the last value is exactly the stop value
            }
        }
    }
}
