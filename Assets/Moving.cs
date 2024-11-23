using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class Moving : MonoBehaviour
{
    public Rigidbody rb;
    public PlayerInputActions playerControls;
    public float speed = 2f;
    public float rot;
    private InputAction move;
    private Vector3 moveDirection = Vector3.zero;
    private bool isMoving = false;
    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }
    private void OnEnable()
    {
        move = playerControls.Player.Move;
        move.Enable();
    }
    private void OnDisable()
    {
        move.Disable();
    }
    void Update()
    {
        Vector3 input = move.ReadValue<Vector3>();
        moveDirection = new Vector3(input.x, 0, input.z);
        bool currentlyMoving = moveDirection != Vector3.zero;
        if (currentlyMoving != isMoving)
        {
            isMoving = currentlyMoving;
        }
    }
    private void FixedUpdate()
    {
        Vector3 movement = moveDirection * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rot * Time.deltaTime);
        }
    }
}
