using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    public bool isAssigned = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = isAssigned ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}
