using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 로딩 전략 인터페이스
/// 전략 패턴 기반 설계. 로딩 방식의 유연한 교체 목적
/// LoadingController가 구체적인 로딩 방식(로컬/네트워크)에 비의존적으로 동작하도록 보장
/// </summary>
public interface ILoadingStrategy
{
    /// <summary>
    /// 로딩 선행 작업 비동기 대기
    /// 로컬 파일 로딩, 서버 접속, 데이터 동기화 예정
    /// </summary>
    /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
    /// <returns>완료 시점을 알리는 UniTask</returns>
    UniTask WaitUntilReadyAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// 사용 리소스 정리
    /// 전략 객체 소멸 시 호출. 예: 이벤트 구독 해제, 네트워크 연결 종료
    /// </summary>
    void Cleanup();
}

/// <summary>
/// 로컬(싱글 플레이) 로딩 시뮬레이션 전략
/// 실제 작업 없이 최소 로딩 시간 보장 목적
/// </summary>
public class LocalLoadingStrategy : ILoadingStrategy
{
    /// <summary>
    /// 지정된 시간만큼 대기
    /// CancellationToken을 통해 대기 중 작업 즉시 중단 가능
    /// </summary>
    public async UniTask WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        // 로컬 로딩의 최소 딜레이 보장
        // ignoreTimeScale: false 설정으로 Time.timeScale 영향 받음 (일시정지 시 로딩 동시 정지)
        await UniTask.Delay(1000, ignoreTimeScale: false, cancellationToken: cancellationToken); 
    }

    /// <summary>
    /// 리소스 정리 (LocalLoadingStrategy)
    /// 특별히 정리할 리소스가 없어 내용 없음
    /// </summary>
    public void Cleanup() { }
}

/// <summary>
/// 네트워크(멀티플레이) 로딩 전략
/// 서버의 특정 신호(패킷) 수신 시 로딩 완료. 클라이언트 간 준비 상태 동기화 목적
/// </summary>
public class NetworkLoadingStrategy : ILoadingStrategy
{
    // 외부에서 UniTask를 수동으로 완료시키기 위한 UniTaskCompletionSource
    // 서버 패킷 수신 시 WaitUntilReadyAsync 대기 작업을 재개하는 용도
    private UniTaskCompletionSource<bool> _serverReadyTcs = new UniTaskCompletionSource<bool>();

    /// <summary>
    /// 생성자
    /// 서버 패킷 수신을 위한 이벤트 핸들러 등록
    /// </summary>
    public NetworkLoadingStrategy()
    {
        // NullReferenceException 방지를 위한 유효성 검사
        if (GameSession.Instance?.Sender != null)
        {
            GameSession.Instance.Sender.OnPacketReceived += OnPacketReceived;
        }
    }

    /// <summary>
    /// 서버 준비 신호 비동기 대기
    /// </summary>
    public async UniTask WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        // OnPacketReceived에서 SetResult 호출 전까지 무한 대기
        // AttachExternalCancellation으로 CancellationToken 취소 시 즉시 대기 중단
        await _serverReadyTcs.Task.AttachExternalCancellation(cancellationToken);
    }

    /// <summary>
    /// 서버 패킷 수신 이벤트 핸들러
    /// </summary>
    /// <param name="packet">수신된 패킷 데이터</param>
    private void OnPacketReceived(Packet packet)
    {
        // 로딩 완료를 의미하는 특정 패킷 타입인지 확인
        if (packet.type.Contains(SceneConstants.PacketTypeLoad))
        {
            // 네트워크 스레드에서 직접 Unity API 호출 방지
            // MainThreadDispatcher를 통해 TCS 완료 호출을 메인 스레드에서 안전하게 실행
            // TrySetResult는 중복 호출에도 안전
            MainThreadDispatcher.Enqueue(() => _serverReadyTcs.TrySetResult(true));
        }
    }

    /// <summary>
    /// 이벤트 핸들러 해제
    /// 객체 파괴 시 호출. 미수행 시 메모리 누수 발생 가능
    /// </summary>
    public void Cleanup()
    {
        if (GameSession.Instance?.Sender != null)
        {
            GameSession.Instance.Sender.OnPacketReceived -= OnPacketReceived;
        }
    }
}