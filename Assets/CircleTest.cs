
using UnityEditor;
using UnityEngine;

public class CircleTest : MonoBehaviour
{
    [SerializeField] private Transform input0; 
    [SerializeField] private Transform input1; 
    [SerializeField] private Transform input2;

    [SerializeField] private Transform result;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos() {
        
        Vector3 p0 = input0.position;
        Vector3 p1 = input2.position;

        Vector3 t0 = input1.position - p0;
        Vector3 t1 = p1 - input1.position;

        float detT = t0.x * t1.y - t0.y * t1.x;

        float p0_t0 = p0.x * t0.x + p0.y * t0.y;
        float p1_t1 = p1.x * t1.x + p1.y * t1.y;
        float result_x = (1 / detT) * (t1.y * p0_t0 - t0.y * p1_t1);
        float result_y = (1 / detT) * (-t1.x * p0_t0 + t0.x * p1_t1);

        result.position = new Vector3(result_x, result_y);
        
        Gizmos.color = Color.white;
        Gizmos.DrawLine(p0, p0 + t0);
        Gizmos.DrawLine(p1, p1 + t1);
        
        Gizmos.DrawLine(p0, result.position);
        Gizmos.DrawLine(p1, result.position);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(input0.position, input1.position);
        Gizmos.DrawLine(input1.position, input2.position);
        
        Handles.color = Color.green;
        Handles.DrawWireDisc(result.position, Vector3.forward, Vector3.Distance(result.position, p0));
        Handles.DrawWireDisc(result.position, Vector3.forward, Vector3.Distance(result.position, p1));
    }
}
