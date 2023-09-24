using UnityEngine;

namespace RocketAgent
{
    public class State
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 target;
        float distanceToTarget;
        
        public State(Rigidbody agentBody, Target targetTransform)
        {
            this.position = agentBody.position;
            this.rotation = agentBody.rotation.eulerAngles;
            this.velocity = agentBody.velocity;
            this.angularVelocity = agentBody.angularVelocity;
            this.target = targetTransform.position;
            this.distanceToTarget = Vector3.Distance(agentBody.position, targetTransform.position);
            
        }

        public float[,] VectorNotation()
        {
            // n features = 16;
            return new float[16, 1]
            {
                {this.position.x},
                {this.position.y},
                {this.position.z},

                {this.rotation.x},
                {this.rotation.y},
                {this.rotation.z},

                {this.velocity.x},
                {this.velocity.y},
                {this.velocity.z},

                {this.angularVelocity.x},
                {this.angularVelocity.y},
                {this.angularVelocity.z},

                {this.target.x},
                {this.target.y},
                {this.target.z},

                {this.distanceToTarget}
            };
        }
    }
}