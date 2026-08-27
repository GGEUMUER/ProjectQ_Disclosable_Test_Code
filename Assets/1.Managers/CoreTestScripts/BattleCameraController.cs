using UnityEngine;
using Cinemachine; // v3 버전 기준 네임스페이스 (구버전은 Cinemachine)

public class BattleCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    // 인터페이스 대신 구체 클래스 타입을 사용함
    [SerializeField] private CinemachineVirtualCamera _battleCamera; 
    [SerializeField] private CinemachineVirtualCamera _batchCamera;
    private const int ActivePriority = 20;
    private const int InactivePriority = 10;

    void Start()
    {
        _battleCamera.Priority = ActivePriority;
        _batchCamera.Priority = InactivePriority;
    }

    /// <summary>
    /// 배치 단계 진입 시 호출: 플레이어 진영 클로즈업
    /// </summary>
    public void SwitchToBatchView()
    {
        _batchCamera.Priority = ActivePriority;
        _battleCamera.Priority = InactivePriority;
        // 브레인이 자동으로 우선순위가 높은 BatchView로 부드럽게 전환함
    }

    /// <summary>
    /// 배치 종료/전투 시작 시 호출: 전체 전장 뷰로 복귀
    /// </summary>
    public void SwitchToBattleView()
    {
        _batchCamera.Priority = InactivePriority;
        _battleCamera.Priority = ActivePriority;
    }

    /// <summary>
    /// 예외 처리: 라운드 강제 종료 시 즉시 시점 초기화
    /// </summary>
    public void ResetCameraImmediate()
    {
        _batchCamera.Priority = InactivePriority;
        _battleCamera.Priority = ActivePriority;
        // 즉시 전환이 필요할 경우 브레인의 Blending 설정을 일시적으로 0으로 조절 가능함
    }
}