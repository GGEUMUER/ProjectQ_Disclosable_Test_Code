using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.SinglePlay;
using System.Collections.Generic;
using Core.Units;

/// <summary>
/// 전투 씬의 UI 요소들을 관리하고, TurnManager로부터 이벤트를 받아 UI를 갱신합니다.
/// 이 클래스는 게임 로직을 담고 있지 않으며, 오직 화면 표시와 사용자 입력 전달에만 집중합니다.
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private UnitPlacementHandler _unitPlacementHandler;
    [SerializeField] private BattleCameraController _cameraController;

    [Header("Token Database")]
    [SerializeField] private List<TokenData> _tokenDatabase;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private GameObject[] _selectionPanels;
    [SerializeField] private Button[] _firstPickButtons;
    [SerializeField] private Button[] _secondPickButtons;
    [SerializeField] private Button[] _thirdPickButtons;
    [SerializeField] private Image[] _playerSilverTokens;
    [SerializeField] private Image[] _pcSilverTokens;
    [SerializeField] private Image[] _playerRoundWinIcons;
    [SerializeField] private Image[] _pcRoundWinIcons;
    [SerializeField] private GameObject[] _silverTokenPanels;
    [SerializeField] private Button _batchCompleteButton;
    [SerializeField] private Image[] _remainingSilverToken;
    [SerializeField] TextMeshProUGUI _remainingTokenMessage;

    private int _pcFirstPickedUnitId = 0;

    // UI 업데이트에 사용될 상수를 정의하여 하드코딩을 방지하고 일관성을 유지합니다.
    //private const float UIPanelSwitchDuration = 0.2f;
    private readonly Color RoundWinIconColor = Color.red;

    private void Start()
    {
        // 인스펙터에서 참조가 할당되지 않았을 경우에 대한 안전장치입니다.
        // 다만, FindObjectOfType은 비용이 높은 연산이므로 가능한 한 인스펙터에서 직접 할당하는 것을 권장합니다.
        if (_turnManager == null)
            _turnManager = FindObjectOfType<TurnManager>();

        SubscribeToEvents();
        SwitchSilverTokenPanel(false);
        SwitchPanelByIndex(4);
        _batchCompleteButton.gameObject.SetActive(false);
        SwitchRemainingSilverToken(false);

        UpdateStatusText(" ");
    }

    

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독을 해제하여 메모리 누수를 방지합니다.
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// TurnManager의 이벤트를 구독하여 게임 상태 변경에 따라 UI가 반응하도록 설정합니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (_turnManager == null) return;
        _turnManager.OnStatusUpdated += UpdateStatusText;
        _turnManager.OnNewRoundStarted += InitializeRound;
        _turnManager.OnRoundEnded += HandleEndStep;
        _turnManager.OnPCUnitSelectRequested += OnPCUnitSelect;
        _turnManager.OnShuffleCompleted += HandleShuffleStep;
        _turnManager.OnRemovePhaseStarted += HandleRemoveStep;
        _turnManager.OnCardRemoved += OnCardRemoved;
        _turnManager.OnFinalPickPhaseStarted += HandlePickStep;
        _turnManager.OnFinalPickCompleted += OnFinalPickCompleted;
        _turnManager.OnBatchPhaseStarted += HandleBatchStep;
    }

    /// <summary>
    /// 메모리 누수 방지를 위해 구독했던 모든 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_turnManager == null) return;
        _turnManager.OnStatusUpdated -= UpdateStatusText;
        _turnManager.OnNewRoundStarted -= InitializeRound;
        _turnManager.OnRoundEnded -= HandleEndStep;
        _turnManager.OnPCUnitSelectRequested -= OnPCUnitSelect;
        _turnManager.OnShuffleCompleted -= HandleShuffleStep;
        _turnManager.OnRemovePhaseStarted -= HandleRemoveStep;
        _turnManager.OnCardRemoved -= OnCardRemoved;
        _turnManager.OnFinalPickPhaseStarted -= HandlePickStep;
        _turnManager.OnFinalPickCompleted -= OnFinalPickCompleted;
        _turnManager.OnBatchPhaseStarted -= HandleBatchStep;
    }

    #region Event Handlers from TurnManager

    /// <summary>
    /// TurnManager로부터 상태 메시지를 받아 UI 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateStatusText(string status)
    {
        _statusText.text = status;
    }

    /// <summary>
    /// 새 라운드 시작 시, UI를 초기 상태로 리셋하기 위해 호출됩니다.
    /// </summary>
    private void InitializeRound()
    {
        _pcFirstPickedUnitId = 0;

        SwitchPanelByIndex(0);
        SwitchSilverTokenPanel(false);

        for (int i = 0; i < _firstPickButtons.Length; i++)
        {
            _firstPickButtons[i].image.sprite = _tokenDatabase[i].icon_1;
            _firstPickButtons[i].interactable = true;
        }
    }

    /// <summary>
    /// PC가 첫 유닛을 선택했을 때, 해당 유닛 버튼을 비활성화하기 위해 호출됩니다.
    /// </summary>
    private void OnPCUnitSelect(int unitId)
    {
        if (_pcFirstPickedUnitId == 0)
        {
            _pcFirstPickedUnitId = unitId;
            FlipAndDisableButton(unitId - GameConstants.FirstPickButtonIdOffset);
        }
    }

    /// <summary>
    /// 카드 셔플 후, 공개된 카드를 화면에 표시하기 위해 호출됩니다.
    /// Self-Review: _tokenDatabase.Find는 데이터가 많아질 경우 성능 저하를 유발할 수 있습니다.
    /// 데이터베이스 크기가 커질 경우, Awake 시점에 Dictionary로 캐싱하는 전략을 고려해야 합니다.
    /// </summary>
    private void HandleShuffleStep(UnitType userPublic, UnitType computerPublic, int userSelect, int computerSelect)
    {
        SwitchPanelByIndex(3);

        ShowRemainingTokens(userSelect, computerSelect);

        TokenData dataPlayer = _tokenDatabase.Find(t => t.unitType == userPublic);
        TokenData dataPc = _tokenDatabase.Find(t => t.unitType == computerPublic);

        for (int i = 0; i < _secondPickButtons.Length; i++)
        {
            _secondPickButtons[i].image.sprite = (i == 0 && dataPlayer != null) ? dataPlayer.icon_3 : _tokenDatabase[0].icon_4;
            //_playerSilverTokens[i].sprite = (i == 0 && dataPlayer != null) ? dataPlayer.icon_3 : _tokenDatabase[0].icon_4;
            _pcSilverTokens[i].sprite = (i == 0 && dataPc != null) ? dataPc.icon_3 : _tokenDatabase[0].icon_4;
            _secondPickButtons[i].interactable = true;
        }
    }

    List<TokenData> remainingTokenDatas = new List<TokenData>();

    private void ShowRemainingTokens(int userSelect, int computerSelect)
    {
        remainingTokenDatas.Clear();

        foreach(var data in _tokenDatabase)
        {
            if(data.tokenId != userSelect && data.tokenId != computerSelect)
            {
                remainingTokenDatas.Add(data);
            }
        }

        for (int i = 0; i < _remainingSilverToken.Length; i++)
        {
            _remainingSilverToken[i].sprite = remainingTokenDatas[i].icon_3;
        }

        SwitchRemainingSilverToken(true);
    }

    /// <summary>
    /// 플레이어가 카드를 버리는 단계가 되었음을 알리고, 관련 UI를 활성화하기 위해 호출됩니다.
    /// </summary>
    private void HandleRemoveStep()
    {
        foreach (var btn in _secondPickButtons)
            btn.interactable = true;

        SwitchPanelByIndex(1);
        SwitchSilverTokenPanel(true);
    }

    /// <summary>
    /// 플레이어가 카드를 버린 후, 남은 카드들을 UI에 다시 표시하기 위해 호출됩니다.
    /// Self-Review: _tokenDatabase.Find는 데이터가 많아질 경우 성능 저하를 유발할 수 있습니다.
    /// 데이터베이스 크기가 커질 경우, Awake 시점에 Dictionary로 캐싱하는 전략을 고려해야 합니다.
    /// </summary>
    private void OnCardRemoved(int removedIndex, UnitType typeA, UnitType typeB)
    {
        _secondPickButtons[removedIndex].interactable = false;
        _secondPickButtons[removedIndex].image.sprite = _tokenDatabase[0].icon_4;

        TokenData data1 = _tokenDatabase.Find(t => t.unitType == typeA);
        TokenData data2 = _tokenDatabase.Find(t => t.unitType == typeB);

        int currentRemainingIndex = 0;
        for (int i = 0; i < _secondPickButtons.Length; i++)
        {
            if (i == removedIndex) continue;

            TokenData currentData = (currentRemainingIndex == 0) ? data1 : data2;
            if (currentData != null)
            {
                _secondPickButtons[i].image.sprite = currentData.icon_3;
                _thirdPickButtons[currentRemainingIndex].image.sprite = currentData.icon_3;
                _secondPickButtons[i].interactable = true;
                currentRemainingIndex++;
            }
        }
        SwitchSilverTokenPanel(false);
    }

    /// <summary>
    /// 플레이어가 남은 카드 중 하나를 가져가는 단계가 되었음을 알리고, 관련 UI를 활성화하기 위해 호출됩니다.
    /// </summary>
    private void HandlePickStep()
    {
        SwitchPanelByIndex(2);
        foreach (var btn in _thirdPickButtons)
            btn.interactable = true;
    }

    /// <summary>
    /// 플레이어와 PC의 최종 카드 선택이 완료되었을 때 호출됩니다.
    /// 이 단계에서 선택된 유닛 ID를 기반으로 추가적인 UI 피드백(예: 이펙트)을 줄 수 있습니다.
    /// </summary>
    private void OnFinalPickCompleted(int userId, int computerId)
    {
        Logger.Log($"Player picked {userId}, PC picked {computerId}");
    }

    /// <summary>
    /// 유닛 배치 단계가 시작되었을 때, 배치 관련 UI를 활성화하고 카메라 뷰를 전환합니다.
    /// </summary>
    private void HandleBatchStep()
    {
        SwitchPanelByIndex(3);
        _cameraController.SwitchToBatchView();
        _batchCompleteButton.gameObject.SetActive(true);
        _batchCompleteButton.enabled = true;
        if (_unitPlacementHandler != null)
        {
            // Note: 실제 배치 로직은 UnitPlacementHandler에서 TurnManager와 상호작용하여 처리해야 합니다.
            // _unitPlacementHandler.EnablePlacement( ... );
        }

        SwitchRemainingSilverToken(false);
    }

    /// <summary>
    /// 라운드 종료 시, 승/패 결과를 UI에 아이콘으로 표시하기 위해 호출됩니다.
    /// </summary>
    private void HandleEndStep(RoundResult roundResult)
    {
        _statusText.text = $"라운드 승리 = {roundResult.GameResult}";

        for (int i = 0; i < _playerRoundWinIcons.Length; i++)
        {
            if (i < roundResult.UserWinCount)
                _playerRoundWinIcons[i].color = RoundWinIconColor;

            if (i < roundResult.ComputerWinCount)
                _pcRoundWinIcons[i].color = RoundWinIconColor;
        }
    }

    #endregion

    #region UI Event Handlers (Called from UI Components)

    /// <summary>
    /// [UI Event] 플레이어가 첫 유닛 토큰 버튼을 클릭했을 때 호출됩니다.
    /// 이 입력은 TurnManager로 전달되어 게임 로직을 진행시킵니다.
    /// </summary>
    /// <param name="index">플레이어가 클릭한 버튼의 UI 인덱스</param>
    public void OnFirstPickTokenClicked(int index)
    {
        if (_turnManager.TryPlayerFirstPick(index, _pcFirstPickedUnitId))
        {
            FlipAndDisableButton(index);
        }
    }

    /// <summary>
    /// [UI Event] 플레이어가 버릴 카드 버튼을 클릭했을 때 호출됩니다.
    /// TurnManager에 카드 제거를 요청하며, 성공 여부에 따른 UI 변경은 OnCardRemoved 이벤트 핸들러가 담당합니다.
    /// </summary>
    /// <param name="index">플레이어가 클릭한 버튼의 UI 인덱스</param>
    public void OnRemoveTokenClicked(int index)
    {
        _turnManager.TryPlayerRemoveCard(index);
    }

    /// <summary>
    /// [UI Event] 플레이어가 남은 두 카드 중 가져갈 카드 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    /// <param name="index">플레이어가 클릭한 버튼의 UI 인덱스</param>
    public void OnThirdPickTokenClicked(int index)
    {
        if (_turnManager.TryPlayerPickRemainCard(index))
        {
            _thirdPickButtons[index].interactable = false;
        }
    }

    /// <summary>
    /// [UI Event] 플레이어가 '배치 완료' 버튼을 클릭했을 때 호출됩니다.
    /// 배치 모드를 종료하고, 배치 정보를 TurnManager에 전달한 후, 전투 카메라로 전환합니다.
    /// </summary>
    public void OnBatchCompleteClicked()
    {
        _batchCompleteButton.gameObject.SetActive(false);

        if (_unitPlacementHandler != null)
            _unitPlacementHandler.DisablePlacement();

        // TODO: 실제 유저의 유닛 배치 정보를 UnitPlacementHandler에서 가져와 전달해야 합니다.
        _turnManager.CompleteBatching(null);

        _cameraController.SwitchToBattleView();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 인덱스에 해당하는 UI 선택 패널만 활성화하고 나머지는 비활성화합니다.
    /// 이는 현재 게임 단계에 맞는 UI를 사용자에게 보여주기 위함입니다.
    /// </summary>
    private void SwitchPanelByIndex(int index)
    {
        for (int i = 0; i < _selectionPanels.Length; i++)
        {
            if (_selectionPanels[i] != null)
                _selectionPanels[i].SetActive(i == index);
        }
    }

    /// <summary>
    /// 실버 토큰 관련 패널의 활성화 상태를 일괄적으로 변경합니다.
    /// </summary>
    private void SwitchSilverTokenPanel(bool isActive)
    {
        // foreach (var panel in _silverTokenPanels)
        //     panel.SetActive(isActive);
        _silverTokenPanels[0].SetActive(isActive);
    }

    private void SwitchRemainingSilverToken(bool isActive)
    {
        _remainingTokenMessage.gameObject.SetActive(isActive);

        foreach (var token in _remainingSilverToken)
        {
            token.gameObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// 특정 버튼을 뒤집힌 이미지로 바꾸고 비활성화합니다.
    /// 이는 이미 선택된 카드임을 시각적으로 표현하기 위함입니다.
    /// </summary>
    private void FlipAndDisableButton(int id)
    {
        if (id < 0 || id >= _firstPickButtons.Length) return;
        _firstPickButtons[id].interactable = false;
        _firstPickButtons[id].image.sprite = _tokenDatabase[id].icon_2;
    }

    #endregion
}