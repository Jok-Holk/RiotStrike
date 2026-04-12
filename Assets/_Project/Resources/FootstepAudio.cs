using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepInterval = 0.4f;

    private FPSController _fps;
    private float _stepTimer;

    void Start() => _fps = GetComponent<FPSController>();

    void Update()
    {
        // Kiểm tra an toàn: _fps phải tồn tại và NetworkObject (Object) phải khác null
        if (_fps == null || _fps.Object == null || !_fps.Object.HasInputAuthority) return;
        bool isMoving = Mathf.Abs(_fps.LastForward) > 0.1f
                     || Mathf.Abs(_fps.LastStrafe) > 0.1f;
        if (!isMoving) { _stepTimer = 0; return; }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer <= 0f)
        {
            _stepTimer = stepInterval;
            if (footstepClips.Length > 0)
                audioSource.PlayOneShot(
                    footstepClips[Random.Range(0, footstepClips.Length)], 0.5f);
        }
    }
}
