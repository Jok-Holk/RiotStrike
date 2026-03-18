using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;

    public float speed = 6f;
    public float slowSpeed = 3f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;

    public float jumpCooldown = 1.5f;
    private float lastJumpTime;

    Vector3 velocity;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Input WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? slowSpeed : speed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Animation parameters
        if (animator != null)
        {
            float moveSpeed = move.magnitude * currentSpeed;
            animator.SetFloat("Speed", moveSpeed);
            animator.SetFloat("Forward", z);
            animator.SetFloat("Strafe", x);
        }

        // Nhảy với cooldown
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= lastJumpTime + jumpCooldown)
        {
            velocity.y = jumpForce;
            lastJumpTime = Time.time;

            if (animator != null)
            {
                animator.ResetTrigger("Jump"); // reset trước để tránh lỗi spam
                animator.SetTrigger("Jump");   // kích hoạt animation nhảy
            }
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
