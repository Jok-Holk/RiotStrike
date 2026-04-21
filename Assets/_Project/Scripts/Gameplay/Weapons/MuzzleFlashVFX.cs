using UnityEngine;

/// <summary>
/// Gắn vào GameObject MuzzleFlash_VFX cùng với SpriteRenderer.
/// Kéo sprite _Pngtree_muzzle_flash_images vào SpriteRenderer.sprite.
/// WeaponController sẽ SetActive(true) → script tự tắt sau duration.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MuzzleFlashVFX : MonoBehaviour
{
    [SerializeField] private float duration = 0.06f;
    [SerializeField] private float minScale = 0.15f;
    [SerializeField] private float maxScale = 0.35f;
    [SerializeField] private bool randomRotate = true;

    private SpriteRenderer _sr;
    private float _timer;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        // Render on top of everything — Additive blend
        _sr.material = new Material(Shader.Find("Sprites/Default"));
        _sr.color = Color.white;
    }

    void OnEnable()
    {
        _timer = duration;

        // Random scale mỗi lần flash
        float s = Random.Range(minScale, maxScale);
        transform.localScale = new Vector3(s, s, 1f);

        // Random Z rotation
        if (randomRotate)
            transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

        // Billboard: luôn nhìn về camera
        var cam = Camera.main;
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                             cam.transform.rotation * Vector3.up);
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            gameObject.SetActive(false);
    }
}