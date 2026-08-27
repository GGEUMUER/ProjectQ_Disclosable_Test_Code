using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class AttackState : IState
{
    public SkeletonAnimation skeleton;
    string _curAnimPrint;
    
    public void Enter(Character character)
    {
        skeleton = character.gameObject.GetComponent<SkeletonAnimation>();
        if (skeleton == null)
        {
            //Debug.LogWarning("SkeletonAnimation 컴포넌트 X");
            return;
        }

        var current = skeleton.AnimationState.GetCurrent(0);
        if (current != null && current.Animation != null && current.Animation.Name == "Attack")
            return;

        skeleton.AnimationState.SetAnimation(0, "Attack", true);

    }

    public void Exit(Character character)
    {

    }
}


//public class AttackState : IState
//{  
//    private UnitAttackCommand data;
//    private Character target;
//    /// <summary>
//    /// 받아온 정보 클라로 적용
//    /// </summary>
//    public void Enter(Character character)
//    {
//        data = ((UnitAttackCommand)character.data);
//        target = character.gameManager.GetRowPositions().GetChild(data.hitIndex).GetChild(0).GetComponent<Character>();
//        target.nowHP = data.nowHP;
//        target.stat.maxHP = data.maxHP;

//        target.nowMP = data.hitNowMP;
//        target.stat.maxMP = data.hitMaxMP;

//        character.stat.maxMP = data.attackMaxMP;
//        character.nowMP = data.attackNowMP;

//        target.UpdateHPSlider();
//        target.UpdateMPSlider();
//        character.UpdateMPSlider();
//    }
//    public void Exit(Character character)
//    {
//        Debug.Log("Exit Attack");
//    }
//}
