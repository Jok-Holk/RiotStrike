using Fusion;
using UnityEngine;

public class FPSCamera : NetworkBehaviour
{
    [SerializeField] private float sensitivity = 1f;

    private float _xRotation, _yRotation;
    private bool _hasAuthority, _initialized;
    private Transform _bodyTransform;
    private Camera _cam;
    private float  _defaultFOV = 60f;

    public float Yaw   => _yRotation;
    public float Pitch => _xRotation;

    public void Initialize(bool hasInputAuthority)
    {
        _hasAuthority = hasInputAuthority;
        // Tìm FPSController qua component thay vì dùng parent.parent (fragile với hierarchy depth)
        var fps = GetComponentInParent<FPSController>();
        _bodyTransform = fps != null ? fps.transform : transform.parent?.parent;
        _initialized   = true;

        _cam  = GetComponent<Camera>();
        var audio = GetComponent<AudioListener>();

        if (_hasAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _xRotation = 0f;
            _yRotation = _bodyTransform != null ? _bodyTransform.eulerAngles.y : 0f;

            if (_cam)
            {
                _cam.enabled       = true;
                _cam.nearClipPlane = 0.01f;
                _defaultFOV        = _cam.fieldOfView; // lưu FOV mặc định để ADS tính tỷ lệ
            }
            if (audio) audio.enabled = true;
        }
        else
        {
            if (_cam)  _cam.enabled  = false;
            if (audio) audio.enabled = false;
        }
    }

    void Update()
    {
        if (!_hasAuthority || !_initialized) return;

        // ADS (Aim Down Sights) — chuột phải thu hẹp FOV
        if (_cam != null)
        {
            bool isAiming   = Input.GetMouseButton(1);
            float targetFOV = isAiming ? _defaultFOV * 0.55f : _defaultFOV;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * 14f);
        }

        float mouseX =  Input.GetAxisRaw("Mouse X") * sensitivity;
        float mouseY =  Input.GetAxisRaw("Mouse Y") * sensitivity;

        _xRotation -= mouseY;
        _xRotation  = Mathf.Clamp(_xRotation, -90f, 90f);
        _yRotation += mouseX;

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        if (_bodyTransform != null)
            _bodyTransform.rotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    public void ApplyInput(NetworkInputData input, float deltaTime) { }
    public void ApplyVisual() { }
}
