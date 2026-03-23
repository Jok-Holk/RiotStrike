using Fusion;
using UnityEngine;

public class FPSCamera : NetworkBehaviour
{
    [SerializeField] private float sensitivity = 1f;

    private float _xRotation, _yRotation;
    private bool _hasAuthority, _initialized;
    private Transform _bodyTransform;

    public float Yaw => _yRotation;
    public float Pitch => _xRotation;

    public void Initialize(bool hasInputAuthority)
    {
        _hasAuthority = hasInputAuthority;
        _bodyTransform = transform.parent.parent;
        _initialized = true;

        var cam = GetComponent<Camera>();
        var audio = GetComponent<AudioListener>();

        if (_hasAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _xRotation = 0f;
            _yRotation = _bodyTransform != null ? _bodyTransform.eulerAngles.y : 0f;
            if (cam) cam.enabled = true;
            if (audio) audio.enabled = true;
        }
        else
        {
            if (cam) cam.enabled = false;
            if (audio) audio.enabled = false;
        }
    }

    // Gọi từ Update() của InputProvider hoặc tự gọi ở đây
    void Update()
    {
        if (!_hasAuthority || !_initialized) return;

        // Raw delta pixel từ OS, không bị Unity smooth hay clamp
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        _yRotation += mouseX;

        // Apply visual ngay lập tức mỗi frame
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        if (_bodyTransform != null)
            _bodyTransform.rotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    // Giữ để FPSController lấy Yaw gửi vào NetworkInputData
    public void ApplyInput(NetworkInputData input, float deltaTime) { }
    public void ApplyVisual() { }
}