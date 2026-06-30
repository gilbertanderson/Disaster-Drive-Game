using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
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
        
    }
}
