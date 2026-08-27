using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveState : IState
{
    public SkeletonAnimation skeleton;
    string _curAnimPrint;

    public void Enter(Character character)
    {
        skeleton = character.gameObject.GetComponent<SkeletonAnimation>();
        if (skeleton == null)
        {
            return;
        }

        var current = skeleton.AnimationState.GetCurrent(0);
        if (current != null && current.Animation != null && current.Animation.Name == "Walk")
            return;

        skeleton.AnimationState.SetAnimation(0, "Walk", true);
    }
    public void Exit(Character character)
    {
    }
}
//public class MoveState : IState
//{
//    private UnitMoveCommand data;
//    public void Enter(Character character)
//    {
//        data = ((UnitMoveCommand)character.data);
//        MoveToIndex(data.targetIndex,data.ticksUntilArrival,character);
//        Debug.Log("Enter Move");
//    }
//    /// <summary>
//    /// 틱 기반 대상을 향해 이동하는 함수. 
//    /// </summary>
//    /// <param name="targetIndex">유닛이 이동할 타겟 인덱스</param>
//    /// <param name="ticksUntilArrival">이동 시킬 캐릭터의 도착 예정 틱</param>
//    /// <param name="character">이동 시킬 캐릭터</param>
//    public void MoveToIndex(int targetIndex, int ticksUntilArrival,Character character)
//    {
//        float duration = (ticksUntilArrival-character.tick)* character.tickDuration;
//        character.transform.parent = character.gameManager.GetRowPositions().GetChild(targetIndex);
//        Vector3 targetWorldPos = Vector3.zero;
//        character.StopAllCoroutines();
//        character.StartCoroutine(MoveRoutine(targetWorldPos, duration, character));
//    }
//    /// <summary>
//    /// 목표 위치로 이동시키는 코루틴
//    /// </summary>
//    /// <param name="target">이동시킬 대상의 위치 파라메터(부모의 로컬 원점)</param>
//    /// <param name="duration">이동에 걸릴 총 시간</param>
//    /// <param name="character">이동시킬 캐릭터</param>
//    /// <returns></returns>
//    private IEnumerator MoveRoutine(Vector3 target, float duration, Character character)
//    {
//        Vector3 start = character.transform.localPosition;
//        float distance = Vector3.Distance(start, target);
//        float speed = distance / duration; // 일정한 속도 계산

//        while (Vector3.Distance(character.transform.localPosition, target) > 0.01f)
//        {
//            character.transform.localPosition = Vector3.MoveTowards(
//                character.transform.localPosition,
//                target,
//                speed * Time.deltaTime
//            );
//            yield return null;
//        }

//        character.transform.localPosition = target;
//        character.ChangeState("Idle");
//    }
//    public void Exit(Character character)
//    {
//        Debug.Log("Exit Move");
//    }
//}
