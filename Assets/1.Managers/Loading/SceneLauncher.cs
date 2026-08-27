using UnityEngine.SceneManagement;

/// <summary>
/// 전역 씬 전환 요청 처리 정적 클래스
/// 씬 전환에 필요한 데이터 임시 저장 및 로딩 씬 로드 담당
/// </summary>
/// <remarks>
/// 퍼사드 패턴(Facade Pattern) 적용으로 씬 전환 과정 단순화
/// Static 클래스 설계 이유:
/// 1. 접근 편의성: 인스턴스화 없이 어디서든 씬 전환 요청 가능
/// 2. 단순한 데이터 전달: DontDestroyOnLoad 없이 로딩 씬을 거쳐 최종 목적지 씬으로 데이터 전달
/// </remarks>
public static class SceneLauncher
{
    /// <summary>
    /// 최종 목적지 씬 이름
    /// </summary>
    public static string NextSceneName { get; private set; } = SceneConstants.LobbyScene;
    
    /// <summary>
    /// 싱글 플레이어 모드 여부
    /// LoadingController가 로딩 전략(Local/Network)을 결정하는 데 사용
    /// </summary>
    public static bool IsSinglePlayer { get; private set; } = true;

    /// <summary>
    /// 씬 로드 요청
    /// 항상 로딩 씬을 먼저 경유하여 최종 목적지 씬 로드
    /// </summary>
    /// <param name="sceneName">최종 목적지 씬 이름</param>
    /// <param name="isSinglePlayer">싱글 플레이어 모드 여부 (true: 로컬 로딩, false: 네트워크 로딩)</param>
    public static void LoadScene(string sceneName, bool isSinglePlayer = true)
    {
        // 로딩에 필요한 데이터 정적 속성에 저장
        NextSceneName = sceneName;
        IsSinglePlayer = isSinglePlayer;
        
        // 로딩 씬 선행 로드
        // LoadingController가 정적 속성 참조하여 실제 로딩 프로세스 담당
        SceneManager.LoadScene(SceneConstants.LoadingScene);
    }
}