using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class Attack2State : IState
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
        if (current != null && current.Animation != null && current.Animation.Name == "Attack2")
            return;

        skeleton.AnimationState.SetAnimation(0, "Attack2", true);

    }

    public void Exit(Character character)
    {
    }
}
