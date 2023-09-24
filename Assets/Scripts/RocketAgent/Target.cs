
using UnityEngine;
using Random = UnityEngine.Random;

public class Target : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform RunnerStart;
    
    private float circleAngle = 0.0f;
    private float rdAngle = 0.0f;
    private Vector3 velocity = Vector3.zero;
    public Vector3 position
    {
        get => this.transform.position;
        set => this.transform.position = value;
    }
    
    public void SetRandomPosition()
    {
        Vector3 rdCircle = Random.insideUnitSphere.normalized * 75f;
        this.position = RunnerStart.position + rdCircle;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(this.transform.position, 5.25f);
    }
}
