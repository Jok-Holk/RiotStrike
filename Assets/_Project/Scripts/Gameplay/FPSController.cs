using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _controller;
    private Animator _animator;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isCrouching;
    private float _targetHeight;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _targetHeight = standHeight;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleCrouch();
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

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            _isCrouching = !_isCrouching;
            _targetHeight = _isCrouching ? crouchHeight : standHeight;
            _animator?.SetBool("Crouch", _isCrouching);
        }
        _controller.height = Mathf.Lerp(_controller.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !_isCrouching && z > 0;

        float speed = _isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move * speed * Time.deltaTime);

        if (_animator == null) return;
        _animator.SetFloat("Speed", move.magnitude * speed);
        _animator.SetFloat("Forward", z);
        _animator.SetFloat("Strafe", x);
    }

    void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !_isGrounded || _isCrouching) return;
        _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        _animator?.ResetTrigger("Jump");
        _animator?.SetTrigger("Jump");
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    public void TriggerHit() => _animator?.SetTrigger("Hit");
    public void TriggerDeath() => _animator?.SetTrigger("Death");
    public void TriggerFire() => _animator?.SetTrigger("Fire");
    public void TriggerReload() => _animator?.SetTrigger("Reload");
    public void SetWeaponType(int type) => _animator?.SetInteger("WeaponType", type);
    public bool IsCrouching => _isCrouching;
    public bool IsRunning => Input.GetKey(KeyCode.LeftShift) && !_isCrouching;
}
