using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Unity.VisualScripting;

namespace RocketAgent
{
    public class Network
    {
        private int inputSize;
        private int outputSize;
        private int nLayers;
        public NetworkLayer[] layers;
        
        public Network(int inputSize, int outputSize, int nLayers)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            this.nLayers = nLayers;
            this.layers = new NetworkLayer[nLayers];

            for (int i = 0; i < nLayers-1; i++)
            {
                this.layers[i] = new NetworkLayer(inputSize, inputSize);
            }
            
            this.layers[nLayers-1] = new NetworkLayer(inputSize, outputSize);
        }

        public float[,] RunNetwork(float[,] inputData)
        {
            Matrix<float> outPut = Matrix<float>.Build.DenseOfArray(inputData);

            foreach (NetworkLayer networkLayer in layers)
            {
                outPut = networkLayer.GetOutput(outPut);
            }
            
            return outPut.Map((f => (f - 0.5f) * 2.0f)).ToArray();
        }
        
        public void UpdateWeightsRandom(Network p1, Network p2, float exp)
        {
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i].UpdateWeightsFromBest(p1.layers[i], p2.layers[i], exp);
            }
        }

        public Network Clone()
        {
            Network clonedNetwork = new Network(this.inputSize, this.outputSize, this.nLayers);

            NetworkLayer[] clonedLayers = new NetworkLayer[this.nLayers];

            for (int i = 0; i < this.nLayers; i++)
            {
                clonedLayers[i] = this.layers[i].Clone();
            }

            clonedNetwork.layers = clonedLayers;
            
            return clonedNetwork;
        }
    }
}