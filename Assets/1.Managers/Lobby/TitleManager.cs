using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleTouchManager : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool isSinglePlayer = true; // 싱글 모드 여부 체크

    public string LobbySceneName { get; private set; } = SceneConstants.LobbyScene;

    private bool _isTransitioning = false;

    void Update()
    {
        if (!_isTransitioning && Input.GetMouseButtonDown(0))
        {
            _isTransitioning = true;
            StartProcess();
        }
    }

    private void Start()
    {
        AudioManager._instance.PlayBGMSound(BGMLIST.LOBBY); 
    }

    private void StartProcess()
    {
        int port = 7777;
        string playerId = "SinglePlayer_" + Random.Range(100, 999);

        if (isSinglePlayer)
        {
            // 싱글 모드: 서버 연결 없이 세션 정보만 초기화
            GameSession.Instance.Initialize(port, playerId, null);
            Debug.Log($"싱글 모드로 시작합니다. (ID: {playerId})");
        }
        else
        {
            // 멀티 모드: 실제 서버 연결 시도
            try
            {
                TcpClientSender sender = new TcpClientSender("127.0.0.1", port);
                GameSession.Instance.Initialize(port, playerId, sender);
                Debug.Log($"서버 연결 성공 (ID: {playerId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"서버 연결 실패: {e.Message}");
                _isTransitioning = false;
                return;
            }
        }

        SceneLauncher.LoadScene(LobbySceneName, true);
    }
}