using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    public InputAction movementAction;
    private Rigidbody playerRb;
    private Vector2 movementInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        movementAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        movementInput = movementAction.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;

        Vector3 movementDirection = new Vector3(horizontalInput, 0f, verticalInput);
        playerRb.AddForce(movementDirection * speed);
    }
}
