using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;

    void Start()
    {
        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            target.position.x,
            target.position.y,
            -10f
        );

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}