using UnityEngine;

/// Team 0 = XANH = M4A1 | Team 1 = ĐỎ = AK47u
public class WeaponAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   ak47Sound;
    [SerializeField] private AudioClip   m4a1Sound;
    [SerializeField] private AudioClip   pistolSound;
    [SerializeField] private AudioClip   reloadSound;

    private WeaponController _wc;

    void Start()
    {
        _wc = GetComponent<WeaponController>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.rolloffMode  = AudioRolloffMode.Linear;
            audioSource.maxDistance  = 50f;
            audioSource.playOnAwake  = false;
            // spatialBlend được set động khi play — xem PlayFireSound()
        }
    }

    /// Gọi từ RPC_NotifyFire (chạy trên mọi client).
    /// Local player → 2D (spatialBlend=0), Remote → 3D (spatialBlend=1).
    public void PlayFireSound()
    {
        if (_wc == null || audioSource == null) return;

        // Xác định local/remote để set blend
        bool isLocal = _wc.Object != null && _wc.Object.HasInputAuthority;
        audioSource.spatialBlend = isLocal ? 0f : 1f;

        AudioClip clip;
        if (_wc.CurrentSlotID == 1) // Rifle
            clip = _wc.TeamID == 1 ? ak47Sound : m4a1Sound; // team 1 = đỏ = AK
        else
            clip = pistolSound;

        if (clip == null) return;
        // Local player: nghe nhỏ hơn (0.35) vì 2D, remote: giữ 0.6 để nghe vị trí đối thủ
        float vol = isLocal ? 0.35f : 0.6f;
        audioSource.PlayOneShot(clip, vol);
    }

    /// Chỉ gọi từ RPC_NotifyReload (HasInputAuthority guard ở ngoài).
    public void PlayReloadSound()
    {
        if (audioSource == null || reloadSound == null) return;
        // Không gọi Stop() ở đây — cắt ngang tiếng bắn đang phát
        audioSource.spatialBlend = 0f; // reload luôn là local, 2D
        audioSource.PlayOneShot(reloadSound);
    }
}
