using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class RelayServiceManager : NonPersistentSingleton<RelayServiceManager>
{
    public async Task<string> CreateRelay(int maxPlayers = 4)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();

            Debug.Log($"Relay created successfully: {relayCode}");
            return relayCode;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay creation failed: {ex.Message}");
            return null;
        }
    }

    public async void JoinRelay(string relayCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            Debug.Log($"Joined relay successfully: {relayCode}");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay join failed: {ex.Message}");
        }
    }
}