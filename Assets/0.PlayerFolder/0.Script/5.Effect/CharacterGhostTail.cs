using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine.Unity.Examples;

public class CharacterGhostTail : IState
{
    public CharacterGhostTailCorutine CHTC;
    // CharacterGhostTailCorutine을 불러서 실행시키는 쪽으로
    // 모노비헤이비어를 상속해야 스탑 코루틴을 사용할 수 있음
    // 해당 방면을 사용하지 않는 쪽으로 가도 되긴 하는데, 자원 소모율이 너무 커짐

    public void Enter(Character character)
    {
        CHTC = character.gameObject.GetComponent<CharacterGhostTailCorutine>();
        Debug.Log($"CharacterGhostTail in, {character.gameObject.name}");

        if (CHTC == null)
        {
            Debug.LogWarning($"CharacterGhostTailCorutine is Null, {character.gameObject.name}");
            return;
        }

        CHTC.Init(character.gameObject.transform, 
            character.gameObject.GetComponent<SkeletonGhost>());
        CHTC.PlayGhostAction();
    }


    public void Exit(Character character)
    {

    }
}
