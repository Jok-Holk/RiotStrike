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

        // Ẩn hết, chỉ dùng roomNameText + button trên toàn bộ item
        if (playerCountText != null)
            playerCountText.gameObject.SetActive(false);

        if (selectButton != null)
            selectButton.gameObject.SetActive(false);

        if (roomNameText != null)
        {
            roomNameText.text = $"{profile.name}   {profile.playerCount}/{profile.maxPlayers}   [Chọn]";
            roomNameText.color = Color.white;
            roomNameText.fontSize = 28;

            // Anchor stretch toàn bộ item
            var rt = roomNameText.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 0);
            rt.offsetMax = new Vector2(-8, 0);
        }

        // Click vào toàn bộ item = chọn phòng
        var itemButton = gameObject.GetComponent<Button>();
        if (itemButton == null)
            itemButton = gameObject.AddComponent<Button>();

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(() => _onClickCallback?.Invoke(_roomProfile.name));
    }
}