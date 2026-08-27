using UnityEngine;
using DG.Tweening;

/// <summary>
/// 개별 배경 요소를 특정 이징(Ease)을 사용하여 원래 위치로 부드럽게 이동시키는 인트로 애니메이션을 처리합니다.
/// 이 스크립트는 각 배경 요소가 정해진 위치로 이동하며 등장하는 시각적 연출을 담당합니다.
/// </summary>
public class BackgroundIntro : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _moveDuration = 1.0f;
    [SerializeField] private Ease _easeType = Ease.OutBack;

    /// <summary>
    /// 설정된 애니메이션 지속 시간과 이징 타입을 사용하여 배경 요소를 현재 위치에서 (0,0)으로 이동시키는 애니메이션을 시작합니다.
    /// 이 메서드는 배경 요소의 등장 연출을 실행하기 위해 호출됩니다.
    /// </summary>
    public void Play()
    {
        transform.DOLocalMove(Vector2.zero, _moveDuration).SetEase(_easeType);
    }

    private void OnDestroy()
    {
        // 모든 트윈을 즉시 정지
        transform.DOKill(); 
    }
}