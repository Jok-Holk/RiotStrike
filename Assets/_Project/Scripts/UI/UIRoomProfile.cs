using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoomProfile : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button selectButton;

    private RoomProfile _roomProfile;
    private Action<string> _onClickCallback;

    public void Setup(RoomProfile profile, Action<string> onClickCallback)
    {
        _roomProfile = profile;
        _onClickCallback = onClickCallback;

        roomNameText.text = profile.name;
        playerCountText.text = profile.playerCount + "/" + profile.maxPlayers;
        selectButton.onClick.AddListener(() => _onClickCallback?.Invoke(_roomProfile.name));
    }
}
