using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Transform Options1;  // Host, Client, Back
    [SerializeField] private Button Options1HostBtn;
    [SerializeField] private Button Options1ClientBtn;
    [SerializeField] private Button Options1BackBtn;

    [SerializeField] private Transform Options2;  // AddressInput, PortInput, Next, Back
    [SerializeField] private TMP_InputField Options2AddressInput;
    [SerializeField] private TMP_InputField Options2PortInput;
    [SerializeField] private Button Options2HostBtn;
    [SerializeField] private Button Options2ConnectBtn;
    [SerializeField] private Button Options2BackBtn;

    [SerializeField] private Transform UsernameSelectionUI;
    [SerializeField] private Transform MainMenuUI;
    [SerializeField] private string ClientOrHost = "Client";

    private void InitialiseTransport(string ip, int port)
    {
        UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null) {
            unityTransport.SetConnectionData(
                ip,
                (ushort)port,
                ip  // The server listen address
            );
        }
    }

    private void MoveToUsernameSelection() {
        gameObject.SetActive(false);
        UsernameSelectionUI.gameObject.SetActive(true);
    }

    private void MoveToOptions2() {
        // Turn options2 on and options1 off
        Options1.gameObject.SetActive(false);
        Options2.gameObject.SetActive(true);
        // Set whether the host or the connect button should be on based on ClientOrHost
        Options2HostBtn.gameObject.SetActive(ClientOrHost == "Host");
        Options2ConnectBtn.gameObject.SetActive(ClientOrHost == "Client");
        // If we are hosting, then the default listening address is "0.0.0.0"
        // If we are connecting, then the default connecting address is "127.0.0.1"
        if (ClientOrHost == "Host") {
            Options2AddressInput.text = "0.0.0.0";
        } else if (ClientOrHost == "Client") {
            Options2AddressInput.text = "127.0.0.1";
        }
    }

    private void OnEnable() {
        // Turn options1 on and options2 off
        Options1.gameObject.SetActive(true);
        Options2.gameObject.SetActive(false);
    }

    private void Awake() {
        // Options1 Buttons
        Options1HostBtn.onClick.AddListener(() => {
            ClientOrHost = "Host";
            MoveToOptions2();
        });
        Options1ClientBtn.onClick.AddListener(() => {
            ClientOrHost = "Client";
            MoveToOptions2();
        });
        Options1BackBtn.onClick.AddListener(() => {
            gameObject.SetActive(false);
            MainMenuUI.gameObject.SetActive(true);
        });

        // Options2 Buttons
        Options2BackBtn.onClick.AddListener(() => {
            Options1.gameObject.SetActive(true);
            Options2.gameObject.SetActive(false);
        });
        Options2HostBtn.onClick.AddListener(() => {
            InitialiseTransport(Options2AddressInput.text, int.Parse(Options2PortInput.text));
            if (NetworkManager.Singleton.StartHost()) {
                MoveToUsernameSelection();
            }
        });
        Options2ConnectBtn.onClick.AddListener(() => {
            InitialiseTransport(Options2AddressInput.text, int.Parse(Options2PortInput.text));
            if (NetworkManager.Singleton.StartClient()) {
                MoveToUsernameSelection();
            }
        });
    }
}
