using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        nickNameInput.text = "Player" + Random.Range(100, 999);
        roomNameInput.text = "Room1";

        connectButton.onClick.AddListener(OnConnectClick);
        createButton.onClick.AddListener(OnCreateClick);
        joinButton.onClick.AddListener(OnJoinClick);

        createButton.interactable = false;
        joinButton.interactable = false;
    }

    void OnEnable()
    {
        if (LobbyManager.instance == null) return;
        LobbyManager.instance.OnRoomListUpdated += RefreshRoomList;
        LobbyManager.instance.OnJoinFailed += OnJoinFailed;
    }

    void OnDisable()
    {
        if (LobbyManager.instance == null) return;
        LobbyManager.instance.OnRoomListUpdated -= RefreshRoomList;
        LobbyManager.instance.OnJoinFailed -= OnJoinFailed;
    }

    void OnConnectClick()
    {
        string nick = nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) nick = "Player";

        SetStatus("Connecting...");
        connectButton.interactable = false;
        LobbyManager.instance.Connect(nick);

        createButton.interactable = true;
        joinButton.interactable = true;
        SetStatus("Connected. Ready to play.");
    }

    void OnCreateClick()
    {
        string room = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(room)) return;
        SetStatus("Creating room: " + room);
        LobbyManager.instance.CreateRoom(room);
    }

    void OnJoinClick()
    {
        string room = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(room)) return;
        SetStatus("Joining room: " + room);
        LobbyManager.instance.JoinRoom(room);
    }

    void RefreshRoomList(List<SessionInfo> sessions)
    {
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        foreach (SessionInfo session in sessions)
        {
            UIRoomProfile item = Instantiate(roomProfilePrefab, roomListContent);
            item.Setup(new RoomProfile
            {
                name = session.Name,
                playerCount = session.PlayerCount,
                maxPlayers = session.MaxPlayers
            }, OnRoomClick);
        }
    }

    void OnRoomClick(string roomName)
    {
        roomNameInput.text = roomName;
    }

    void OnJoinFailed(string reason)
    {
        SetStatus("Join failed: " + reason);
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
