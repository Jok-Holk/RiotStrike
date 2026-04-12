using UnityEngine;

public class WeaponAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   ak47Sound;
    [SerializeField] private AudioClip   m4a1Sound;
    [SerializeField] private AudioClip   pistolSound;
    [SerializeField] private AudioClip   reloadSound;

    private WeaponController _wc;

    void Start() => _wc = GetComponent<WeaponController>();

    public void PlayFireSound()
    {
        if (_wc == null || audioSource == null) return;

        // Dùng CurrentSlot (networked, chính xác) thay vì IsRifleUnlocked
        AudioClip clip;
        if (_wc.CurrentSlotID == 1) // Rifle
            clip = _wc.TeamID == 0 ? ak47Sound : m4a1Sound;
        else
            clip = pistolSound;

        if (clip == null) return;

        // Chỉ play nếu không đang play clip đó để tránh overlap spam
        // PlayOneShot cho phép overlap — dùng trực tiếp vì đây là tốc độ fire bình thường
        audioSource.PlayOneShot(clip);
    }

    public void PlayReloadSound()
    {
        if (audioSource == null || reloadSound == null) return;
        // Stop reload sound cũ trước khi play mới tránh chồng chéo
        audioSource.Stop();
        audioSource.PlayOneShot(reloadSound);
    }
}
