using UnityEngine;

public class EdgeScrollCamera : MonoBehaviour
{
    [Header("设置")]
    public float scrollSpeed = 20f;
    public float edgeSize = 20f;
    public bool enableEdgeScrolling = true;

    public bool invertAxis;

    Vector3 direction;

    void Update()
    {
        if (!enableEdgeScrolling) return;

        Vector3 mousePos = Input.mousePosition;
        Vector3 moveDirection = Vector3.zero;

        if (mousePos.x < edgeSize)
            moveDirection.x = -1;
        else if (mousePos.x > Screen.width - edgeSize)
            moveDirection.x = 1;

        if (mousePos.y < edgeSize)
            moveDirection.z = -1;
        else if (mousePos.y > Screen.height - edgeSize)
            moveDirection.z = 1;

        if (invertAxis)
        {
            direction = moveDirection.normalized;
        }
        else
        {
            direction = -moveDirection.normalized;
        }

        transform.Translate(direction * scrollSpeed * Time.deltaTime, Space.World);
    }
}