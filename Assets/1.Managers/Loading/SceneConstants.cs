/// <summary>
/// 전역 문자열 상수 관리 클래스
/// 씬 이름, 네트워크 패킷 타입 등 핵심 상수 정의
/// </summary>
/// <remarks>
/// '매직 스트링' 사용 방지를 통한 유지보수성 및 안정성 향상 목적
/// </remarks>
public static class SceneConstants
{
    /// <summary>
    /// 로딩 씬 이름
    /// </summary>
    public const string LoadingScene = "LoadingScene_Test";

    /// <summary>
    /// 로비 씬 이름
    /// </summary>
    public const string LobbyScene = "LobbyScene_Test";

    /// <summary>
    /// 전투 씬 이름
    /// </summary>
    public const string BattleScene = "Card_And_Game_Test";

    /// <summary>
    /// 로딩 완료 식별용 네트워크 패킷 타입
    /// </summary>
    public const string PacketTypeLoad = "Load";
}