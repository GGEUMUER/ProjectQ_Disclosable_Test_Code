using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// 씬 로딩 프로세스 관리 컨트롤러
/// 로딩 UI 업데이트, 비동기 씬 로드, 로딩 전략(로컬/네트워크) 결정 담당
/// </summary>
public class LoadingController : MonoBehaviour
{
    // 로딩 UI 컴포넌트 참조
    // 인스펙터에서 할당. 로딩 상태와 진행률 표시 목적
    [SerializeField] private LoadingUI _loadingUI;
    
    // 최소 로딩 시간
    // 너무 빠른 로딩 완료 시 화면 깜빡임 방지 및 사용자 인지 시간 보장
    [SerializeField] private float _minimumLoadingTime = 2.0f;

    // 현재 로딩 전략
    // 싱글/멀티플레이 모드에 따른 로딩 방식 추상화
    private ILoadingStrategy _loadingStrategy;

    private void Start()
    {
        if (_loadingUI == null)
        {
            Logger.LogError("LoadingPresenter 참조가 없습니다.");
            return;
        }

        InitializeStrategy();
        
        // GameObject 파괴 시 자동 취소되는 CancellationToken 획득
        // 씬 전환, 게임 종료 등 비정상 상황에서 비동기 작업의 안전한 중단 목적
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        
        // 비동기 로딩 프로세스 시작
        // Forget()으로 'fire-and-forget' 호출. 백그라운드에서 독립 실행
        LoadProcessAsync(cancellationToken).Forget();
    }

    /// <summary>
    /// 로딩 전략 초기화
    /// 게임 모드(싱글/네트워크)에 따른 적절한 전략 선택
    /// </summary>
    private void InitializeStrategy()
    {
        if (SceneLauncher.IsSinglePlayer)
        {
            _loadingStrategy = new LocalLoadingStrategy();
            _loadingUI.SetStatusText("Initializing local data...");
        }
        else
        {
            _loadingStrategy = new NetworkLoadingStrategy();
            _loadingUI.SetStatusText("Connecting to server...");
        }
    }

    /// <summary>
    /// 비동기 로딩 프로세스 코어 로직
    /// 씬 로드, 전략별 추가 작업, 최소 로딩 시간 보장을 병렬 처리
    /// </summary>
    /// <param name="cancellationToken">비동기 작업 중단 토큰</param>
    private async UniTask LoadProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            float displayProgress = 0f;
            float startTime = Time.time;

            // 전략 작업 시작
            UniTask strategyTask = _loadingStrategy.WaitUntilReadyAsync(cancellationToken);

            // 2씬 로드 시작
            AsyncOperation sceneOp = SceneManager.LoadSceneAsync(SceneLauncher.NextSceneName);
            sceneOp.allowSceneActivation = false;

            // 루프 내에서 진행률 감시
            while (displayProgress < 1.0f)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool isSceneLoaded = sceneOp.progress >= 0.9f;
                bool isStrategyDone = strategyTask.Status == UniTaskStatus.Succeeded;
                bool isTimeMet = (Time.time - startTime) >= _minimumLoadingTime;

                // 실제 물리적 목표치 계산
                float targetValue = isStrategyDone ? (sceneOp.progress / 0.9f) : (sceneOp.progress / 0.9f) * 0.5f;
                
                // 모든 조건 충족 시 1.0으로 수렴
                if (isSceneLoaded && isStrategyDone && isTimeMet)
                    targetValue = 1.0f;

                // 부드러운 UI 갱신
                displayProgress = Mathf.MoveTowards(displayProgress, targetValue, Time.deltaTime * 0.5f);
                _loadingUI.UpdateProgress(displayProgress);

                // 로딩 완료 후 탈출
                if (displayProgress >= 1.0f && isSceneLoaded && isStrategyDone && isTimeMet)
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _loadingUI.SetStatusText("Ready!");
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            sceneOp.allowSceneActivation = true;
        }
        catch (OperationCanceledException)
        {
            // 로딩 취소 예외 처리
            // CancellationToken에 의한 정상 중단. 의도된 동작이므로 에러가 아님
            // 로딩 강제 중단 시 추가 정리 로직 구현 가능
            Logger.Log("Loading process was safely canceled.");
        }
        catch (Exception ex)
        {
            // 예기치 않은 로딩 오류 처리
            // 디버깅을 위한 심각한 오류 로그 기록
            Logger.LogError($"로딩 중 치명적 오류 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// 로딩 전략 리소스 정리
    /// GameObject 파괴 시 호출되어 메모리 누수 방지
    /// </summary>
    private void OnDestroy()
    {
        // 로딩 전략의 모든 리소스 안전하게 해제
        // 예: 네트워크 연결, 이벤트 구독 등
        _loadingStrategy?.Cleanup();
    }
}