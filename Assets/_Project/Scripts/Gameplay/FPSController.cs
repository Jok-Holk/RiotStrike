using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _controller;
    private Animator _animator;
    private Vector3 _velocity;
    private bool _isGrounded;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void HandleGroundCheck()
    {
        _isGrounded = Physics.CheckSphere(
            groundCheck ? groundCheck.position : transform.position,
            groundDistance,
            groundMask
        );

        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? crouchSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move * speed * Time.deltaTime);

        if (_animator == null) return;
        _animator.SetFloat("Speed", move.magnitude * speed);
        _animator.SetFloat("Forward", z);
        _animator.SetFloat("Strafe", x);
    }

    void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !_isGrounded) return;

        _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        if (_animator == null) return;
        _animator.ResetTrigger("Jump");
        _animator.SetTrigger("Jump");
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
