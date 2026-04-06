using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoomManager : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI roomStatusText;

    [Header("Team A - kéo Content vào")]
    [SerializeField] private Transform teamAPlayerList;
    [SerializeField] private Button    joinTeamAButton;

    [Header("Team B - kéo Content vào")]
    [SerializeField] private Transform teamBPlayerList;
    [SerializeField] private Button    joinTeamBButton;

    [Header("Player Item Prefab")]
    [SerializeField] private GameObject playerNameItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private Button configButton;
    [SerializeField] private Button closeConfigButton;

    [Header("Panels")]
    [SerializeField] private GameObject panelRoom;
    [SerializeField] private GameObject hostConfigPanel;

    [Header("Start Game Warning")]
    [SerializeField] private TextMeshProUGUI startWarningText; // text cảnh báo điều kiện chưa đủ

    private NetworkRunner _runner;
    private bool _isHost => _runner != null && _runner.IsServer;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();

        joinTeamAButton.onClick.AddListener(() => RequestJoinTeam(0));
        joinTeamBButton.onClick.AddListener(() => RequestJoinTeam(1));
        leaveRoomButton.onClick.AddListener(OnLeaveRoom);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGame);
        if (configButton != null)
            configButton.onClick.AddListener(() => hostConfigPanel.SetActive(true));
        if (closeConfigButton != null)
            closeConfigButton.onClick.AddListener(() => hostConfigPanel.SetActive(false));

        if (panelRoom)       panelRoom.SetActive(true);
        if (hostConfigPanel) hostConfigPanel.SetActive(false);
        if (startGameButton) startGameButton.gameObject.SetActive(_isHost);
        if (configButton)    configButton.gameObject.SetActive(_isHost);
        if (startWarningText) startWarningText.gameObject.SetActive(false);

        if (_runner != null && roomNameText != null)
            roomNameText.text = _runner.SessionInfo.Name;

        StartCoroutine(SendNicknameWhenReady());
        RefreshPlayerList();
    }

    void OnEnable()
    {
        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();
        RefreshPlayerList();
    }

    System.Collections.IEnumerator SendNicknameWhenReady()
    {
        while (_runner == null || RoomPlayerData.instance == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }
        string nick = LobbyManager.instance != null
            ? LobbyManager.instance.GetLocalNickName()
            : "Player";
        RoomPlayerData.instance.RPC_RegisterPlayer(_runner.LocalPlayer, nick);
    }

    public void RefreshPlayerList()
    {
        if (_runner == null) return;

        foreach (Transform c in teamAPlayerList) Destroy(c.gameObject);
        foreach (Transform c in teamBPlayerList) Destroy(c.gameObject);

        if (RoomPlayerData.instance == null)
        {
            if (roomStatusText != null) roomStatusText.text = "Đang chờ...";
            return;
        }

        var slots = RoomPlayerData.instance.GetOccupied();
        foreach (var slot in slots)
        {
            bool isLocal     = slot.PlayerRef == _runner.LocalPlayer;
            Transform parent = slot.Team == 0 ? teamAPlayerList : teamBPlayerList;
            AddPlayerItem(parent, slot.NickName.ToString(), isLocal, slot.PlayerRef, slot.Team);
        }

        StartCoroutine(RebuildNextFrame());

        if (roomStatusText != null)
            roomStatusText.text = $"Đang chờ... ({slots.Count}/{_runner.SessionInfo.MaxPlayers})";

        // Cập nhật trạng thái nút BẮT ĐẦU
        if (_isHost) UpdateStartButton();
    }

    // Kiểm tra điều kiện start game
    string GetStartBlockReason()
    {
        if (RoomPlayerData.instance == null)
            return "Chưa sẵn sàng...";

        var slots = RoomPlayerData.instance.GetOccupied();

        int teamACount = 0, teamBCount = 0;
        foreach (var s in slots)
        {
            if (s.Team == 0) teamACount++;
            else             teamBCount++;
        }

        if (teamACount == 0 || teamBCount == 0)
            return "Mỗi team cần ít nhất 1 người!";

        if (slots.Count < 2)
            return "Cần ít nhất 2 người chơi!";

        // Kiểm tra config hợp lệ
        var config = FindFirstObjectByType<HostConfigManager>();
        if (config != null && config.RoundTime == 0)
            return "Cần set thời gian trận đấu!";

        return null; // null = không bị block
    }

    void UpdateStartButton()
    {
        if (startGameButton == null) return;

        string reason = GetStartBlockReason();
        bool canStart = reason == null;

        // Dim button khi chưa đủ điều kiện
        var colors = startGameButton.colors;
        colors.normalColor = canStart
            ? Color.white
            : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        startGameButton.colors = colors;
        startGameButton.interactable = canStart;

        // Hiện warning text nếu có
        if (startWarningText != null)
        {
            startWarningText.gameObject.SetActive(!canStart);
            if (!canStart) startWarningText.text = reason;
        }
    }

    System.Collections.IEnumerator RebuildNextFrame()
    {
        yield return null;
        RebuildContent(teamAPlayerList);
        RebuildContent(teamBPlayerList);
    }

    void RebuildContent(Transform content)
    {
        if (content == null) return;
        var rect = content.GetComponent<RectTransform>();
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    void AddPlayerItem(Transform parent, string playerName, bool isLocal,
                       PlayerRef playerRef, int currentTeam)
    {
        if (playerNameItemPrefab == null) return;

        var item = Instantiate(playerNameItemPrefab, parent);

        // Fix item height
        var itemLE = item.GetComponent<LayoutElement>() ?? item.AddComponent<LayoutElement>();
        itemLE.minHeight       = 50;
        itemLE.preferredHeight = 50;
        itemLE.flexibleHeight  = 0;

        // Text tên player
        var text = item.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text      = playerName;
            text.color     = isLocal ? new Color(1f, 0.9f, 0.2f) : Color.white;
            text.fontSize  = 22;
            text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = false;

            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(6, 0);
            trt.offsetMax = (_isHost && !isLocal)
                ? new Vector2(-88, 0)
                : new Vector2(-4, 0);
        }

        // Chỉ host mới thấy các nút điều khiển, không hiện trên chính mình
        if (_isHost && !isLocal)
        {
            string arrow    = currentTeam == 0 ? "→" : "←";
            int    nextTeam = currentTeam == 0 ? 1 : 0;

            var moveBtn = CreateAnchoredButton(item.transform, arrow,
                new Color(0.3f, 0.8f, 0.3f), rightOffset: 44);
            var capturedRef  = playerRef;
            var capturedTeam = nextTeam;
            moveBtn.onClick.AddListener(() => MovePlayer(capturedRef, capturedTeam));

            var kickBtn = CreateAnchoredButton(item.transform, "X",
                new Color(0.9f, 0.2f, 0.2f), rightOffset: 0);
            var capturedKick = playerRef;
            kickBtn.onClick.AddListener(() => KickPlayer(capturedKick));
        }
    }

    Button CreateAnchoredButton(Transform parent, string label, Color bgColor, float rightOffset)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(1, 0.5f);
        rt.offsetMin = new Vector2(-rightOffset - 40, 4);
        rt.offsetMax = new Vector2(-rightOffset, -4);

        var img = go.AddComponent<Image>();
        img.color         = bgColor;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor      = bgColor;
        cs.highlightedColor = bgColor + new Color(0.2f, 0.2f, 0.2f);
        cs.pressedColor     = bgColor * 0.7f;
        cs.fadeDuration     = 0.05f;
        btn.colors = cs;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);

        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18;
        tmp.color     = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        return btn;
    }

    void MovePlayer(PlayerRef playerRef, int newTeam)
    {
        if (!_isHost || RoomPlayerData.instance == null) return;
        RoomPlayerData.instance.RPC_RequestChangeTeam(playerRef, newTeam);
    }

    void KickPlayer(PlayerRef playerRef)
    {
        if (!_isHost || _runner == null) return;
        _runner.Disconnect(playerRef);
    }

    void RequestJoinTeam(int team)
    {
        if (_runner == null || RoomPlayerData.instance == null) return;
        RoomPlayerData.instance.RPC_RequestChangeTeam(_runner.LocalPlayer, team);
    }

    void OnLeaveRoom()
    {
        if (_runner != null) _ = _runner.Shutdown();
    }

    void OnStartGame()
    {
        if (!_isHost) return;

        string reason = GetStartBlockReason();
        if (reason != null)
        {
            if (startWarningText != null)
            {
                startWarningText.gameObject.SetActive(true);
                startWarningText.text = reason;
            }
            return;
        }

        LobbyManager.instance.StartGame();
    }
}