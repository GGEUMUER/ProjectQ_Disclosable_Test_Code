using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
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
        if (current != null && current.Animation != null && current.Animation.Name == "Idle")
            return;

        skeleton.AnimationState.SetAnimation(0, "Idle", true);

    }
    public void Exit(Character character)
    {
    }
}
