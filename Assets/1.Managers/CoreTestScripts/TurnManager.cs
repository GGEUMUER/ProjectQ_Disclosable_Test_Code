using System;
using UnityEngine;
using Core.SinglePlay;
using System.Collections.Generic;
using System.Threading;
using Core.Units;
using Cysharp.Threading.Tasks;

/// <summary>
/// 게임의 전체 턴과 상태 흐름을 관리합니다.
/// SinglePlayCore의 상태를 제어하고, 각 단계에 맞는 이벤트를 외부에 알리는 '엔진' 역할을 합니다。
/// Coroutine 대신 UniTask를 사용하여 비동기 흐름을 관리함으로써, GC 부담을 줄이고 코드 구조를 명확하게 합니다。
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private BackgroundSequenceManager _bgManager;

    [Header("Playing Flags")]
    bool _batchConfirmed;
    Dictionary<int, Vector2Int> _cachedPlacements;
    bool _battlePlayBackgroundRunning = false;
    bool _battlePlayBackgroundFinished = false;

    public GameObject battleRunnerPrefab;
    public SingleBattleRunner battleRunner;

    private readonly SinglePlayCore _singlePlayCore = new SinglePlayCore();
    [SerializeField]
    private int _roundIndex = 1;
    [SerializeField]
    private const int MaxRound = 3;

    private SinglePlayStep _previousStep = (SinglePlayStep)byte.MaxValue;
    
    private CancellationTokenSource _cts;

    // 이벤트 정의: 각 게임 단계에서 UI 또는 다른 시스템에 상태 변경을 알리기 위해 사용됩니다。
    public event Action<SinglePlayStep> OnStepChanged;
    public event Action<string> OnStatusUpdated;
    public event Action<int> OnPlayerUnitSpawnRequested;
    public event Action<int> OnPCUnitSpawnRequested;
    public event Action<int> OnPCUnitSelectRequested;
    public event Action OnBatchPhaseStarted;
    public event Action<RoundResult> OnRoundEnded;
    public event Action OnNewRoundStarted;
    public event Action<UnitType, UnitType, int, int> OnShuffleCompleted;
    public event Action OnRemovePhaseStarted;
    public event Action<int, UnitType, UnitType> OnCardRemoved;
    public event Action OnFinalPickPhaseStarted;
    public event Action<int, int> OnFinalPickCompleted;

    // 유닛 ID 리스트: 현재 라운드에서 선택된 유닛들을 추적합니다。
    private readonly List<int> _mySelectedUnitIds = new List<int>();
    private List<int> _pcSelectedUnitIds = new List<int>();

    private UnitSorter unitSorter = new UnitSorter();

    private void Awake()
    {
        // 이 컴포넌트가 파괴될 때 비동기 작업(GameFlow)을 안전하게 중단시키기 위한 토큰을 설정합니다。
        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 게임 시작 시, 코어 로직을 초기화하고 메인 게임 플로우를 시작합니다.
    /// </summary>
    private async void Start()
    {
        try
        {
            AudioManager._instance.PlayBGMSound(BGMLIST.BattleStart);

            _singlePlayCore.Init();
            
            // BackgroundManager의 시작 시퀀스가 있다면, 완료될 때까지 기다립니다.
            if (_bgManager != null)
            {
                // 콜백 기반의 StartSequence를 await 키워드로 기다릴 수 있도록 변환합니다.
                var tcs = new UniTaskCompletionSource();
                _bgManager.StartSequence(() => tcs.TrySetResult());
                
                // 토큰을 전달하여 대기 중 파괴 시 즉시 중단
                await tcs.Task.AttachExternalCancellation(_cts.Token);
            }
            
            // 시퀀스가 완료된 후 메인 게임 플로우를 시작합니다.
            await GameFlow(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 객체 파괴로 인한 정상적인 취소이므로 로그를 남기지 않거나 디버그용으로만 처리
            Logger.Log("[TurnManager] GameFlow cancelled safely.");
        }
        catch (Exception e)
        {
            // 기타 예외 상황 로그
            Logger.LogError($"[TurnManager] Unexpected Error: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        // 명시적으로 취소 요청 및 리소스 정리
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        // 이벤트 구독 해제 (메모리 누수 방지)
        OnStepChanged = null;
        OnStatusUpdated = null;
        OnPlayerUnitSpawnRequested = null;
        OnPCUnitSpawnRequested = null;
        OnPCUnitSelectRequested = null;
        OnBatchPhaseStarted = null;
        OnRoundEnded = null;
        OnNewRoundStarted = null;
        OnShuffleCompleted = null;
        OnRemovePhaseStarted = null;
        OnCardRemoved = null;
        OnFinalPickPhaseStarted = null;
        OnFinalPickCompleted = null;
        
        Logger.Log("[TurnManager] Destroyed and Cleaned up.");
    }

    private void Update() // 게임 종료 임시
    {
        //게임 종료
        if (_roundIndex > MaxRound)
        {
            Logger.Log("[End if] In?");
            OnStatusUpdated?.Invoke("게임 종료");
            SceneLauncher.LoadScene(SceneConstants.LobbyScene, true);
            return;
        }
    }

    /// <summary>
    /// 게임의 메인 로직을 순차적으로 실행하는 비동기 메서드입니다。
    /// 각 라운드의 단계를 UniTask를 통해 순차적으로 진행합니다。
    /// </summary>
    private async UniTask GameFlow(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_previousStep != _singlePlayCore.Step)
            {
                _previousStep = _singlePlayCore.Step;
                OnStepChanged?.Invoke(_singlePlayCore.Step);
            }

            OnStatusUpdated?.Invoke($"라운드 {_roundIndex} - {_singlePlayCore.Step}");

            switch (_singlePlayCore.Step)
            {
                case SinglePlayStep.Start:
                    OnNewRoundStarted?.Invoke();
                    _mySelectedUnitIds.Clear();
                    _pcSelectedUnitIds.Clear();

                    await HandleStartStep(cancellationToken);
                    break;
                case SinglePlayStep.FirstPlayerFirstPick:
                case SinglePlayStep.SecondPlayerFirstPick:
                    await HandleFirstPickStep(cancellationToken);
                    break;
                case SinglePlayStep.FirstDeal:
                    await HandleShuffleStep(SinglePlayStep.FirstDeal, cancellationToken);
                    break;
                case SinglePlayStep.FirstRemovePick:
                    await HandleRemoveStep(SinglePlayStep.FirstRemovePick, cancellationToken);
                    break;
                case SinglePlayStep.SecondPick:
                    await HandlePickStep(SinglePlayStep.SecondPick, cancellationToken);
                    break;
                case SinglePlayStep.SecondDeal:
                    await HandleShuffleStep(SinglePlayStep.SecondDeal, cancellationToken);
                    break;
                case SinglePlayStep.SecondRemovePick:
                    await HandleRemoveStep(SinglePlayStep.SecondRemovePick, cancellationToken);
                    break;
                case SinglePlayStep.ThirdPick:
                    await HandlePickStep(SinglePlayStep.ThirdPick, cancellationToken);
                    break;
                case SinglePlayStep.Batch:
                    await HandleBatchStep(cancellationToken);
                    break;
                case SinglePlayStep.End:
                    await HandleEndStep(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 루프 과부하 방지: 스텝 변경이 없을 때 CPU를 과도하게 사용하지 않도록 한 프레임 쉽니다.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    /// <summary>
    /// '시작' 단계를 처리합니다. 선공/후공을 결정하고 UI에 알립니다.
    /// </summary>
    private async UniTask HandleStartStep(CancellationToken cancellationToken)
    {
        if (_singlePlayCore.StartCardSelect(out var firstPlayer))
        {

            OnStatusUpdated?.Invoke((firstPlayer == 0) ? "[선택 우선권]" : "[선택 후순위]");
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
           
        }
    }

    /// <summary>
    /// '첫 유닛 선택' 단계를 처리합니다. 플레이어의 입력을 기다리거나 PC의 선택을 자동으로 진행합니다.
    /// </summary>
    private async UniTask HandleFirstPickStep(CancellationToken cancellationToken)
    {
        bool isMyTurn = (_singlePlayCore.FirstPlayer == 0 && _singlePlayCore.Step == SinglePlayStep.FirstPlayerFirstPick) ||
                    (_singlePlayCore.FirstPlayer == 1 && _singlePlayCore.Step == SinglePlayStep.SecondPlayerFirstPick);

        if (!isMyTurn)
        {
            // PC 턴 로직
            OnStatusUpdated?.Invoke("상대방이 선택 중 입니다.");
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);

            if (_singlePlayCore.TryFirstPickComputer(out int npcId, out UnitType computerPublic))
            {
                _pcSelectedUnitIds.Add(npcId);
                OnPCUnitSelectRequested.Invoke(npcId);
                // PC 선택 후 연출을 위해 잠깐 대기 후 단계 변경
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
                return; // PC 턴 처리가 끝났으므로 여기서 종료
            }
        }
        else
        {
            // 플레이어 턴 로직
            OnStatusUpdated?.Invoke("금화 토큰 1개를 선택하세요.\n선택한 토큰은 즉시 획득되며, 이번 라운드에서 제외됩니다.");

            await UniTask.WaitUntil(() => _mySelectedUnitIds.Count > 0, cancellationToken: cancellationToken);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// '카드 셔플 및 공개' 단계를 처리합니다. 각 플레이어의 공개 카드를 결정하고 이벤트를 발생시킵니다.
    /// </summary>
    private async UniTask HandleShuffleStep(SinglePlayStep step, CancellationToken cancellationToken)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
        if (_singlePlayCore.TryShuffle(out UnitType userPublic, out UnitType computerPublic))
        {
            OnShuffleCompleted?.Invoke(userPublic, computerPublic, _mySelectedUnitIds[0] - 5, _pcSelectedUnitIds[0] - 5);
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// '카드 제거' 단계를 처리합니다. 플레이어가 카드를 버릴 때까지 대기합니다.
    /// </summary>
    private async UniTask HandleRemoveStep(SinglePlayStep step, CancellationToken cancellationToken)
    {
        OnStatusUpdated?.Invoke("버릴 은화 토큰을 1개 선택하세요.\n남은 2장의 토큰을 비교합니다.");
        OnRemovePhaseStarted?.Invoke();
        
        // 실제 카드 제거 로직은 UI 입력에 의해 TryPlayerRemoveCard가 호출되면서 진행됩니다.
        // 여기서는 해당 단계가 끝날 때까지 (다음 단계로 넘어갈 때까지) 기다립니다.
        await UniTask.WaitUntil(() => _singlePlayCore.Step != step, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// '카드 선택' 단계를 처리합니다. 플레이어가 남은 카드 중 하나를 선택할 때까지 대기합니다.
    /// </summary>
    private async UniTask HandlePickStep(SinglePlayStep step, CancellationToken cancellationToken)
    {
        OnStatusUpdated?.Invoke("획득할 은화 토큰을 1개 선택하세요");
        OnFinalPickPhaseStarted?.Invoke();
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
        
        // 실제 카드 선택 로직은 UI 입력에 의해 TryPlayerPickRemainCard가 호출되면서 진행됩니다.
        await UniTask.WaitUntil(() => _singlePlayCore.Step != step, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// '유닛 배치' 단계를 처리합니다. 플레이어가 배치를 완료할 때까지 대기합니다.
    /// </summary>
    private async UniTask HandleBatchStep(CancellationToken cancellationToken)
    {
        //AudioManager._instance.PlayBGMSound(BGMLIST.Batch);
        
        OnStatusUpdated?.Invoke("캐릭터를 선택한 뒤 위치를 교체할 캐릭터를 선택하세요.");
        OnBatchPhaseStarted?.Invoke();

        // 플레이어가 '배치 완료' 버튼을 누를 때까지 (다음 단계로 넘어갈 때까지) 대기합니다.
        await UniTask.WaitUntil(() => _batchConfirmed, cancellationToken: cancellationToken);
        
        GameObject insedBattleRunner = Instantiate(battleRunnerPrefab);
        battleRunner = insedBattleRunner.GetComponent<SingleBattleRunner>();

        _batchConfirmed = false;

        _battlePlayBackgroundRunning = true;
        _battlePlayBackgroundFinished = false;

        battleRunner.BegindBattle();
        battleRunner.Init();

        OnStatusUpdated?.Invoke(" ");

        await UniTask.WaitUntil(() => battleRunner.IsPlaybackFinished, cancellationToken: cancellationToken);

        battleRunner.BOCleaner();
        Destroy(battleRunner.gameObject);
        _battlePlayBackgroundFinished = true;
        _battlePlayBackgroundRunning = false;
    }

    /// <summary>
    /// '라운드 종료' 단계를 처리합니다. 승패를 판정하고 다음 라운드로 넘어가거나 게임을 종료합니다.
    /// </summary>
    private async UniTask HandleEndStep(CancellationToken cancellationToken)
    {
        if(_battlePlayBackgroundRunning && !_battlePlayBackgroundFinished)
        {
            await UniTask.WaitUntil(()=> _battlePlayBackgroundFinished, cancellationToken: cancellationToken);
        }

        if (_singlePlayCore.TryWinPlayerCheck(0, out RoundResult roundResult))
        {
            OnRoundEnded?.Invoke(roundResult);
            await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: cancellationToken);
            
            _roundIndex++;
        }
    }

    /// <summary>
    /// 배틀 종료: 최종 승패를 판정하고 결과를 보여주고 배틀 씬을 종료.
    /// </summary>
    private async UniTask HandleBattleEnd(CancellationToken cancellationToken)
    {
        // 로비로 돌아가기 전 연출 시간을 확보
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
        
        // 씬 전환 호출
        SceneLauncher.LoadScene(SceneConstants.LobbyScene, true);
    }

    #region Public Methods for UI Interaction

    /// <summary>
    /// [UI] 플레이어의 첫 유닛 선택을 시도합니다. UI로부터 호출됩니다.
    /// </summary>
    /// <param name="uiIndex">UI상에서 선택된 버튼의 인덱스</param>
    /// <param name="pcSelectedUnitId">PC가 먼저 선택한 유닛의 ID (없으면 0)</param>
    /// <returns>선택 성공 여부</returns>
    public bool TryPlayerFirstPick(int uiIndex, int pcSelectedUnitId)
    {
        int adjustedIndex = uiIndex;
        // PC가 먼저 유닛을 선택한 경우, UI 인덱스는 실제 데이터 인덱스와 달라지므로 보정합니다。
        if (pcSelectedUnitId > 0 && pcSelectedUnitId - GameConstants.FirstPickButtonIdOffset < uiIndex)
            adjustedIndex -= 1;

        if (_singlePlayCore.TryFirstPickUser(adjustedIndex, out int unitId, out UnitType a))
        {
            _mySelectedUnitIds.Add(unitId);
            OnPlayerUnitSpawnRequested?.Invoke(unitId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// [UI] 플레이어의 카드 제거 선택을 시도합니다. UI로부터 호출됩니다.
    /// </summary>
    /// <param name="index">UI상에서 선택된 카드의 인덱스</param>
    /// <returns>제거 성공 여부</returns>

    public bool TryPlayerRemoveCard(int index)
    {
        if (_singlePlayCore.TryRemoveCard(index, out var typeA, out var typeB))
        {
            OnCardRemoved?.Invoke(index, typeA, typeB);

            // 두 카드의 타입이 같아 자동으로 분배되는 특별 케이스를 처리합니다。
            if (typeA == typeB)
            {
                _singlePlayCore.TryPickRemainCard(0, out int userId, out int computerId);
                _mySelectedUnitIds.Add(userId);
                _pcSelectedUnitIds.Add(computerId);
                OnPlayerUnitSpawnRequested?.Invoke(userId);
                OnFinalPickCompleted?.Invoke(userId, computerId);
            }
            else
            {

            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// [UI] 플레이어의 남은 카드 선택을 시도합니다. UI로부터 호출됩니다.
    /// </summary>
    /// <param name="index">UI상에서 선택된 카드의 인덱스</param>
    /// <returns>선택 성공 여부</returns>
    public bool TryPlayerPickRemainCard(int index)
    {
        if (_singlePlayCore.TryPickRemainCard(index, out int userId, out int computerId))
        {
            _mySelectedUnitIds.Add(userId);
            _pcSelectedUnitIds.Add(computerId);

            OnPlayerUnitSpawnRequested?.Invoke(userId);
            OnFinalPickCompleted?.Invoke(userId, computerId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// [UI] 플레이어의 유닛 배치 완료를 처리합니다. UI로부터 호출됩니다.
    /// </summary>
    /// <param name="placements">플레이어의 유닛 배치 정보 (Key: 유닛 ID, Value: 좌표)</param>
    public void CompleteBatching(Dictionary<int, Vector2Int> placements)
    {
        // TODO: 서버가 구현되면 `placements` 데이터를 서버로 전송하는 로직이 필요합니다。
        if (_singlePlayCore.TryGetComputerBatch(out var _))
        {
            _pcSelectedUnitIds = unitSorter.SortSelectedUnits(_pcSelectedUnitIds);

            // PC가 선택한 모든 유닛의 스폰을 요청합니다.
            foreach (var id in _pcSelectedUnitIds)
                OnPCUnitSpawnRequested?.Invoke(id);

            OnStatusUpdated?.Invoke("전투 시작!");
        }

        _cachedPlacements = placements;
        _batchConfirmed = true;
    }
    #endregion
}