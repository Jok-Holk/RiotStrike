using UnityEngine;

/// <summary>
/// Đặt script này trên một object LUÔN ACTIVE trong scene (ví dụ Canvas_HUD hoặc GameManager).
/// Kéo Panel_Scoreboard vào trường scoreboardPanel trong Inspector.
/// Nếu không kéo, script sẽ tự tìm qua ScoreboardUI component — kể cả sau khi player spawn.
/// </summary>
public class ScoreboardToggle : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;
    private ScoreboardUI _scoreboardUI;
    private bool _panelInitialized = false;

    void Start()
    {
        // Cố gắng tìm trong Start() — nếu player chưa spawn thì Update() sẽ retry
        TryFindPanel();
    }

    void TryFindPanel()
    {
        if (scoreboardPanel != null)
        {
            if (_scoreboardUI == null)
                _scoreboardUI = scoreboardPanel.GetComponentInChildren<ScoreboardUI>(true);
            if (!_panelInitialized)
            {
                scoreboardPanel.SetActive(false);
                _panelInitialized = true;
                Debug.Log($"[ScoreboardToggle] Panel sẵn sàng: {scoreboardPanel.name}");
            }
            return;
        }

        // QUAN TRỌNG: Tìm trong cùng Canvas với ScoreboardToggle.
        // FindFirstObjectByType có thể trả về panel của REMOTE player (trong Canvas đang inactive).
        // Nếu parent canvas inactive thì SetActive(true) trên panel con cũng không hiện được.
        var myCanvas = GetComponentInParent<Canvas>(true);
        if (myCanvas != null)
        {
            _scoreboardUI = myCanvas.GetComponentInChildren<ScoreboardUI>(true);
        }

        // Fallback: nếu ScoreboardToggle không nằm trong canvas nào
        // (đặt trên GameManager v.v.) → tìm ScoreboardUI trong canvas ACTIVE
        // Fallback 1: tìm trong canvas active bất kỳ
        if (_scoreboardUI == null)
        {
            foreach (var sui in FindObjectsByType<ScoreboardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var parentCanvas = sui.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.gameObject.activeInHierarchy)
                {
                    _scoreboardUI = sui;
                    break;
                }
            }
        }

        // Fallback 2: tìm qua NetworkPlayer của local player (ScoreboardUI nằm trong PlayerPrefab)
        if (_scoreboardUI == null)
        {
            foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (np.Object != null && np.Object.HasInputAuthority)
                {
                    _scoreboardUI = np.GetComponentInChildren<ScoreboardUI>(true);
                    if (_scoreboardUI != null)
                    {
                        Debug.Log($"[ScoreboardToggle] Tìm thấy ScoreboardUI qua NetworkPlayer: {np.gameObject.name}");
                        break;
                    }
                }
            }
        }

        // Fallback 3: lấy bất kỳ ScoreboardUI nào trong scene
        if (_scoreboardUI == null)
            _scoreboardUI = FindFirstObjectByType<ScoreboardUI>(FindObjectsInactive.Include);

        if (_scoreboardUI != null)
        {
            scoreboardPanel = _scoreboardUI.gameObject;
            scoreboardPanel.SetActive(false);
            _panelInitialized = true;
            Debug.Log($"[ScoreboardToggle] Tìm thấy panel: {scoreboardPanel.name} (parent: {scoreboardPanel.transform.parent?.name})");
        }
        else
        {
            // Log mỗi 2 giây để không spam
            if (Time.time % 2f < Time.deltaTime)
                Debug.LogWarning("[ScoreboardToggle] Chưa tìm thấy ScoreboardUI. Kéo Panel_Scoreboard vào Inspector!");
        }
    }

    void Update()
    {
        // Lazy retry — player có thể spawn sau khi Start() chạy
        if (!_panelInitialized)
        {
            TryFindPanel();
            return; // chờ tìm được panel mới xử lý Tab
        }

        if (scoreboardPanel == null) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            scoreboardPanel.SetActive(true);
            if (_scoreboardUI == null)
                _scoreboardUI = scoreboardPanel.GetComponentInChildren<ScoreboardUI>(true);
            _scoreboardUI?.Refresh();
            Debug.Log("[ScoreboardToggle] Tab pressed — showing scoreboard");
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreboardPanel.SetActive(false);
            Debug.Log("[ScoreboardToggle] Tab released — hiding scoreboard");
        }
    }
}
