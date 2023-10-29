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
    }
}
