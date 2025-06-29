using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class GameLauncher : MonoBehaviour
{
    // 1 = Host, 2 = Client
    [Tooltip("1 = Host, 2 = Client")]
    public int launchMode = 1;

    // host adres
    [Tooltip("Adres hosta (np. 127.0.0.1 lub 192.168.1.5)")]
    public string hostAddress = "127.0.0.1";

    private void Start()
    {
        if (launchMode == 1)
        {
            // start host
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started as Host");
        }
        else if (launchMode == 2)
        {
            // adres w UT
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.ConnectionData.Address = hostAddress;
            // klient start
            NetworkManager.Singleton.StartClient();
            Debug.Log($"Started as Client, connecting to {hostAddress}");
        }
        else
        {
            Debug.LogWarning("Nieznany tryb launchMode, nic nie robię.");
        }
    }
}
