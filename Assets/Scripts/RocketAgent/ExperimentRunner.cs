using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace RocketAgent
{
    public class ExperimentRunner : MonoBehaviour
    {
       
        [Header("Component References")]
        public Target target;
        
        private List<Experiment> allExperiments = new List<Experiment>();
        private List<Experiment.ExperimentResult> topResults = new List<Experiment.ExperimentResult>();
        
        private int nExperimentFrames;
        private int maxNFrams = 100*30;
        private Experiment.ExperimentResult bestResult = new Experiment.ExperimentResult(float.MinValue, null);

        private float nTest = 0.0f;

        private void Start()
        {
            foreach (Transform childTransform in this.transform)
            {
                allExperiments.Add(childTransform.GetComponent<Experiment>());
            }

            nTest = 0.0f;
        }

        private void FixedUpdate()
        {
            nExperimentFrames++;

            if (nExperimentFrames > maxNFrams)
            {
                float preTime = Time.timeScale;
                
                Physics.autoSimulation = false;
                Time.timeScale = 0.0f;
                
                    ResetExperiments();
                
                Time.timeScale = preTime;
                Physics.autoSimulation = true;
                
            }
            
        }

        private void ResetExperiments()
        {
            nTest += 1.0f;

            int nTop = 5;
            
            var allResults = allExperiments.Select(experiment => experiment.GetExperimentResult()).OrderBy(result => -result.totalReward).ToArray();
            topResults = new List<Experiment.ExperimentResult>(allResults.Take(nTop).Select(result => result.Clone()));

            SoftMax sampler = new SoftMax(topResults.Select(result => result.totalReward /1e6f).ToArray());
            
            float expValue = (0.0005f + Mathf.Exp(-nTest * 0.002f)) * 0.75f;
            Debug.Log($"New value with rdFactor {expValue}");
            
            foreach (Experiment.ExperimentResult experimentResult in topResults)
            {
                Debug.Log($"The best result was {experimentResult.totalReward}");
            }
            
            
            for (int i = 0; i < allResults.Length; i++){
                Experiment experiment = allExperiments[i];

                int p1I = sampler.Sample();
                int p2I = sampler.Sample();
                
                while (p1I == p2I)
                {
                    p2I = sampler.Sample();
                }
                

                Experiment.ExperimentResult p1 = topResults[p1I];
                Experiment.ExperimentResult p2 = topResults[p2I];
                
                experiment.ResetTrial(p1, p2, expValue);
            }

            for (int i = 1; i < nTop; i++){
                Experiment experiment = allExperiments[^i];
                experiment.ResetTrial(topResults[i].network);
            }
            
            nExperimentFrames = 0;
            
            target.SetRandomPosition();
            
        }

        class SoftMax
        {
            private readonly float[] _probabilities;
            public SoftMax(float[] weights)
            {
                float[] expWeights = new float[weights.Length];
                float sum = 0.0f;
                
                for (int i = 0; i < weights.Length; i++){
                    expWeights[i] = Mathf.Exp(weights[i]);
                    sum += expWeights[i];
                }
                
                _probabilities = new float[weights.Length];
                
                for (int i = 0; i < weights.Length; i++){
                    _probabilities[i] = expWeights[i] / sum;
                }
                
            }

            public int Sample()
            {
                float r = Random.value;
                float cumulativeProbability = 0;
                
                for (int i = 0; i < _probabilities.Length; i++){

                    cumulativeProbability += _probabilities[i];
                    
                    if (r < cumulativeProbability)
                    {
                        return i;
                    }
                }
                
                return -1;
            }
        }
    }
}