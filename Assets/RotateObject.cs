using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 5f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed);
    }
}
