using UnityEngine;

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

        // Auto-create AudioSource nếu không được gắn trong Inspector
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.rolloffMode  = AudioRolloffMode.Linear;
            audioSource.maxDistance  = 50f;
            audioSource.playOnAwake  = false;
        }
    }

    public void PlayFireSound()
    {
        if (_wc == null || audioSource == null) return;

        AudioClip clip;
        if (_wc.CurrentSlotID == 1) // Rifle
            clip = _wc.TeamID == 0 ? ak47Sound : m4a1Sound;
        else
            clip = pistolSound;

        if (clip == null) return;

        // PlayOneShot cho phép overlap tự nhiên theo fireRate — đúng behavior
        audioSource.PlayOneShot(clip);
    }

    public void PlayReloadSound()
    {
        if (audioSource == null || reloadSound == null) return;
        // Stop clip hiện tại để tránh chồng reload sound
        audioSource.Stop();
        audioSource.PlayOneShot(reloadSound);
    }
}
