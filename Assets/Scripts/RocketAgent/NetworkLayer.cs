using System;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RocketAgent
{
    public class NetworkLayer
    {
        public int inputSize;
        public int outputSize;
        
        public Matrix<float> featureWeights;
        public Matrix<float> outputBias;

        public NetworkLayer(int inputSize, int outputSize)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;

            this.featureWeights = Matrix<float>.Build.Random(outputSize, inputSize);
            this.outputBias = Matrix<float>.Build.Random(outputSize, 1);
            
        }

        public NetworkLayer Clone()
        {
            NetworkLayer clonedLayer = new NetworkLayer(this.inputSize, this.outputSize);
            
            clonedLayer.featureWeights = this.featureWeights.Clone();
            clonedLayer.outputBias = this.outputBias.Clone();

            return clonedLayer;
        }
        public Matrix<float> GetOutput(Matrix<float> inputVector)
        {
            Matrix<float> output = this.featureWeights.Multiply(inputVector);
            output += this.outputBias;
            
            return ActivationFunction(output);
        }

        private Matrix<float> ActivationFunction(Matrix<float> inputVector)
        {
            return Sigmoid(inputVector);
        }
        
        private Matrix<float> Sigmoid(Matrix<float> inputVector)
        {
            return 1 / (1 + Matrix<float>.Exp(-inputVector));
        }

        public void UpdateWeightsFromBest(NetworkLayer p1, NetworkLayer p2, float exp)
        {
            void UpdateBiasWeights()
            {
                var p1Layer1D = p1.outputBias.ToRowMajorArray();
                var p2Layer2D = p2.outputBias.ToRowMajorArray();

                int length1D = p1Layer1D.Length;

                int cutIndex = 0;//Mathf.FloorToInt((0.4f * Random.value + 0.3f) * length1D);

                float[] newArray1 = new float[p1Layer1D.Length];

                Array.Copy(p1Layer1D, 0, newArray1, 0, cutIndex);
                Array.Copy(p2Layer2D, cutIndex, newArray1, cutIndex, length1D - cutIndex);

                this.outputBias = Matrix<float>.Build.DenseOfRowMajor(outputSize, 1, newArray1) +
                                  Matrix<float>.Round(Matrix<float>.Build.Random(outputSize, 1) * 0.90f)
                                      .PointwiseMultiply(Matrix<float>.Build.Random(outputSize, 1))*exp;
            }

            void UpdateFeaturesWeights()
            {
                var p1Layer1D = p1.featureWeights.ToRowMajorArray();
                var p2Layer2D = p2.featureWeights.ToRowMajorArray();

                int length1D = p1Layer1D.Length;

                int cutIndex = 0;//Mathf.FloorToInt((0.4f * Random.value + 0.3f) * length1D);

                float[] newArray1 = new float[p1Layer1D.Length];

                Array.Copy(p1Layer1D, 0, newArray1, 0, cutIndex);
                Array.Copy(p2Layer2D, cutIndex, newArray1, cutIndex, length1D - cutIndex);

                this.featureWeights = Matrix<float>.Build.DenseOfRowMajor(outputSize, inputSize, newArray1) +
                                      Matrix<float>.Round(Matrix<float>.Build.Random(outputSize, inputSize) * 0.90f)
                                          .PointwiseMultiply(Matrix<float>.Build.Random(outputSize, inputSize)) * exp;
             
            }

            UpdateFeaturesWeights();
            UpdateBiasWeights();
        }
    }
}