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
        if (_wc == null) return;
        AudioClip clip = _wc.IsRifleUnlocked
            ? (_wc.TeamID == 0 ? ak47Sound : m4a1Sound)
            : pistolSound;
        audioSource.PlayOneShot(clip);
    }

    public void PlayReloadSound() => audioSource.PlayOneShot(reloadSound);
}
