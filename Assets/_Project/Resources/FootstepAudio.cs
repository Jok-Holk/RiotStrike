using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepInterval = 0.55f;

    private FPSController _fps;
    private float         _stepTimer;
    private bool          _ready;
    private bool          _wasMoving = false;

    void Awake()
    {
        // Fix lỗi "nghe tiếng chân ngay khi vào game" — AudioSource.playOnAwake=true trong Inspector
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake  = false;
            audioSource.spatialBlend = 0f;   // 2D — footstep của chính mình phải nghe rõ
            audioSource.volume       = 1f;   // volume tối đa, clip tự điều chỉnh độ lớn
            audioSource.Stop();
        }
    }

    void Start()
    {
        _fps = GetComponent<FPSController>();
        _stepTimer = stepInterval; // không play ngay frame đầu
    }

    void Update()
    {
        if (!_ready)
        {
            if (_fps == null || _fps.Object == null || !_fps.Object.IsValid) return;
            _ready = true;
            _stepTimer = stepInterval;
        }

        // Chỉ play cho local player
        if (!_fps.Object.HasInputAuthority) return;

        bool isMoving = Mathf.Abs(_fps.LastForward) > 0.15f
                     || Mathf.Abs(_fps.LastStrafe)  > 0.15f;

        // KHÔNG dùng _fps.IsGrounded vì CharacterController bị disabled trên máy client
        // (FPSController.Spawned() set _cc.enabled = HasStateAuthority — client = false)
        // → isGrounded luôn false trên client → footstep không bao giờ phát
        // Game này không có jump nên chỉ cần kiểm tra isMoving là đủ.
        if (!isMoving)
        {
            _stepTimer = stepInterval;
            _wasMoving = false;
            // Dừng clip ngay lập tức khi không di chuyển — tránh âm thanh vang sau khi dừng
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            return;
        }

        // Bắt đầu di chuyển từ đứng yên → delay rất ngắn cho bước đầu tiên
        if (!_wasMoving)
        {
            _wasMoving = true;
            _stepTimer = 0.1f; // footstep đầu phát gần như ngay lập tức
        }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer <= 0f)
        {
            _stepTimer = stepInterval;
            // Guard isPlaying: nếu clip trước chưa xong thì bỏ qua lần này.
            // Tránh trường hợp stepInterval < clip duration → nhiều tiếng chồng nhau.
            if (audioSource != null && footstepClips.Length > 0 && !audioSource.isPlaying)
            {
                audioSource.clip   = footstepClips[Random.Range(0, footstepClips.Length)];
                audioSource.volume = 0.5f;
                audioSource.Play();
            }
        }
    }
}
