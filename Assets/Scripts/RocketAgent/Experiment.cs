using System;
using Unity.VisualScripting;
using UnityEngine;

namespace RocketAgent
{
    public class Experiment : MonoBehaviour
    {
        public struct ExperimentResult
        {
            public float totalReward;
            public Network network;

            public ExperimentResult(float totalReward, Network network)
            {
                this.totalReward = totalReward;
                this.network = network;
            }

            public ExperimentResult Clone()
            {
                ExperimentResult cloneResult = new ExperimentResult();
                cloneResult.totalReward = this.totalReward;
                cloneResult.network = this.network.Clone();
                
                return cloneResult;
            }
        }
        
        public RocketAgent rocketAgent;
        public Target target;
        public Transform startPosition;

        private void Start()
        {
            rocketAgent.SetStartPosition(this.startPosition);
            rocketAgent.SetTarget(this.target);
            rocketAgent.FirstStart();
        }

        public ExperimentResult GetExperimentResult()
        {
            return this.rocketAgent.GetResult();
        }

        public void ResetTrial(ExperimentResult p1, ExperimentResult p2, float exp)
        {
            rocketAgent.ResetTrial(p1.network, p2.network, exp);
        }

        public void ResetTrial(Network network)
        {
            rocketAgent.ResetTrial(network);
        }
    }
}