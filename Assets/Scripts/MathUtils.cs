namespace DefaultNamespace
{
    public static class MathUtils {
        
        public static float[] Linspace(float start, float stop, int num = 50, bool endpoint = true) { 
            
            if (num <= 0) {
                return new float[0];
            }

            if (num == 1) {
                return new float[] { start };
            }

            float[] samples = new float[num];
            float step;

            if (endpoint) {
                step = (stop - start) / (num - 1);
                for (int i = 0; i < num; i++)
                {
                    samples[i] = start + step * i;
                }
            }
            else {
                step = (stop - start) / num;
                for (int i = 0; i < num; i++)
                {
                    samples[i] = start + step * i;
                }
            }

            return samples;
        }
        
        
    }
}