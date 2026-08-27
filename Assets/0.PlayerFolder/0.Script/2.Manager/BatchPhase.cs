using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class BatchPhase : IGameScenePhase
{
    private string playerId;
    private List<int> batchIndexes = new List<int>(){0,1,2};
    private GameSceneManager myManager;
    private UIManager uIManager;
    SmoothFollowCamera _myFollowCamera;
    Transform firstAttackBatchPos;
    Transform secAttackBatchPos;

    bool _hasSubmitted = false; // 중복 방지용 플레그 값
    float timmerTime = 5f;

    /// <summary>
    /// Uimanager, gameSceneManager 초기화, 패킷에 페이즈 스타트 알림
    /// </summary>
    public void Enter(GameSceneManager gameSceneManager)
    {
        /*
        myManager = gameSceneManager;
        uIManager = gameSceneManager.uIManager;

        _myFollowCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<SmoothFollowCamera>();
        firstAttackBatchPos = GameObject.Find("FirstAttackBatch").transform;
        secAttackBatchPos = GameObject.Find("SecondAttackBatch ").transform;

        Debug.Log("유닛 배치 상태 진입");
        GameSession.Instance.Sender.SendPacket("BatchPhase", "{}");*/

        myManager = gameSceneManager;
        uIManager = gameSceneManager.uIManager;

        firstAttackBatchPos = GameObject.Find("FirstAttackBatch").transform;
        secAttackBatchPos = GameObject.Find("SecondAttackBatch ").transform;

        _myFollowCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<SmoothFollowCamera>();
        // 아래 두 줄로 대체 (Find 쓰지 않기; 공백 오타 방지)
        var firstParent = myManager.FirstSecondDatas[0].batchPos;
        var secondParent = myManager.FirstSecondDatas[1].batchPos;

        Debug.Log("유닛 배치 상태 진입");
        GameSession.Instance.Sender.SendPacket("BatchPhase", "{}");
    }
    private int ToLocalBatchIndex(int serverIndex, int childCount)
    {
        // 일반적으로 0..(childCount-1) 범위면 그대로 사용
        if (serverIndex >= 0 && serverIndex < childCount) return serverIndex;

        // 서버가 11,12,13을 보내는 패턴(반대 진영 절대 인덱스) → 11→0, 12→1, 13→2
        if (serverIndex >= 11 && serverIndex <= 11 + (childCount - 1))
            return serverIndex - 11;

        // 그 외 값은 유효하지 않으므로 -1
        return -1;
    }
    private Transform SafeGetChild(Transform parent, int localIndex, string who, int side)
    {
        if (parent == null)
        {
            Debug.LogError($"[Batch] parent is NULL (side={side}) for {who}");
            return null;
        }
        int n = parent.childCount;
        if (localIndex < 0 || localIndex >= n)
        {
            Debug.LogError($"[Batch] INVALID localIndex={localIndex} (0..{n - 1}) | side={side} | {who} | parent={parent.name}");
            return null;
        }
        return parent.GetChild(localIndex);
    }

    public void UpdateLoop()
    { }

    /// <summary>
    /// 서버에 도착한 패킷 받아 타입별 처리
    /// </summary>
    public void OnPacketReceived(Packet packet)
    {
        switch (packet.type)
        {
            case "BatchStart": // 배치 스타트 수신 배치 캔버스 키기, 드래그 가능 설정,
            {
                Debug.Log($"[BatchStart] BatchStart 수신::::");

                uIManager.SetCurrentCanvas("BatchCanvas");
                uIManager.ReturnCurrentCanvas().SetActive(true);
                foreach (var unit in myManager.myData.units)
                {
                    unit.GetComponent<SnapPosition>().enabled = true;
                }

                if (myManager.isFirst)
                {
                    _myFollowCamera.CoroutineCamera(firstAttackBatchPos.position);
                }
                else
                {
                    _myFollowCamera.CoroutineCamera(secAttackBatchPos.position);
                }
                uIManager.ReturnTimerBar().SetTimer(timmerTime, timmerTime);
                    
                break;
            }
            case "BatchEnd":
            {
                 Debug.Log("[BatchEnd] :: 전송 ");
                 SubmitBatch();
                 break;
            }
            case "UnitsBatchIndex":
            {
                var data = JsonConvert.DeserializeObject<UnitsBatchIndex>(packet.payload,
                    JsonSettings.CamelCaseSettings);

                Debug.Log("UnitsBatchIndex 수신");
                ApplyBatchIndexes(data);
                //myManager.SetPhase(new BattlePhase());
                break;
            }
            case "BattlePhase":
            {
                Debug.Log("BattlePhase 진입");
                myManager.SetPhase(new BattlePhase());
                break;
            }

                /*
                 * 
            case "TimerEnd":
            {
                Debug.Log("타이머 엔드 수신!!");
                StairDown();
                break;
            }
                 */
        }
    }
    /// <summary>
    /// 서버로 받아온 패킷 페이로드 중, 유닛 배치 인덱스를 클라에 적용.
    /// </summary>
    /// <param name="data"></param>
    private void ApplyBatchIndexes(UnitsBatchIndex data)
    {
        // 내/상대 부모
        Transform myParent = myManager.myData.batchPos;
        Transform enemyParent = myManager.enemyData.batchPos;

        int mySide = myManager.isFirst ? 0 : 1;
        int enemySide = 1 - mySide;

        // 길이 안전
        int myN = Mathf.Min(myManager.myData.units.Count, data.MyUnitIndex?.Count ?? 0);
        int enemyN = Mathf.Min(myManager.enemyData.units.Count, data.OpponentIndex?.Count ?? 0);

        // 내 유닛
        for (int i = 0; i < myN; i++)
        {
            int localIdx = myManager.MapServerIndexToLocal(mySide, data.MyUnitIndex[i]);
            var row = myManager.SafeBatchRow(mySide, localIdx, $"MY[{i}] serverIdx={data.MyUnitIndex[i]}");
            if (row == null) continue;

            var go = myManager.myData.units[i];
            go.GetComponent<SnapPosition>().enabled = false;
            go.transform.SetParent(row, false);
            go.transform.localPosition = Vector3.zero;

            var ch = go.GetComponent<Character>();
            ch.smoothMove = false;
            ch.moveEvnet = true;
        }

        // 상대 유닛
        for (int i = 0; i < enemyN; i++)
        {
            int localIdx = myManager.MapServerIndexToLocal(enemySide, data.OpponentIndex[i]);
            var row = myManager.SafeBatchRow(enemySide, localIdx, $"ENEMY[{i}] serverIdx={data.OpponentIndex[i]}");
            if (row == null) continue;

            var go = myManager.enemyData.units[i];
            go.GetComponent<SnapPosition>().enabled = false;
            go.transform.SetParent(row, false);
            go.transform.localPosition = Vector3.zero;

            var ch = go.GetComponent<Character>();
            ch.smoothMove = false;
            ch.moveEvnet = true;
        }

        // 6) (선택) 좌/우 기준 위치 보정 (자식 1을 중앙으로 보고 0/2를 좌우로 옮기는 로직)
        ChangeFirstBatch();

        /*
        List<int> myBatchIndexes;
        List<int> enemyBatchIndexes;
        if (data.isFirst)
        {
            myBatchIndexes = data.MyUnitIndex;
            enemyBatchIndexes = data.OpponentIndex;
        }
        else
        {
            myBatchIndexes = data.MyUnitIndex;
            enemyBatchIndexes = data.OpponentIndex;
        }

        Debug.Log($"[enemyBatchIndexes]: {string.Join(",", enemyBatchIndexes)} enemy batchPos count: {myManager.enemyData.batchPos.childCount}");

        /*
        ChangeFirstBatch();

        for (int i =0; i< myManager.myData.units.Count; i++)
        {
            myManager.myData.units[i].transform.parent = 
                myManager.myData.batchPos.GetChild(myBatchIndexes[i]);
        }
        for(int i =0; i<myManager.enemyData.units.Count; i++)
        {
            myManager.enemyData.units[i].transform.parent = 
                myManager.enemyData.batchPos.GetChild(enemyBatchIndexes[i]);
        }

        for(int i =0; i < myManager.myData.units.Count;i++)
        {
            myManager.myData.units[i].GetComponent<SnapPosition>().enabled = false;
            myManager.myData.units[i].transform.localPosition = Vector3.zero;
        }

        for (int i = 0; i < myManager.enemyData.units.Count; i++)
        {
            myManager.enemyData.units[i].GetComponent<SnapPosition>().enabled = false;
            myManager.enemyData.units[i].transform.localPosition = Vector3.zero;
        }*/

        /*
        for (int i = 0; i < myManager.myData.units.Count; i++)
        {
            if (myBatchIndexes[i] == 13) myBatchIndexes[i] = 2;
            if (myBatchIndexes[i] == 12) myBatchIndexes[i] = 1;
            if (myBatchIndexes[i] == 11) myBatchIndexes[i] = 0;
            Debug.Log($"[enemyBatchIndexes]: {myBatchIndexes[i]}");

            myManager.myData.units[i].transform.parent = myManager.myData.batchPos.GetChild(myBatchIndexes[i]);
        }
        for (int i = 0; i < myManager.enemyData.units.Count; i++)
        {
            if (enemyBatchIndexes[i] == 13) enemyBatchIndexes[i] = 2;
            if (enemyBatchIndexes[i] == 12) enemyBatchIndexes[i] = 1;
            if (enemyBatchIndexes[i] == 11) enemyBatchIndexes[i] = 0;
            Debug.Log($"[enemyBatchIndexes]: {enemyBatchIndexes[i]}");

            myManager.enemyData.units[i].transform.parent = myManager.enemyData.batchPos.GetChild(enemyBatchIndexes[i]);
        }

        ChangeFirstBatch();

        for (int i = 0; i < myManager.myData.units.Count; i++)
        {
            myManager.myData.units[i].GetComponent<SnapPosition>().enabled = false;
            myManager.myData.units[i].transform.localPosition = Vector3.zero;
            myManager.enemyData.units[i].transform.localPosition = Vector3.zero;
        }*/
    }

    /*

    void UpdateTimerUI(SelectionTimePayload data)
    {
        uIManager.ReturnTimerBar().SetTimer(data.durationTime,data.remainingTime);
    }
    */
    void SubmitBatch()
    {
        if(_hasSubmitted) return;
        _hasSubmitted = true;

        Debug.Log("[SubmitBatch]:");
        GameSession.Instance.Sender.SendPacket("SetComplete", batchIndexes);
    }

    public void ChangeBatch(int selectedIndex,int targetIndex)
    {
        int index = selectedIndex;
        batchIndexes[selectedIndex] = targetIndex;
        batchIndexes[targetIndex] = index;
    }
    /// <summary>
    /// 패킷에서 받아온 UnitsIndexespayload를 클라에 적용
    /// </summary>
    /// <param name="data"></param>
    /// 
    /*
    public void MoveLastBatch(UnitsIndexespayload data)
    {
        List<int> myBatchIndexes;
        List<int> enemyBatchIndexes;
        if (data.isFirst)
        {
            myBatchIndexes = data.firstPlayerIndex;
            enemyBatchIndexes = data.secondPlayerIndex;
        }
        else
        {
            myBatchIndexes = data.secondPlayerIndex;
            enemyBatchIndexes = data.firstPlayerIndex;
        }
        
        for (int i = 0; i < myManager.myData.units.Count; i++)
        {
            myManager.myData.units[i].transform.parent =  myManager.myData.batchPos.GetChild(myBatchIndexes[i]);
        }
        for (int i = 0; i <  myManager.enemyData.units.Count; i++)
        {
            myManager.enemyData.units[i].transform.parent =  myManager.enemyData.batchPos.GetChild(enemyBatchIndexes[i]);
        }
        
        ChangeFirstBatch();
        
        for (int i = 0; i <  myManager.myData.units.Count; i++)
        {
            myManager.myData.units[i].GetComponent<SnapPosition>().enabled = false;
            myManager.myData.units[i].transform.localPosition=Vector3.zero;
            myManager.enemyData.units[i].transform.localPosition=Vector3.zero;
        }
    }*/

    public void ChangeFirstBatch()
    {
        var a = myManager.FirstSecondDatas[0].batchPos;
        var b = myManager.FirstSecondDatas[1].batchPos;

        if (a == null || b == null) { Debug.LogWarning("[Batch] ChangeFirstBatch: parent null"); return; }
        if (a.childCount < 3 || b.childCount < 3) { Debug.LogWarning("[Batch] ChangeFirstBatch: need >=3 children"); return; }

        float dx = myManager.grid.cellSize.x;

        // First side: 1을 가운데로 보고 0/2를 좌우로
        a.GetChild(0).position = a.GetChild(1).position + Vector3.right * dx;
        a.GetChild(2).position = a.GetChild(1).position - Vector3.right * dx;

        // Second side: 반대
        b.GetChild(2).position = b.GetChild(1).position + Vector3.right * dx;
        b.GetChild(0).position = b.GetChild(1).position - Vector3.right * dx;
        /*
        myManager.FirstSecondDatas[0].batchPos.GetChild(0).position
            = myManager.FirstSecondDatas[0].batchPos.GetChild(1).position + Vector3.right*myManager.grid.cellSize.x;
        myManager.FirstSecondDatas[0].batchPos.GetChild(2).position = 
            myManager.FirstSecondDatas[0].batchPos.GetChild(1).position - Vector3.right*myManager.grid.cellSize.x;
        
        myManager.FirstSecondDatas[1].batchPos.GetChild(2).position
            = myManager.FirstSecondDatas[1].batchPos.GetChild(1).position + Vector3.right*myManager.grid.cellSize.x;
        myManager.FirstSecondDatas[1].batchPos.GetChild(0).position = 
            myManager.FirstSecondDatas[1].batchPos.GetChild(1).position - Vector3.right*myManager.grid.cellSize.x;
        */

    }
}
