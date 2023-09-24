using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.Common;
using UnityEngine;

namespace DefaultNamespace.TrackGenerator
{
    public class AnimateAlongTrack
    {
        private static MonoBehaviour _corRunner;
        private static MonoBehaviour CorRunner
        {
            get
            {
                if (_corRunner == null)
                {
                    _corRunner = new GameObject("AnimationRunner").AddComponent<CoroutineRunner>();
                }

                return _corRunner;
            }
        }

        private Coroutine animationLoop;
        private float speed;
        
        public void StartAnimation(Transform updateObject, List<Vector3> track, float animationSpeed)
        {
            speed = animationSpeed;
            animationLoop = CorRunner.StartCoroutine(AnimationLoop(updateObject, track));
        }

        private IEnumerator AnimationLoop(Transform updateObject, List<Vector3> track)
        {
          
            int nPoints = track.Count;
            float currentTime = Random.Range(0.00001f, nPoints- 1.0f);
            
            while (true)
            {
                
                int start = Mathf.FloorToInt(currentTime);
                int end =  Mathf.CeilToInt(currentTime);

                float lerpT = currentTime - start;

                Vector3 startP = track[start % nPoints];
                Vector3 endP = track[end % nPoints];
                
                float derMag = Mathf.Max(0.01f, (endP-startP).magnitude);
                
                float lambda = 0.25f;
                
                Vector3 der = (endP - startP) * ((1f-lerpT) * (1.0f - lambda)) ;
                
                for (int i = 1; i < 6; i++)
                {
                    Vector3 s = track[(start + i) % nPoints];
                    Vector3 e = track[(end + i) % nPoints];

                    float weight = (1.0f - lambda) * Mathf.Pow(lambda, (i));
                    der += (e-s).normalized * weight;
                    
                }
                
                updateObject.position =  Vector3.Lerp(startP, endP, lerpT);
                
                Quaternion desiredRotation = Quaternion.LookRotation(der, Vector3.up);

                Vector3 derRot = (updateObject.rotation.eulerAngles - desiredRotation.eulerAngles);
                float derRotMag = derRot.magnitude;

                float tValue = Mathf.Min(0.025f, derRotMag / 100f);

                updateObject.rotation = Quaternion.Lerp(updateObject.rotation, desiredRotation, tValue);

                float finalSpeed = ((Mathf.Exp(speed) - 1.0f) / (Mathf.Exp(1.0f) - 1.0f))*5.5f;
                
                currentTime += Time.deltaTime * (finalSpeed / derMag);

                if (currentTime >= nPoints)
                {
                    currentTime = 0.0f;
                }
                
                yield return null;
            }
        }
        
        public void SetSpeed(float animationSpeed)
        {
            this.speed = animationSpeed;
        }

        private void StopAnimation()
        {
            if (animationLoop != null)
            {
                CorRunner.StopCoroutine(animationLoop);
            }
        }

        
    }
}