using UnityEngine;
using Spine.Unity;
using DG.Tweening;

public class LobbyCharacter : MonoBehaviour
{
    public SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    public string idleAnim = "Idle";
    public string walkAnim = "Walk";

    void OnDestroy()
    {
        DOTween.Kill(this);
    }

    // 이동 메서드
    public Tween MoveToTarget(Vector3 targetPos, float duration)
    {
        // 방향 전환
        skeletonAnimation.Skeleton.ScaleX = (targetPos.x > transform.position.x) ? 1f : -1f;

        // 걷기 애니메이션
        skeletonAnimation.AnimationState.SetAnimation(0, walkAnim, true);

        // DOTween 이동 및 완료 처리
        return transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear) // 일정한 속도로 이동
            .SetLink(gameObject)
            .OnComplete(() => {
                // 도착 시 대기 애니메이션으로 전환
                skeletonAnimation.AnimationState.SetAnimation(0, idleAnim, true);
            });
    }
}