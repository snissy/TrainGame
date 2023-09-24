using System;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

namespace RocketAgent
{
    public class RocketAgent : MonoBehaviour
    {
        [SerializeField]
        private Transform startTransform;
        
        [SerializeField] 
        private Rigidbody body;

        [SerializeField] 
        private float maxForceSize = 30f;
        
        public Network network;
        
        private Target target;

        private float totalEpisodReward;
        private float totalTime;

        // This could be separate data structure
        
        private int nFeatures = 16;
        private int nOutputs = 3;
        
        
        public void SetTarget(Target newTarget)
        {
            this.target = newTarget;
        }
        public void SetStartPosition(Transform startTransform)
        {
            this.startTransform = startTransform;
        }
        
        public void FirstStart()
        {
            network = new Network(nFeatures, nOutputs, 3);
            SetStartConfiguration();
        }
        
        public Experiment.ExperimentResult GetResult()
        {
            return new Experiment.ExperimentResult(this.totalEpisodReward, this.network);
        }

        private void SetStartConfiguration()
        {
            totalEpisodReward = 0.0f;
            totalTime = 0.0f;
            this.body.velocity = Vector3.zero;
            this.body.angularVelocity = Vector3.zero;
            this.body.position = this.startTransform.position;
            this.body.rotation = this.startTransform.rotation;
        }
        
        private void FixedUpdate()
        {
            State s = new State(this.body, this.target);
            Action(s);
            float reward = FrameReward();
            totalEpisodReward += reward;
        }

        public void ResetTrial(Network p1, Network p2, float exp)
        {
            UpdateWeightsRandom(p1, p2, exp);
            SetStartConfiguration();
        }
        
        public void ResetTrial(Network p1)
        {
            this.network = p1.Clone();
            SetStartConfiguration();
        }
        
        private void UpdateWeightsRandom(Network p1, Network p2, float exp)
        {
            this.network.UpdateWeightsRandom(p1, p2, exp);
        }
        
        private void Action(State s)
        {
            float[,] floatState = s.VectorNotation();
            float[,] outPutVector = network.RunNetwork(floatState);

            Vector3 outputForce = new Vector3(outPutVector[0, 0], outPutVector[1, 0], outPutVector[2, 0])*maxForceSize;
            body.AddForce(outputForce);
        }

        private float FrameReward()
        {
            return -1.0f * DistanceToTarget();
        }

        private float DistanceToTarget()
        {
            return Vector3.Distance(this.transform.position, target.position);
        }

        public float RandomValueMinusOnePlusOne()
        {
            return (Random.value - 0.5f)*2.0f;
        }

        public float Sigmoid(float x)
        {
            // I don't remember the correct final steps for the activation function but I will do this little hack
            // This is not correct. 
            float baseSigmoidValue = (1.0f / (1.0f + MathF.Exp(-x)));
            float minusOneToPlusOne = (baseSigmoidValue - 0.5f) * 2.0f;
            
            return minusOneToPlusOne;
        }
        
    }
}