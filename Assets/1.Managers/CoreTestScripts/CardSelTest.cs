using Core;
using Core.SinglePlay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CardTest
{
    int _userIndex = 0;
    public SinglePlayCore single = new SinglePlayCore();
    
    public void Init() => single.Init();

    public bool EachStepLog()
    {
        //if(single.Step == SinglePlayStep.End) return false;
        //var before = single.Step;
        //bool advanced = false;
        //bool check = true;
        //var stepRaw = (int)single.Step;
        //var stepName = Enum.GetName(typeof(SinglePlayStep), single.Step) ?? "<UNKNOWN>";
        //var isDefined = Enum.IsDefined(typeof(SinglePlayStep), single.Step);
        //Debug.Log($"[SinglePlayStep] Step raw={stepRaw}, name={stepName}, isDefined={isDefined}");
        //switch (single.Step)
        //{
        //    case SinglePlayStep.Start:
        //    {
        //        check = single.StartCardSelect(out var firstPlayer);
        //        Debug.Log($"[Start][StartCardSelect] (user) Check = {check} (firstPlayer) = {firstPlayer}");
        //        break;
        //    }
        //    case SinglePlayStep.FirstPlayerFirstPick:
        //    {
        //        if (single.FirstPlayer == 0)
        //        {
        //            check = single.TryFirstPickUser(_userIndex, out int id);
        //            Debug.Log($"[FirstPlayerFirstPick][TryFirstPickUser] (user) Check = {check} (Userid) = {id}");
        //        }
        //        else
        //        {
        //            check = single.TryFirstPickComputer(out int id);
        //            Debug.Log($"[FirstPlayerFirstPick][TryFirstPickUser] (NPC) Check = {check} (Userid) = {id}");
        //        }
        //        break;
        //    }
        //    case SinglePlayStep.SecondPlayerFirstPick:
        //    {
        //        if (single.FirstPlayer == 1)
        //        {
        //            check = single.TryFirstPickUser(_userIndex, out int id);
        //            Debug.Log($"[SecondPlayerFirstPick][TryFirstPickUser] (NPC) Check = {check} (Userid) = {id}");
        //        }
        //        else
        //        {
        //            check = single.TryFirstPickComputer(out int id);
        //            Debug.Log($"[SecondPlayerFirstPick][TryFirstPickUser] (User) Check = {check} (Userid) = {id}");
        //        }
        //        break;
        //    }
        //    case SinglePlayStep.FirstDeal:
        //    {
        //        check = single.TryShuffle(out var userPublic, out var computerPublic);
        //        Debug.Log($"[FirstDeal][TryShuffle] Check = {check} (userPublic) = {string.Join(",", userPublic)} (computerPublic) = {string.Join(",", computerPublic)}");
        //        break;
        //    }
        //    case SinglePlayStep.FirstRemovePick:
        //    {
        //        check = single.TryRemoveCard(_userIndex, out var typeA, out var typeB);
        //        Debug.Log($"[FirstRemovePick][TryRemoveCard] (User) Check = {check} TypeA = {typeA} typeB = {typeB}");
        //        break;
        //    }
        //    case SinglePlayStep.SecondPick:
        //    {
        //        check = single.TryPickRemainCard(_userIndex, out int userId, out int computerId);
        //        Debug.Log($"[SecondPick][TryPickRemainCard] Check = {check} (userId) = {userId} (computerId) = {computerId}");
        //        break;
        //    }
        //    case SinglePlayStep.SecondDeal:
        //    {
        //        check = single.TryShuffle(out var userPublic, out var computerPublic);
        //        Debug.Log($"[SecondDeal][TryShuffle] Check = {check} (userPublic) = {string.Join(",", userPublic)} (computerPublic) = {string.Join(",", computerPublic)}");
        //        break;
        //    }
        //    case SinglePlayStep.SecondRemovePick:
        //    {
        //        check = single.TryRemoveCard(_userIndex, out var typeA, out var typeB);
        //        Debug.Log($"[SecondRemovePick][TryRemoveCard] (User) Check = {check} TypeA = {typeA} typeB = {typeB}");
        //        break;
        //    }
        //    case SinglePlayStep.ThirdPick:
        //    {
        //        check = single.TryPickRemainCard(_userIndex, out int userId, out int computerId);
        //        Debug.Log($"[ThirdPick][TryPickRemainCard] Check = {check} (userId) = {userId} (computerId) = {computerId}");
        //        break;
        //    }
        //    case SinglePlayStep.Batch:
        //    {
        //        check = single.TryGetComputerBatch(out var units);
        //        Debug.Log($"[Batch][TryGetComputerBatch] Check = {check} (units) = {string.Join(",", units)}");
        //        break;
        //    }
        //    default:
        //        Debug.Log("Default In?");
        //        break;
        //}
        //var after = single.Step;
        //advanced = (after != before);

        //if(!advanced)
        //{
        //    Debug.LogError("[SinglePlayStep Error] Non Stepped");
        //    return false;
        //}

        //if(single.Step == SinglePlayStep.End)
        //{
        //    check = single.TryWinPlayerCheck((sbyte)_userIndex, out var roundResult);
        //    Debug.Log($"[End][TryWinPlayerCheck] winner = {roundResult.GameResult} userWinCount = {roundResult.UserWinCount} computerWinCount = {roundResult.ComputerWinCount}");
        //    return false;
        //}
        return true;
    }
}
public class CardSelTest : MonoBehaviour
{
    public CardTest _test;

    void Awake()
    {
        _test = new CardTest();
        _test.Init();
    }


    //IEnumerator Start()
    //{
    //    _test = new CardTest();
    //    _test.Init();

    //    while (true)
    //    {
    //        bool more = _test.EachStepLog();

    //        if (!more)
    //        {
    //            yield return null;
    //            continue;
    //        }
    //        yield return null;
    //    }
    //}
}

