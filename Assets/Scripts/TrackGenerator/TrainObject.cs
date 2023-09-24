using System;
using UnityEngine;

namespace DefaultNamespace.TrackGenerator
{
    public class TrainObject:MonoBehaviour
    {
        [Header("Component references")]
        public TrackGenerator trackGenerator;

        [Header("Settings")] 
        [Range(0.00f, 1f)] 
        public float animationSpeed;
        
        private AnimateAlongTrack animationController;

        private void Start()
        {
            animationController = new AnimateAlongTrack();
            animationController.StartAnimation(this.transform, trackGenerator.GeneratePoints(), animationSpeed);
        }

        public void Update()
        {
            animationController.SetSpeed(this.animationSpeed);
        }
    }
}