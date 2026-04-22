using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Gắn vào Panel_Scoreboard trong Canvas_HUD.
/// Tự tìm content transforms theo tên — không cần gán Inspector nếu đặt tên đúng.
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    // Nếu gán trong Inspector thì dùng, không thì tự tìm theo tên
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform  teamAContent;
    [SerializeField] private Transform  teamBContent;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private TextMeshProUGUI teamAScoreText;
    [SerializeField] private TextMeshProUGUI teamBScoreText;
    [SerializeField] private TextMeshProUGUI roundTimerText;

    void Start() => EnsureRefs();

    // Không xử lý Tab ở đây — ScoreboardToggle đảm nhiệm show/hide
    // ScoreboardUI chỉ lo data refresh

    /// Khởi tạo lazy — gọi ở cả Start() lẫn đầu Refresh().
    /// Cần thiết vì Refresh() có thể được gọi trước Start():
    /// ScoreboardToggle gọi SetActive(true) rồi Refresh() ngay trong cùng frame,
    /// còn Start() chỉ chạy ở cuối frame đó → refs vẫn null khi Refresh() chạy.
    void EnsureRefs()
    {
        if (teamAContent == null)   teamAContent   = FindChildByName(transform, "Image_TeamA");
        if (teamBContent == null)   teamBContent   = FindChildByName(transform, "Image_TeamB");
        if (teamAScoreText == null) teamAScoreText = FindTMPByName("Text_NameTeamA");
        if (teamBScoreText == null) teamBScoreText = FindTMPByName("Text_NameTeamB");
        if (roundTimerText == null) roundTimerText = FindTMPByName("Text_RoundTimer");
    }

    public void Refresh()
    {
        EnsureRefs(); // đảm bảo refs sẵn sàng dù Refresh() gọi trước Start()

        // === DIAGNOSTIC LOG — xóa sau khi debug xong ===
        Debug.Log($"[ScoreboardUI] Refresh() | teamAContent={teamAContent?.name ?? "NULL"} teamBContent={teamBContent?.name ?? "NULL"} rowPrefab={playerRowPrefab?.name ?? "NULL"} RoomData={RoomPlayerData.instance != null}");

        if (teamAContent) { foreach (Transform c in teamAContent) Destroy(c.gameObject); EnsureListLayout(teamAContent); }
        if (teamBContent) { foreach (Transform c in teamBContent) Destroy(c.gameObject); EnsureListLayout(teamBContent); }

        // Score
        if (GameManager.instance != null)
        {
            if (teamAScoreText) teamAScoreText.text = $"XANH: {GameManager.instance.TeamAScore}";
            if (teamBScoreText) teamBScoreText.text = $"ĐỎ: {GameManager.instance.TeamBScore}";
        }

        // Timer
        if (roundTimerText)
        {
            var tm = FindFirstObjectByType<TimerManager>();
            if (tm != null)
            {
                float t = tm.GetRemainingTime();
                roundTimerText.text = $"{Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
            }
        }

        if (playerRowPrefab == null)
        {
            Debug.LogError("[ScoreboardUI] playerRowPrefab CHƯA ĐƯỢC GÁN trong Inspector! Kéo prefab hàng player vào ScoreboardUI component.");
            return;
        }

        if (teamAContent == null)
            Debug.LogError("[ScoreboardUI] Không tìm thấy 'Image_TeamA' trong Panel_Scoreboard. Kiểm tra tên object trong hierarchy.");
        if (teamBContent == null)
            Debug.LogError("[ScoreboardUI] Không tìm thấy 'Image_TeamB' trong Panel_Scoreboard. Kiểm tra tên object trong hierarchy.");

        var runner = FindFirstObjectByType<NetworkRunner>();
        int slotCount = 0;

        if (RoomPlayerData.instance != null)
        {
            // Ưu tiên RoomPlayerData (chứa NickName + Team đã đồng bộ từ lobby)
            foreach (var slot in RoomPlayerData.instance.GetOccupied())
            {
                slotCount++;
                var parent = slot.Team == 0 ? teamAContent : teamBContent;
                if (parent == null) continue;

                var row   = Instantiate(playerRowPrefab, parent);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                string teamLabel = slot.Team == 0 ? "[Xanh]" : "[Đỏ]";
                if (texts.Length >= 1)
                {
                    texts[0].text = $"{slot.NickName}  {teamLabel}";
                    if (runner != null && slot.PlayerRef == runner.LocalPlayer)
                        texts[0].color = new Color(1f, 0.9f, 0.2f);
                }
            }
        }
        else
        {
            // Fallback: RoomPlayerData bị null sau scene load → đọc trực tiếp từ NetworkPlayer
            // NickName và Team được sync qua [Networked] nên vẫn chính xác.
            Debug.LogWarning("[ScoreboardUI] RoomPlayerData null → fallback đọc từ NetworkPlayer objects.");
            foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (np.Object == null || !np.Object.IsValid) continue;
                slotCount++;
                var parent = np.Team == 0 ? teamAContent : teamBContent;
                if (parent == null) continue;

                var row   = Instantiate(playerRowPrefab, parent);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                string teamLabel = np.Team == 0 ? "[Xanh]" : "[Đỏ]";
                if (texts.Length >= 1)
                {
                    texts[0].text = $"{np.NickName}  {teamLabel}";
                    if (runner != null && np.Object.InputAuthority == runner.LocalPlayer)
                        texts[0].color = new Color(1f, 0.9f, 0.2f);
                }
            }
        }

        Debug.Log($"[ScoreboardUI] Hiển thị {slotCount} người chơi.");
    }

    /// Đảm bảo container có VerticalLayoutGroup + ContentSizeFitter để rows không đè nhau
    static void EnsureListLayout(Transform container)
    {
        var vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight  = false;
            vlg.childControlWidth   = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
        }

        var csf = container.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = container.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    // Helpers
    static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static TextMeshProUGUI FindTMPByName(string name)
    {
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (t.gameObject.name == name) return t;
        return null;
    }
}
