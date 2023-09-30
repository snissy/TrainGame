using System;
using UnityEngine;

namespace Common
{
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