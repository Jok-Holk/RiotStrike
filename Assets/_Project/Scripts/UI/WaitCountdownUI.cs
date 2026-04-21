using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị đếm ngược trước khi game bắt đầu (thời gian chờ trong SafeZone).
/// Gắn vào bất kỳ object nào trong Canvas_HUD của player.
/// Kéo một TextMeshProUGUI vào countdownText trong Inspector.
/// countdownRoot là optional parent panel để ẩn/hiện nguyên cụm UI.
/// </summary>
public class WaitCountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject      countdownRoot; // panel wrapper (nếu có)

    void Update()
    {
        var szm = SafeZoneManager.instance;

        // Không có SafeZoneManager → ẩn UI
        if (szm == null)
        {
            SetVisible(false);
            return;
        }

        // Game đã bắt đầu → ẩn đếm ngược
        if (szm.GameStarted)
        {
            SetVisible(false);
            return;
        }

        float t = szm.GetCountdown();
        if (t <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        if (countdownText != null)
        {
            int secs = Mathf.CeilToInt(t);
            countdownText.text = $"GAME STARTS IN\n<size=150%><b>{secs}</b></size>";
        }
    }

    void SetVisible(bool visible)
    {
        if (countdownRoot != null)
        {
            if (countdownRoot.activeSelf != visible)
                countdownRoot.SetActive(visible);
        }
        else if (countdownText != null)
        {
            var go = countdownText.gameObject;
            if (go.activeSelf != visible)
                go.SetActive(visible);
        }
    }
}
