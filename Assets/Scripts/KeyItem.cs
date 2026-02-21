using UnityEngine;

public class KeyItem : MonoBehaviour
{
    private Transform followTarget; // 要跟随的目标（玩家）
    public Vector3 offset = new Vector3(0.8f, 0.8f, 0); // 悬浮在玩家右上角
    public float smoothSpeed = 5f; // 跟随的平滑度
    private bool isPickedUp = false;

    void Update()
    {
        if (isPickedUp && followTarget != null)
        {
            // 计算目标位置（玩家位置 + 偏移量）
            Vector3 targetPosition = followTarget.position + offset;
            // 平滑移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }

    // 被玩家调用的拾取方法
    public void PickUp(Transform playerTransform)
    {
        followTarget = playerTransform;
        isPickedUp = true;

        // 关键：拾取后关闭碰撞体，防止钥匙挡住子弹或触发怪物
        GetComponent<Collider2D>().enabled = false;
    }
}