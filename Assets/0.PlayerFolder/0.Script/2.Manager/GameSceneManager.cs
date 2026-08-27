using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CharacterData
{
    public string types;
    public List<GameObject> prefabs;
}
[System.Serializable]
public class FirstSecond
{
    public Transform batchPos;
    public List<GameObject> units;
}
public class GameSceneManager : MonoBehaviour
{
    public UIManager uIManager;
    private int receiverPort;
    public List<CharacterData> CharacterObjDatas = new List<CharacterData>();
    private Dictionary<string, List<GameObject>> characterObjectsDict = new();
    private string playerId;
    public FirstSecond[] FirstSecondDatas = new FirstSecond[2];
    private IGameScenePhase currentPhase;
    [HideInInspector]
    public bool isFirst;
    [Header("Check unit data")]
    public FirstSecond myData;
    public FirstSecond enemyData;
    
    public  int halfWidth = 7; // 왼쪽/오른쪽 범위
    public  Grid grid;
    private Transform rowPositions;
    private void Awake()
    {
    }

    void Start()
    {
        receiverPort = GameSession.Instance.ReceiverPort;
        playerId = GameSession.Instance.PlayerId; 
        GameSession.Instance.Sender.OnPacketReceived += OnPacketReceived;

        Debug.Log($"GameSceneManager 초기화 완료 - 포트 {receiverPort} 수신 대기중, 플레이어 ID: {playerId}");

        foreach (var characterData in CharacterObjDatas)
        {
            characterObjectsDict.Add(characterData.types, characterData.prefabs);
        }

        rowPositions = GetCenteredRowPositions(grid, 0, 0, halfWidth);
        SetPhase(new SelectPhase());
    }

    public void SetPhase(IGameScenePhase newPhase)
    {
        currentPhase = newPhase;
        currentPhase.Enter(this);
        Debug.Log($"▶현재 게임 Phase: {currentPhase.GetType().Name}");
    }

    private void OnPacketReceived(Packet packet)
    {
        if (currentPhase != null)
            currentPhase.OnPacketReceived(packet);
    }

    private void OnDestroy()
    {
        if ( GameSession.Instance.Sender != null)
            GameSession.Instance.Sender.OnPacketReceived -= OnPacketReceived;
    }

    private void Update()
    {
        if (currentPhase != null)
        {
            currentPhase.UpdateLoop();
        }
    }

    public GameObject InstantUnit(string type, int level, string playerType)
    {
        Debug.LogWarning($"{type} || {level} || {playerType}");
        GameObject obj = Instantiate(characterObjectsDict[type][level]);
        if (playerType == "First")
        {
            FirstSecondDatas[0].units.Add(obj);
        }
        else if (playerType == "Second")
        {
            FirstSecondDatas[1].units.Add(obj);
        }

        return obj;
    }

    public string ReturnPlayerId()
    {
        return playerId;
    }

    public IGameScenePhase ReturnCurrentPhase()
    {
        return currentPhase;
    }
    public static Transform GetCenteredRowPositions(Grid grid, int centerX, int centerY, int halfWidth)
    {
        int y = centerY - 2; // 중앙보다 두 칸 아래 줄
        int index = 0;
        GameObject all=new GameObject("BatchIndeies");
        all.transform.position = Vector3.zero;
        for (int x = centerX - halfWidth; x <= centerX + halfWidth-1; x++)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);
            Vector3 worldPos = grid.GetCellCenterWorld(cell);
            GameObject empty=new GameObject("BatchIndex["+index.ToString()+"]");
            empty.transform.position = worldPos;
            empty.transform.parent = all.transform;
            index++;
        }

        return all.transform;
    }

    public Transform GetRowPositions()
    {
        return rowPositions;
    }

    public void SetData(bool isFirst)
    {
        this.isFirst = isFirst;
        if ( this.isFirst)
        {
            myData = FirstSecondDatas[0];
            enemyData = FirstSecondDatas[1];
        }
        else
        {
            myData = FirstSecondDatas[1];
            enemyData = FirstSecondDatas[0];
        }
    }
    public int MapServerIndexToLocal(int side, int serverIndex)
    {
        // 배치 부모 자식 수
        var parent = FirstSecondDatas[side].batchPos;
        int n = (parent != null) ? parent.childCount : 0;

        // 0..n-1 이면 그대로 사용
        if (serverIndex >= 0 && serverIndex < n) return serverIndex;

        // 서버가 두 번째 진영을 10~ 로 코딩해서 보낼 때 (10,11,12 → 0,1,2)
        if (side == 1)
        {
            if (serverIndex >= 10 && serverIndex < 10 + n) return serverIndex - 10;
            if (serverIndex >= 11 && serverIndex < 11 + n) return serverIndex - 11; // 변종(예전 11~13)도 케어
                                                                                    // 최후 방어: 10 이상이면 10의 자리 버리고 쓰기
            if (serverIndex >= 10) return serverIndex % 10;
        }

        // 그 밖은 유효하지 않음
        return -1;
    }

    public Transform GetBatchParent(int side)
    => FirstSecondDatas[side].batchPos;

    // side 부모의 자식 인덱스를 안전하게 얻는다
    public Transform SafeBatchRow(int side, int index, string who = "")
    {
        var parent = GetBatchParent(side);
        if (parent == null) { Debug.LogError($"[Batch] parent null (side={side})"); return null; }
        int n = parent.childCount;
        if (index < 0 || index >= n)
        {
            Debug.LogError($"[Batch] INVALID index={index} (0..{n - 1}) | side={side} | {who}");
            return null;
        }
        return parent.GetChild(index);
    }

    // 유닛 GO를 사이드/인덱스로 가져온다
    public GameObject GetUnitGO(int side, int unitIndex)
    {
        var list = FirstSecondDatas[side].units; // List<GameObject>
        if (list == null || unitIndex < 0 || unitIndex >= list.Count) return null;
        return list[unitIndex];
    }
}
