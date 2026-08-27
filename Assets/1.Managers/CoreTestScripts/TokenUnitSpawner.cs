using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// TurnManager로부터 유닛 생성/정리 이벤트를 받아 실제 게임 씬에 유닛을 생성하고 관리합니다.
/// 이 클래스는 유닛의 '생성'과 '제거'라는 단일 책임을 가집니다.
/// </summary>
public class TokenUnitSpawner : MonoBehaviour
{
    [Tooltip("게임의 로직을 관장하는 TurnManager에 대한 참조입니다.")]
    [SerializeField]
    private TurnManager _turnManager;

    [Serializable]
    public struct UnitPrefabMap
    {
        public int baseStatID;
        public GameObject prefab;
    }

    [Header("유닛 프리팹 설정")]
    [Tooltip("유닛의 ID와 실제 생성될 프리팹을 매핑한 리스트입니다.")]
    [SerializeField]
    private List<UnitPrefabMap> _prefabMaps = new();
    [Tooltip("매핑되지 않은 ID에 대한 기본 프리팹입니다.")]
    [SerializeField]
    private GameObject _defaultUnitPrefab;

    [Header("유닛 스폰 위치")]
    [SerializeField]
    private Transform[] _playerRows = new Transform[3];
    [SerializeField]
    private Transform[] _pcRows = new Transform[3];

    // 유닛 ID를 키로 사용하여 프리팹을 빠르게 조회하기 위한 딕셔너리입니다. (리스트 순회보다 성능이 우수합니다.)
    private readonly Dictionary<int, GameObject> _prefabByStatID = new();
    
    // 현재 씬에 생성된 유닛들을 추적하기 위한 리스트입니다.
    private readonly List<GameObject> _spawnedPlayerUnits = new List<GameObject>();
    private readonly List<GameObject> _spawnedPCUnits = new List<GameObject>();

    /// <summary>
    /// 컴포넌트 초기화 시, 인스펙터에 설정된 프리팹 리스트를 딕셔너리로 변환합니다.
    /// 이는 런타임에 유닛을 생성할 때 List를 순회하는 것보다 훨씬 빠른 조회를 가능하게 하여 성능을 향상시키기 위함입니다.
    /// </summary>
    private void Awake()
    {
        _prefabByStatID.Clear();
        foreach (var map in _prefabMaps)
        {
            if (map.prefab != null)
                _prefabByStatID[map.baseStatID] = map.prefab;
        }
    }

    /// <summary>
    /// 게임 시작 시 TurnManager의 이벤트를 구독하여, 게임 로직에 맞춰 유닛을 생성/제거할 수 있도록 준비합니다.
    /// </summary>
    private void Start()
    {
        // 인스펙터에서 참조가 할당되지 않았을 경우에 대한 안전장치입니다.
        // 다만, FindObjectOfType은 비용이 높은 연산이므로 가능한 한 인스펙터에서 직접 할당하는 것을 권장합니다.
        if (_turnManager == null)
            _turnManager = FindObjectOfType<TurnManager>();

        SubscribeToEvents();
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 이벤트 구독을 해제하여 메모리 누수를 방지합니다.
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// TurnManager의 이벤트를 구독하여 유닛 관리 메서드를 연결합니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (_turnManager == null) return;
        
        _turnManager.OnPlayerUnitSpawnRequested += SpawnPlayerUnit;
        _turnManager.OnPCUnitSpawnRequested += SpawnPCUnit;
        _turnManager.OnNewRoundStarted += ClearPreviousUnits;
    }
    
    /// <summary>
    /// 메모리 누수 방지를 위해 OnDestroy 시점에 구독했던 모든 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_turnManager == null) return;

        _turnManager.OnPlayerUnitSpawnRequested -= SpawnPlayerUnit;
        _turnManager.OnPCUnitSpawnRequested -= SpawnPCUnit;
        _turnManager.OnNewRoundStarted -= ClearPreviousUnits;
    }

    /// <summary>
    /// TurnManager의 요청에 따라 플레이어 유닛을 생성합니다.
    /// </summary>
    /// <param name="unitId">생성할 유닛의 baseStatID</param>
    public void SpawnPlayerUnit(int unitId)
    {
        int i = _spawnedPlayerUnits.Count;
        if (i >= _playerRows.Length)
        {
            Debug.LogWarning($"플레이어 유닛 스폰 위치가 부족하여 {unitId} 유닛을 생성할 수 없습니다.");
            return;
        }

        GameObject prefab = ResolveUnitPrefabByStatID(unitId);
        GameObject unitGO = Instantiate(prefab, _playerRows[i], false);
        unitGO.name = $"Player_Unit_{unitId}_Slot{i}";
        
        var character = unitGO.GetComponent<Character>();
        character.ChangeState("Idle");
        character.SetDir(1); // 플레이어 유닛은 오른쪽을 바라보도록 설정

        var metaTag = unitGO.GetComponent<UnitMetaTag>() ?? unitGO.AddComponent<UnitMetaTag>();
        metaTag.baseStatId = unitId;
        metaTag.isPlayerUnit = true;
        metaTag.currentSlotIndex = i;

        _spawnedPlayerUnits.Add(unitGO);
    }

    /// <summary>
    /// TurnManager의 요청에 따라 PC(컴퓨터) 유닛을 생성합니다.
    /// </summary>
    /// <param name="unitId">생성할 유닛의 baseStatID</param>
    public void SpawnPCUnit(int unitId)
    {
        int i = _spawnedPCUnits.Count;
        if (i >= _pcRows.Length)
        {
            Debug.LogWarning($"PC 유닛 스폰 위치가 부족하여 {unitId} 유닛을 생성할 수 없습니다.");
            return;
        }

        GameObject prefab = ResolveUnitPrefabByStatID(unitId);
        GameObject unitGO = Instantiate(prefab, _pcRows[i], false);
        unitGO.name = $"PC_Unit_{unitId}_Slot{i}";

        var character = unitGO.GetComponent<Character>();
        character.ChangeState("Idle");
        character.SetDir(-1); // PC 유닛은 왼쪽을 바라보도록 설정

        var metaTag = unitGO.GetComponent<UnitMetaTag>() ?? unitGO.AddComponent<UnitMetaTag>();
        metaTag.baseStatId = unitId;
        metaTag.isPlayerUnit = false;
        metaTag.currentSlotIndex = i;

        _spawnedPCUnits.Add(unitGO);
    }

    /// <summary>
    /// 새 라운드 시작 시, 이전에 생성된 모든 유닛을 파괴하여 씬을 정리합니다.
    /// TODO: 모바일 환경에서 Instantiate/Destroy로 인한 GC(Garbage Collection) 발생은 큰 부담이 될 수 있습니다.
    /// 향후 성능 최적화를 위해 이 부분을 오브젝트 풀링(Object Pooling) 방식으로 전환해야 합니다.
    /// </summary>
    public void ClearPreviousUnits()
    {
        foreach (var unit in _spawnedPlayerUnits)
            Destroy(unit);
        _spawnedPlayerUnits.Clear();

        foreach (var unit in _spawnedPCUnits)
            Destroy(unit);
        _spawnedPCUnits.Clear();
    }

    /// <summary>
    /// 유닛의 baseStatID에 해당하는 프리팹을 딕셔너리에서 찾아 반환합니다.
    /// 매핑된 프리팹이 없으면 안전을 위해 기본 프리팹을 반환합니다.
    /// </summary>
    /// <returns>유닛 프리팹</returns>
    private GameObject ResolveUnitPrefabByStatID(int baseStatID)
    {
        if (_prefabByStatID.TryGetValue(baseStatID, out var prefab) && prefab != null)
            return prefab;
        
        Debug.LogWarning($"{baseStatID}에 해당하는 유닛 프리팹이 없어 기본 프리팹을 사용합니다.");
        return _defaultUnitPrefab;
    }

    public IReadOnlyList<GameObject> SpawnedPlayerUnits => _spawnedPlayerUnits;
    public IReadOnlyList<GameObject> SpawnedPCUnits => _spawnedPCUnits;

    /// <summary>
    /// 두 플레이어 유닛의 위치를 DOTween을 사용하여 부드럽게 교환하고, 내부 데이터(슬롯 인덱스 등)도 함께 업데이트합니다.
    /// 이는 유닛 배치 단계에서 플레이어가 유닛 위치를 바꿀 때 사용하기 위함입니다.
    /// </summary>
    /// <param name="onComplete">애니메이션과 데이터 교환이 모두 완료된 후 호출될 콜백입니다.</param>
    public void SwapUnitPositions(UnitMetaTag unit1Meta, UnitMetaTag unit2Meta)
    {
        if (unit1Meta == null || unit2Meta == null || !unit1Meta.isPlayerUnit || !unit2Meta.isPlayerUnit)
        {
            Debug.LogError("유닛 위치 교환 실패: 유효하지 않은 유닛 메타 태그이거나 플레이어 유닛이 아닙니다.");
            //onComplete?.Invoke();
            return;
        }
        
        Vector3 pos1 = unit1Meta.transform.position;
        Vector3 pos2 = unit2Meta.transform.position;
        Transform parent1 = unit1Meta.transform.parent;
        Transform parent2 = unit2Meta.transform.parent;
        int index1 = unit1Meta.currentSlotIndex;
        int index2 = unit2Meta.currentSlotIndex;
        
        if (index1 < 0 || index1 >= _spawnedPlayerUnits.Count || index2 < 0 || index2 >= _spawnedPlayerUnits.Count)
        {
            Debug.LogError($"유닛 위치 교환 실패: 유효하지 않은 슬롯 인덱스입니다. Index1: {index1}, Index2: {index2}");
            //onComplete?.Invoke();
            return;
        }

        // DOTween 시퀀스를 사용하여 두 유닛의 이동 애니메이션을 동시에 재생합니다.
        Sequence swapSequence = DOTween.Sequence();
        swapSequence.Append(unit1Meta.transform.DOMove(pos2, 0.3f).SetEase(Ease.OutQuad))
            .Join(unit2Meta.transform.DOMove(pos1, 0.3f).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                // 애니메이션 완료 후, 실제 부모 Transform과 관리 리스트 내의 참조를 교환합니다.
                unit1Meta.transform.SetParent(parent2);
                unit2Meta.transform.SetParent(parent1);

                GameObject tempUnit = _spawnedPlayerUnits[index1];
                _spawnedPlayerUnits[index1] = _spawnedPlayerUnits[index2];
                _spawnedPlayerUnits[index2] = tempUnit;

                // MetaTag에 저장된 슬롯 인덱스도 업데이트하여 데이터 정합성을 맞춥니다.
                unit1Meta.currentSlotIndex = index2;
                unit2Meta.currentSlotIndex = index1;

                //onComplete?.Invoke();
            })
            .SetLink(gameObject); // 이 오브젝트가 파괴될 때 트윈도 함께 Kill하여 메모리 누수를 방지합니다.
    }
}
