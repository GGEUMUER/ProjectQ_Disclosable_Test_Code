// GameRobby.cs

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;

public class GameRobby : MonoBehaviour
{
    //오쏘 기반 명세 코드
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    private bool _isReady = false;

    [Header("Server Data")]
    TcpClientSender _sender;
    string _playerID = "MyID1";
    int _port = 7777;
    bool _changeScene = false;
    AsyncOperation _asyncOperation;

    private bool changeScene = false;

    [System.Serializable]
    public class AuthPayload
    {
        public string username; 
        //현재 비밀번호는 없음. 혹시나 하는 마음에 미리 추가 후, 주석처리
        //public string password;
    }

    private void Start()
    {
        _sender = new TcpClientSender("127.0.0.1", _port);
        GameSession.Instance.Initialize(_port, _playerID, _sender);

        _sender.OnPacketReceived += OnPacketReceived;
        readyButtonText.text = "Unready";

        readyButton.onClick.AddListener(() =>
        {
            if (!_isReady)
            {
                // 패킷 전송 구문, 일단 플레이어 아이디 고정으로
                readyButtonText.text = "Ready";
                _isReady = true;
                _sender.SendRawStringPayloadPacket("Auth", _playerID);
                Debug.Log("전송 잘 됐음: Auth 송신 (Client to Server)");
            }
        });

        //// 패킷 전송 구문, 일단 플레이어 아이디 고정으로
        //_sender.SendPacket("Auth", new AuthPayload { username = _playerID });
        //Debug.Log("전송 잘 됐음: Auth 송신 (Client to Server)");
    }

    void OnPacketReceived(Packet packet)
    {
        /*
            현재 패킷 명세서에 적힌 타입 리스트
            1. MatchingStart
            2. GameStart
        
         */
        switch(packet.type)
        {
            case "MatchingStart":
            {
                Debug.Log("수신 잘 됐음: MatchingStart 수신 (Server to Client)");
                break;
            }
            case "GameStart":
            {
                Debug.Log("수신 잘 됐음: GameStart 수신 (Server to Client)");
                StartCoroutine(LoadSceneAsyncWithHold());
                break;
            }
        }
    }

    IEnumerator LoadSceneAsyncWithHold()
    {
        _asyncOperation = SceneManager.LoadSceneAsync("GameScene");
        // 로딩이 끝나면
        while (!_asyncOperation.isDone)
        {
            Debug.Log("Loading... " + (_asyncOperation.progress * 100f) + "%");
            yield return null;
        }
    }

    void OnApplicationQuit()
    {
        _sender?.SendPacket("Leave", new object());
        _sender?.Close();
    }

    // 기존 패킷 명세 기반 코드
    /*
     * 기존 코드
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    public bool isPlayer1;
    
    private TcpClientSender sender;
    private bool isReady = false;
    private string playerId;
    private int port;
    private AsyncOperation asyncOp;
    private bool changeScene = false;
    
    [System.Serializable]
    public class JoinPayload { public int port; }

    void Start()
    {
        playerId = "MyID1";
        port = 7777;

        sender = new TcpClientSender("127.0.0.1", 7777);
        GameSession.Instance.Initialize(port, playerId, sender);

        sender.OnPacketReceived += OnPacketReceived;

        readyButtonText.text = "Unready";
        sender.SendPacket("Join", new JoinPayload { port = port });
        Debug.Log("Join 패킷 전송됨");

        readyButton.onClick.AddListener(() =>
        {
            if (!isReady)
            {
                sender.SendPacket("Ready", "{}");
                readyButtonText.text = "Ready";
                isReady = true;
                Debug.Log("Ready 전송");
            }
            else
            {
                sender.SendPacket("Unready", "{}");
                readyButtonText.text = "Unready";
                isReady = false;
                Debug.Log("Unready 전송");
            }
        });
    }

    void OnPacketReceived(Packet packet)
    {
        switch (packet.type)
        {
            case "SceneLoad":
                Debug.Log("SceneLoad 패킷 수신 → 씬 로딩 시작");
                StartCoroutine(LoadSceneAsyncWithHold());
                break;
            case "GameStart":
                Debug.Log("GameStart 패킷 수신 → 씬 전환");
                changeScene = true;
                break;
        }
    }

    void OnApplicationQuit()
    {
        sender?.SendPacket("Leave", new object());
        sender?.Close();
    }
    IEnumerator LoadSceneAsyncWithHold()
    {
        asyncOp = SceneManager.LoadSceneAsync("GameScene");
        asyncOp.allowSceneActivation = false;

        // 로딩이 90% (0.9)까지 됐을 때
        while (asyncOp.progress < 0.9f)
        {
            Debug.Log("Loading... " + (asyncOp.progress * 100f) + "%");
            yield return null;
        }
        sender.SendPacket("SceneReady","{}");
        Debug.Log("Wait for Start");
        
        yield return new WaitUntil(() => changeScene);
        changeScene = false;
        // 이제 씬 전환!
        asyncOp.allowSceneActivation = true;
    }*/
}