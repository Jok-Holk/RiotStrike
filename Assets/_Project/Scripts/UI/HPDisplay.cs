using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HPDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider hpSlider;

    void OnEnable()  => PlayerHealth.OnHealthChanged += UpdateHP;
    void OnDisable() => PlayerHealth.OnHealthChanged -= UpdateHP;

    void UpdateHP(PlayerHealth health)
    {
        // Chỉ update UI cho chính người chơi có InputAuthority
        if (!health.Object.HasInputAuthority) return;

        if (hpText)   hpText.text = health.Health.ToString();
        if (hpSlider) hpSlider.value = health.Health / 100f;
    }
}
