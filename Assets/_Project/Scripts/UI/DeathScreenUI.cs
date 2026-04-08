using UnityEngine;              // cho MonoBehaviour, GameObject, SerializeField
using TMPro;                    // cho TextMeshProUGUI
using UnityEngine;
using TMPro;
using System.Collections;

using System.Collections;       // cho IEnumerator

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI respawnText;

    void OnEnable()  => PlayerHealth.OnPlayerDied += OnDied;
    void OnDisable() => PlayerHealth.OnPlayerDied -= OnDied;

    void OnDied(PlayerHealth health)
    {
        if (!health.Object.HasInputAuthority) return;
        deathPanel.SetActive(true);
        StartCoroutine(CountdownRespawn(3f));
    }

    IEnumerator CountdownRespawn(float delay)
    {
        float t = delay;
        while (t > 0)
        {
            if (respawnText) respawnText.text = $"Hồi sinh sau: {Mathf.CeilToInt(t)}";
            yield return null;
            t -= Time.deltaTime;
        }
        deathPanel.SetActive(false);
    }
}
