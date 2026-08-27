using UnityEngine;
using Core.SinglePlay;

/// <summary>
/// 유닛 배치 단계(Batch Phase)에서 플레이어 유닛의 위치를 교환하는 로직을 담당합니다.
/// TurnManager의 이벤트를 구독하여 배치 단계가 시작되면 활성화되고, 종료되면 비활성화됩니다.
/// </summary>
public class UnitPlacementHandler : MonoBehaviour
{
    [Tooltip("게임의 턴과 상태를 관리하는 TurnManager 참조")]
    [SerializeField] private TurnManager _turnManager;
    [Tooltip("유닛의 생성과 제거를 담당하는 TokenUnitSpawner 참조")]
    [SerializeField] private TokenUnitSpawner _tokenUnitSpawner;

    private UnitMetaTag _selectedUnitMetaTag;
    private Vector3 _selectedUnitOriginalScale;
    private bool _isPlacementActive;

    private const float _highlightScaleFactor = 1.2f; // 선택 유닛 하이라이트 배율

    private void Awake()
    {
        // 인스펙터에서 참조가 할당되지 않았을 경우, 씬에서 직접 찾습니다.
        if (_turnManager == null)
            _turnManager = FindObjectOfType<TurnManager>();
        if (_tokenUnitSpawner == null)
            _tokenUnitSpawner = FindObjectOfType<TokenUnitSpawner>();
    }

    private void Start()
    {
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// TurnManager의 이벤트를 구독하여 배치 단계 시작/종료에 따라 이 핸들러를 활성화/비활성화합니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (_turnManager != null)
        {
            _turnManager.OnBatchPhaseStarted += EnablePlacement;
            _turnManager.OnStepChanged += HandleStepChange;
        }
    }

    /// <summary>
    /// 메모리 누수 방지를 위해 구독했던 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_turnManager != null)
        {
            _turnManager.OnBatchPhaseStarted -= EnablePlacement;
            _turnManager.OnStepChanged -= HandleStepChange;
        }
    }

    /// <summary>
    /// TurnManager의 게임 단계(Step) 변경에 따라 호출됩니다.
    /// 배치 단계가 아닐 경우, 유닛 배치 기능을 비활성화합니다.
    /// </summary>
    private void HandleStepChange(SinglePlayStep newStep)
    {
        if (newStep != SinglePlayStep.Batch && _isPlacementActive)
        {
            DisablePlacement();
        }
    }

    /// <summary>
    /// 유닛 클릭 시 호출되는 메서드. UnitClickHandler를 통해 호출됩니다.
    /// </summary>
    /// <param name="clickedUnitMetaTag">클릭된 유닛의 UnitMetaTag 컴포넌트</param>
    public void OnUnitClicked(UnitMetaTag clickedUnitMetaTag)
    {
        // 배치 단계가 아닐 때는 선택 로직 비활성화
        if (!_isPlacementActive)
        {
            Debug.Log("유닛 배치는 활성화 상태에서만 가능합니다.");
            return;
        }

        // 상대방(PC) 유닛은 클릭 불가 처리
        if (!clickedUnitMetaTag.isPlayerUnit)
        {
            Debug.Log("PC 유닛과는 상호작용할 수 없습니다.");
            return;
        }

        if (_selectedUnitMetaTag == null)
        {
            // 첫 번째 유닛 선택
            SelectUnit(clickedUnitMetaTag);
        }
        else
        {
            // 두 번째 유닛 선택
            if (_selectedUnitMetaTag == clickedUnitMetaTag)
            {
                // 동일한 유닛을 두 번 클릭할 경우 선택 해제
                DeselectUnit();
            }
            else
            {
                // 다른 유닛을 클릭한 경우 위치 교환
                SwapUnits(clickedUnitMetaTag);
            }
        }
    }
    
    /// <summary>
    /// 유닛을 선택하고 시각적 피드백을 줍니다.
    /// </summary>
    private void SelectUnit(UnitMetaTag unitMetaTag)
    {
        _selectedUnitMetaTag = unitMetaTag;
        _selectedUnitOriginalScale = unitMetaTag.transform.localScale;
        _selectedUnitMetaTag.transform.localScale = _selectedUnitOriginalScale * _highlightScaleFactor;
        Debug.Log($"유닛 선택: {_selectedUnitMetaTag.gameObject.name}");
    }

    /// <summary>
    /// 선택된 두 유닛의 위치를 교환합니다.
    /// </summary>
    private void SwapUnits(UnitMetaTag otherUnitMetaTag)
    {
        Debug.Log($"유닛 {_selectedUnitMetaTag.gameObject.name} (슬롯 {_selectedUnitMetaTag.currentSlotIndex})와 " +
                  $"{otherUnitMetaTag.gameObject.name} (슬롯 {otherUnitMetaTag.currentSlotIndex}) 교환 시도.");
                
        _tokenUnitSpawner.SwapUnitPositions(_selectedUnitMetaTag, otherUnitMetaTag);
        DeselectUnit(); // 교환 후 선택 해제
    }

    /// <summary>
    /// 현재 선택된 유닛을 해제하고 시각적 피드백을 초기화합니다.
    /// </summary>
    private void DeselectUnit()
    {
        if (_selectedUnitMetaTag != null)
        {
            _selectedUnitMetaTag.transform.localScale = _selectedUnitOriginalScale; // 원래 크기로 복원
            Debug.Log($"유닛 선택 해제: {_selectedUnitMetaTag.gameObject.name}");
            _selectedUnitMetaTag = null;
        }
    }

    /// <summary>
    /// 유닛 배치 기능을 활성화하고, 플레이어 유닛들이 클릭에 반응하도록 설정합니다.
    /// </summary>
    private void EnablePlacement()
    {
        if (_tokenUnitSpawner == null)
        {
            Debug.LogError("TokenUnitSpawner 참조가 없어 유닛 배치 시스템을 활성화할 수 없습니다.");
            return;
        }

        _isPlacementActive = true;
        
        // 플레이어 유닛들에 있는 UnitClickHandler를 활성화합니다.
        // 유닛 프리팹에는 반드시 UnitClickHandler와 Collider가 존재해야 합니다.
        foreach (GameObject playerUnitGO in _tokenUnitSpawner.SpawnedPlayerUnits)
        {
            var metaTag = playerUnitGO.GetComponent<UnitMetaTag>();
            var clickHandler = playerUnitGO.GetComponent<UnitClickHandler>();
            
            if (clickHandler != null && metaTag != null)
            {
                clickHandler.Init(this, metaTag);
                clickHandler.enabled = true;
            }
            else
            {
                Debug.LogWarning($"유닛 {playerUnitGO.name}에 UnitClickHandler 또는 UnitMetaTag가 없습니다. 프리팹 설정을 확인하세요.");
            }
        }
        Debug.Log("유닛 배치 시스템 활성화.");
    }

    /// <summary>
    /// 유닛 배치 기능을 비활성화합니다.
    /// </summary>
    public void DisablePlacement()
    {
        DeselectUnit(); // 선택된 유닛이 있다면 해제합니다.
        _isPlacementActive = false;

        if (_tokenUnitSpawner != null && _tokenUnitSpawner.SpawnedPlayerUnits != null)
        {
            foreach (GameObject playerUnitGO in _tokenUnitSpawner.SpawnedPlayerUnits)
            {
                if (playerUnitGO == null) continue; // 유닛이 중간에 파괴되었을 경우 대비
                
                UnitClickHandler clickHandler = playerUnitGO.GetComponent<UnitClickHandler>();
                if (clickHandler != null)
                {
                    clickHandler.enabled = false; // 클릭 핸들러 비활성화
                }
            }
        }
        Debug.Log("유닛 배치 시스템 비활성화.");
    }
}