using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TODO khi làm tiếp:
/// - Thêm Button_Settings (gọi canvasSettings.SetActive(true))
/// - Thêm Button_Back (quay về Canvas_MainMenu)
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private TMP_InputField nickNameInput;
    [SerializeField] private Button connectButton;

    [Header("Room")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private UIRoomProfile roomProfilePrefab;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Canvas Navigation")]
    [SerializeField] private GameObject canvasMainMenu;   // Canvas_MainMenu
    [SerializeField] private GameObject canvasLobby;      // Canvas_Lobby (self)
    [SerializeField] private GameObject canvasSettings;   // Canvas_Settings

    // TODO: uncommment khi thêm button vào scene
    // [SerializeField] private Button backButton;
    // [SerializeField] private Button settingsButton;

    void Start()
    {
        nickNameInput.text = PlayerPrefs.GetString("NickName", "Player" + Random.Range(100, 999));
        roomNameInput.text = "Room1";

        connectButton.onClick.AddListener(OnConnectClick);
        createButton.onClick.AddListener(OnCreateClick);
        joinButton.onClick.AddListener(OnJoinClick);

        // TODO: uncommment khi thêm button vào scene
        // backButton.onClick.AddListener(OnBackClick);
        // settingsButton.onClick.AddListener(() => canvasSettings.SetActive(true));

        createButton.interactable = false;
        joinButton.interactable = false;

        LobbyManager.instance.OnRoomListUpdated += RefreshRoomList;
        LobbyManager.instance.OnJoinFailed      += OnJoinFailed;
        LobbyManager.instance.OnConnected       += OnConnectedSuccess;
    }

    void OnDestroy()
    {
        if (LobbyManager.instance == null) return;
        LobbyManager.instance.OnRoomListUpdated -= RefreshRoomList;
        LobbyManager.instance.OnJoinFailed      -= OnJoinFailed;
        LobbyManager.instance.OnConnected       -= OnConnectedSuccess;
    }

    void OnConnectClick()
    {
        string nick = nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) nick = "Player";
        SetStatus("Đang kết nối...");
        connectButton.interactable = false;
        LobbyManager.instance.Connect(nick);
    }

    void OnConnectedSuccess()
    {
        createButton.interactable = true;
        joinButton.interactable   = true;
        SetStatus("Đã kết nối. Sẵn sàng chơi.");
    }

    void OnCreateClick()
    {
        string room = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(room)) return;
        SetStatus("Đang tạo phòng: " + room);
        createButton.interactable = false;
        joinButton.interactable   = false;
        LobbyManager.instance.CreateRoom(room);
    }

    void OnJoinClick()
    {
        string room = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(room)) return;
        SetStatus("Đang vào phòng: " + room);
        createButton.interactable = false;
        joinButton.interactable   = false;
        LobbyManager.instance.JoinRoom(room);
    }

    // TODO: uncommment khi thêm button vào scene
    // void OnBackClick()
    // {
    //     canvasLobby.SetActive(false);
    //     canvasMainMenu.SetActive(true);
    // }

    void RefreshRoomList(List<SessionInfo> sessions)
    {
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        foreach (SessionInfo session in sessions)
        {
            UIRoomProfile item = Instantiate(roomProfilePrefab, roomListContent);
            item.Setup(new RoomProfile
            {
                name        = session.Name,
                playerCount = session.PlayerCount,
                maxPlayers  = session.MaxPlayers
            }, OnRoomClick);
        }
    }

    void OnRoomClick(string roomName) => roomNameInput.text = roomName;

    void OnJoinFailed(string reason)
    {
        SetStatus("Thất bại: " + reason);
        createButton.interactable  = true;
        joinButton.interactable    = true;
        connectButton.interactable = true;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}