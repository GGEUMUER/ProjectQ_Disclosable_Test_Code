using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DeathState : IState
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
        if (current != null && current.Animation != null && current.Animation.Name == "Death")
            return;

        current = skeleton.AnimationState.SetAnimation(0, "Death", false);
        UnityEngine.MonoBehaviour.Destroy(character.gameObject, current.Animation.Duration + .5f);
    }
    public void Exit(Character character)
    {
    }
}
