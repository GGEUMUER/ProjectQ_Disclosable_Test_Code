using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public int ReceiverPort { get; private set; }
    public string PlayerId { get; private set; }
    public TcpClientSender Sender { get; private set; }


    public void Initialize(int port, string playerId, TcpClientSender sender = null)
    {
        ReceiverPort = port;
        PlayerId = playerId;
        Sender = sender;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnApplicationQuit()
    {
        Debug.Log("Application Quit");
        Sender?.Close();     // 송신 스트림 정리
    }
}