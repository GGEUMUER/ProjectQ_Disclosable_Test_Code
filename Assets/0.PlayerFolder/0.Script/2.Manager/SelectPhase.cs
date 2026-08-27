using Newtonsoft.Json;
using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Vector3 = UnityEngine.Vector3;
/// 너무 돌아서 생각한 것 같음. 일단 쉬고 다시 생각해보자.
public class SelectPhase : IGameScenePhase
{
    private string playerId;
    private GameSceneManager myManager;
    private UIManager uIManager;
    int _insChecker = 1;
    SmoothFollowCamera _myFollowCamera;

    public void Enter(GameSceneManager gameSceneManager)
    {
       myManager = gameSceneManager;
       uIManager = gameSceneManager.uIManager;
       uIManager.SetCurrentCanvas("SelectCanvas");
       uIManager.ReturnCurrentCanvas().SetActive(true);

        _myFollowCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<SmoothFollowCamera>();


        Debug.Log("카드 선택 상태 진입");
        GameSession.Instance.Sender.SendPacket("PhaseStart", "{}");
    }

    public void UpdateLoop()
    {
        
    }
    /// <summary>
    /// 패킷을 받았을 때 패킷을 스위치 케이스 문으로 나눠 진행하도록 만들어주는 구문
    /// </summary>
    /// <param name="packet"></param>
    public void OnPacketReceived(Packet packet)
    {
        switch (packet.type)
        {
            case "PickFirstCards":
            {
                var data = JsonConvert.DeserializeObject<PickFirstCards>(packet.payload, JsonSettings.CamelCaseSettings);
                Debug.Log($"[PickFirstCards]{data.Step}: 현재 Step | 0일 시 정상");

                myManager.SetData(data.IsMyTurn);
                Debug.Log("들어왔음.");
                Debug.Log($"{myManager.myData} || {myManager.enemyData}");

                uIManager.ReturnCurrentCanvas().SetActive(true);
                var cardSelector = uIManager.ReturnCurrentCanvas().GetComponent<CardSelector>();
                cardSelector.InstantiateFirstPick(data.AllTypes, data.IsMyTurn, data.Step);

                    //InstantCardSelectionUI(data);
                break;
            }
            case "UpdateSelectedCard":
            {
                var data = JsonConvert.DeserializeObject<UpdateSelectedCard>(packet.payload, JsonSettings.CamelCaseSettings);
                Debug.Log($"[UpdateSelectedCard]{data.Step}: 현재 Step | 1일 시(선공) 또는 2일 시(후공) 정상 @@ 선택된 카드: {data.UnitType}");

                uIManager.ReturnCurrentCanvas().
                GetComponent<CardSelector>().
                ReflectSelectedCard(data.UnitType, data.IsOwner,data.Step);

                if (data.Step == 1 || data.Step == 2)
                {
                    if (data.IsOwner)
                    {
                        // 내 유닛 생성
                        GameObject obj = myManager.InstantUnit(data.UnitType, 1, "Player");
                        obj.transform.parent = myManager.myData.batchPos.GetChild(0); // 첫 번째 위치
                        _myFollowCamera.CoroutineCamera(obj.transform.parent.parent.position);
                        obj.transform.localPosition = Vector3.zero;
                        var sa = obj.GetComponent<SkeletonAnimation>();
                        SpineUtil.SetSideFacing(sa, isLeftSide: myManager.isFirst);
                        obj.GetComponent<SnapPosition>().enabled = false;
                        obj.GetComponent<Character>().gameManager = myManager;
                        myManager.myData.units.Add(obj);
                    }
                    else
                    {
                        // 상대 유닛 생성
                        GameObject obj = myManager.InstantUnit(data.UnitType, 1, "Enemy");
                        obj.transform.parent = myManager.enemyData.batchPos.GetChild(0);
                        //_myFollowCamera.CoroutineCamera(obj.transform.parent.position);
                        obj.transform.localPosition = Vector3.zero;
                        var sa = obj.GetComponent<SkeletonAnimation>();
                        SpineUtil.SetSideFacing(sa, isLeftSide: !myManager.isFirst);
                        obj.GetComponent<SnapPosition>().enabled = false;
                        obj.GetComponent<Character>().gameManager = myManager;
                        myManager.enemyData.units.Add(obj);
                    }
                }

                break;
            }
            case "DealCards":
            {
                var data = JsonConvert.DeserializeObject<DealCards>(packet.payload, JsonSettings.CamelCaseSettings);
                Debug.Log($"[DealCards]{data.Step}: 현재 Step | 3, 5일 시 정상 @@ 내 수신 카드: {data.MyCard} @@ 상대 수신 카드: {data.OpponentCard}");

                uIManager.ReturnCurrentCanvas().
                        GetComponent<CardSelector>().
                        ShowDealtCards(data.MyCard, data.OpponentCard, data.Step);
                break;
            }
            case "PickTwoCards":
            {
                var data = JsonConvert.DeserializeObject<PickTwoCards>(packet.payload, JsonSettings.CamelCaseSettings);
                Debug.Log($"[PickTwoCards]{data.Step}: 현재 Step | 3, 5일 시 정상 @@ 남은 두 카드: {data.Units[0]} || {data.Units[1]}");

                uIManager.ReturnCurrentCanvas().
                GetComponent<CardSelector>().
                ShowRemainingCards(data.Units, data.Step);
                break;
            }
            case "UpdateBothUnits":
            {
                var data = JsonConvert.DeserializeObject<UpdateBothUnits>(packet.payload, JsonSettings.CamelCaseSettings);
                Debug.Log($"[UpdateBothUnits]{data.Step}: 현재 Step | 4, 6일 시 정상 @@ 나: {data.MyUnit.Type} @@ 적: {data.OpponentUnit.Type}");
                EndStep(data);

                break;
            }

            case "TimerUpdate":
            {
                var data = JsonConvert.DeserializeObject<SelectionTimePayload>(packet.payload,
                    JsonSettings.CamelCaseSettings);
                Debug.Log($"타이머 갱신 수신: {data.remainingTime}");

                UpdateTimerUI(data);
                break;
            }

            case "BatchPhase":
            {
                Debug.Log("배치 단계로");
                uIManager.ReturnCurrentCanvas().SetActive(false);
                myManager.SetPhase(new BatchPhase());

                break;
            }

            case "BattlePhase":
            {
                Debug.Log("공격 단계로");
                myManager.SetPhase(new BattlePhase());
                break;
            }
        }
    }

    // 援ъ“ 蹂寃?
    /// <summary>
    /// 한 스텝 끝났을 때 불려지는 함수.
    /// 적과 내 유닛의 선택 정보를 패킷으로 받아와 생성을 하는 역할을 함.
    /// 임시적으로 0, 1, 2 순으로 배치. 배치 변경은 다음 배치 페이스에서 진행됨.
    /// </summary>
    /// <param name="data"></param>
    void EndStep(UpdateBothUnits data)
    {
        // 디버그 라인
        Debug.Log($"batchPos Count: {myManager.myData.batchPos.childCount}");

        if (myManager == null)
        {
            Debug.LogError("myManager is null");
            return;
        }
        if (myManager.myData == null)
        {
            Debug.LogError("myManager.myData is null");
            return;
        }
        if (myManager.myData.batchPos == null)
        {
            Debug.LogError("myManager.myData.batchPos is null");
            return;
        }

        foreach (UnityEngine.Transform t in myManager.myData.batchPos)
            Debug.Log($"배치 위치 있음: {t.name}");


        Unitpayload myUnit = data.MyUnit;
        Unitpayload enemyUnit = data.OpponentUnit;

        //uIManager.ReturnCurrentCanvas().SetActive(false);

        GameObject obj = myManager.InstantUnit(myUnit.Type, myUnit.Level - 1, "Player");

        UnityEngine.Transform myTempPos = myManager.myData.batchPos.GetChild(_insChecker);
        obj.transform.parent = myTempPos;
        obj.GetComponent<SnapPosition>().nearest = myTempPos;
        obj.GetComponent<SnapPosition>().gameSceneManager = myManager;
        obj.GetComponent<Character>().gameManager = myManager;
        obj.transform.localPosition = Vector3.zero;

        foreach (UnityEngine.Transform child in myManager.myData.batchPos)
        {
            obj.GetComponent<SnapPosition>().snapPositions.Add(child);
        }

        obj.GetComponent<SnapPosition>().enabled = false;
        var mySa = obj.GetComponent<SkeletonAnimation>();
        SpineUtil.SetSideFacing(mySa, isLeftSide: myManager.isFirst);

        //if(!myManager.isFirst)
        //{
        //    var mySa = obj.GetComponent<SkeletonAnimation>();
        //    SpineUtil.SetSideFacing(mySa, isLeftSide: myManager.isFirst);
        //}
        myManager.myData.units.Add(obj);

        GameObject obj2 = myManager.InstantUnit(enemyUnit.Type, enemyUnit.Level - 1, "Enemy");

        UnityEngine.Transform enemyTempPos = myManager.enemyData.batchPos.GetChild(_insChecker);
        obj2.transform.parent = enemyTempPos;
        obj2.GetComponent<SnapPosition>().enabled = false;
        obj2.GetComponent<Character>().gameManager = myManager;
        obj2.transform.localPosition = Vector3.zero;

        //if (myManager.isFirst)
        //{
        //    var enemySa = obj2.GetComponent<SkeletonAnimation>();
        //    SpineUtil.SetSideFacing(enemySa, isLeftSide: !myManager.isFirst);
        //}
        var enemySa = obj2.GetComponent<SkeletonAnimation>();
        SpineUtil.SetSideFacing(enemySa, isLeftSide: !myManager.isFirst);
        myManager.enemyData.units.Add(obj2);

        _insChecker++;
        GameSession.Instance.Sender.SendPacket("NextProgress", "{}");
    }

    //湲곗〈 肄붾뱶
    /*
    void EndProgress(SpawnDatapayload data)
    {
        Unitpayload myUnit;
        Unitpayload enemyUnit;
        if (data.isFirst)
        {
            myUnit = data.firstUnit;
            enemyUnit = data.secondUnit;
        }
        else
        {
            myUnit = data.secondUnit;
            enemyUnit = data.firstUnit;
        }
        
        if (data.progress % 2 == 0)
        {
            uIManager.ReturnCurrentCanvas().SetActive(false);
            GameObject obj = myManager.InstantUnit(myUnit.type, myUnit.level,"Player");
            obj.transform.parent =myManager.myData.batchPos.GetChild(myUnit.batchIndex);
            obj.GetComponent<SnapPosition>().nearest = myManager.myData.batchPos.GetChild(myUnit.batchIndex);
            obj.GetComponent<SnapPosition>().gameSceneManager = myManager;
            obj.GetComponent<Character>().gameManager = myManager;
            obj.transform.localPosition = Vector3.zero;
            foreach (Transform child in myManager.myData.batchPos)
            {
                obj.GetComponent<SnapPosition>().snapPositions.Add(child);
            }
            obj.GetComponent<SnapPosition>().enabled = false;
            if (!myManager.isFirst)
            {
                obj.GetComponent<SpriteRenderer>().flipX = true;
            }
            myManager.myData.units.Add(obj);
            
            GameObject obj2 = myManager.InstantUnit(enemyUnit.type, enemyUnit.level,"Enemy");
            obj2.transform.parent = myManager.enemyData.batchPos.GetChild(enemyUnit.batchIndex);
            obj2.GetComponent<SnapPosition>().enabled = false;
            obj2.GetComponent<Character>().gameManager = myManager;
            obj2.transform.localPosition = Vector3.zero;
            if (myManager.isFirst)
            {
                obj2.GetComponent<SpriteRenderer>().flipX = true;
            }
            myManager.enemyData.units.Add(obj2);
        }
        GameSession.Instance.Sender.SendPacket("NextProgress", "{}");
    }*/

    void UpdateTimerUI(SelectionTimePayload data)
    {
        uIManager.ReturnTimerBar().SetTimer(data.durationTime,data.remainingTime);
    }


}

static class SpineUtil
{    public static void SetSideFacing(SkeletonAnimation sa, bool isLeftSide)
    {
        if (!sa) return;
        var t = sa.transform;
        var s = t.localScale;
        float abs = Mathf.Abs(s.x);
        s.x = isLeftSide ? +abs : -abs;
        t.localScale = s;
    }
}
