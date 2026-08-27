using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatchState : IExecutableState
{
    public float smoothSpeed=10;
    public float moveSpeed=5;
    public void Enter(Character character)
    {
    }

    public void Execute(Character character)
    {
        if (character.smoothMove)
        {
            character.transform.localPosition = Vector3.Lerp(character.transform.localPosition, Vector3.zero, smoothSpeed * Time.deltaTime);
        }
        else
        {
            character.transform.localPosition = Vector3.MoveTowards(character.transform.localPosition, Vector3.zero, moveSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(character.transform.localPosition, Vector3.zero) < 0.01f)
        {
            character.transform.localPosition = Vector3.zero; // 스냅 고정
            if (character.moveEvnet)
            {
                character.moveEvnet = false;
                if (character.gameManager.ReturnCurrentPhase() is IBattleReady phase)
                {
                    phase.NotifyUnitArrived();
                    character.ChangeState("Idle");
                }
            }
        }
    }

    public void Exit(Character character)
    {
    }
}

