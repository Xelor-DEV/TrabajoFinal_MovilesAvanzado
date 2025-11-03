using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class PlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button kickButton;

    private Player player;

    private void OnEnable()
    {
        kickButton.onClick.AddListener(KickPlayer);
    }

    private void OnDisable()
    {
        kickButton.onClick.RemoveListener(KickPlayer);
    }

    public void Initialize(Player player, bool isHost)
    {
        this.player = player;

        if (player.Data.ContainsKey("PlayerName"))
        {
            playerNameText.text = player.Data["PlayerName"].Value;
        }
        else
        {
            playerNameText.text = "Unknown Player";
        }

        // Only show kick button for host and don't allow kicking yourself
        bool isOwnPlayer = player.Id == AuthenticationService.Instance.PlayerId;
        kickButton.gameObject.SetActive(isHost && !isOwnPlayer);
    }

    private void KickPlayer()
    {
        LobbyServiceManager.Instance.KickPlayer(player.Id);
    }
}