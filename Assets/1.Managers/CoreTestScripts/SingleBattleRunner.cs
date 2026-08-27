using Core.BattleObject;
using Core.Client;
using Core.FxMath;
using Core.Generators.Sink;
using Core.Payloads;
using Core.Payloads.Requests;
using Core.Sink;
using Core.Sink.Events;
using Core.Units;
using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class SingleBattleRunner : MonoBehaviour
{
    BattleAttackObj bao;
    bool _canDecode = false;

    [Header("For Debug: Left Character Set")]
    public CharacterIndex L0;
    public CharacterIndex L1;
    public CharacterIndex L2;

    [Header("For Debug: Right Character Set")]
    public CharacterIndex R0;
    public CharacterIndex R1;
    public CharacterIndex R2;

    public bool IsPlaybackFinished { get; private set; }

    public void BegindBattle()
    {
        IsPlaybackFinished = false;
        _isFinishedBlock = false;
    }
    [Serializable]
    public struct UnitPrefabMap
    {
        public int baseStatID;
        public GameObject prefab;
    }

    [System.Serializable]
    public struct BattleObjPair
    {
        public int key;
        public GameObject value;
    }

    [SerializeField]
    struct OneTickSnapShotBuffer // 틱이 해석되는 순간, 해당 틱에 저장되어 있는 행동들(이동, 방향, 스테이트만 포함)을 저장하는 버퍼. LateUpdate에서 행동이 적용될 때까지 유지되어야 함.
    {
        public bool hasMove;
        public Vector3 before, after;

        public bool hasChangeState;
        public UnitAct afterState;
        public int dir;
    }
    [SerializeField]
    Dictionary<int, OneTickSnapShotBuffer> _oneTickSnapShotBuffer = new();

    private GameObject[] _leftSlotObjs = new GameObject[3];
    private GameObject[] _rightSlotObjs = new GameObject[3];
    private int _nextLeftBind = 0;
    private int _nextRightBind = 0;

    [Header("Unit/BattleObj Prefabs")]
    public List<UnitPrefabMap> prefabMaps = new();
    public GameObject defaultUnitPrefabs; // 일단 유닛 하나로 만들었다가, 나중에 리스트로 받아 여러 유닛 테스트
    public GameObject defaultProjectilePrefab;
    public GameObject[] projecttilePrefabs; // 없으면 위 디폴트로
    readonly Dictionary<int, int> _unitBaseStatByUnitId = new();
    readonly Dictionary<int, (UnitAct act, float time)> _lastCastByUnitId = new();
    const float CAST_TO_HIT_WINDOW = .5f;

    [Header("BattleObj Binders")]
    readonly Dictionary<int, GameObject> _battleObjs = new();
    readonly Dictionary<int, List<(int xRaw, int yRaw)>> _pendingBattleObjMoves = new();
    readonly Dictionary<int, Vector2> _battleObjLastPos = new();

    Dictionary<int, GameObject> _prefabByStatID = new();

    [Header("BO Debugs")]
    [SerializeField] private List<BattleObjPair> battleObjDebugView = new();


    [Header("Slots overrides")]
    public GameObject[] leftSlotOverrides = new GameObject[3];
    public GameObject[] rightSlotOverrides = new GameObject[3];


    [Header("Rows")]
    public Transform[] leftRows = new Transform[3];
    public Transform[] rightRows = new Transform[3];

    [Header("Core Init")]
    public int seed = 0;
    public int[] leftBaseStatIds = new int[3] // 좌측 3개
        { 0, 1, 1};
    public int[] rightBaseStatIds = new int[3] // 우측 3개
        { 1, 1, 0};
    public bool batchIndexIsOneBased = true;

    TickBlockPipeline _pipe;
    readonly Dictionary<int, GameObject> _unitGameObjs = new();
    
    [SerializeField]
    [Header("Tick")]
    TickBlock _curBlock;
    public float _tickTimeUp = 1f;
    const int TICKS_PER_SEC = 30; // tickblock.TickCount = 30hz
    float _curBlockDurSec = 0f;
    int curTick = 0;
    int curOffset = 0;
    bool _ishasBlock = false;
    bool _isFinishedBlock = false;
    const float FIXED_INTERPOLARTION_TICK_DURATION = 1f / TICKS_PER_SEC;

    [SerializeField]
    float groundY = -2.25f;
    UnitInfoPanel _unitInfoPanel;

    int maxTicksPerFrame = 2;
    int tickCount = 0;

    struct MoveLerp
    {
        public Transform tr;
        public Vector3 from, 
                       to;
        public float startTime,
                     duration;
    }
    Dictionary<int, MoveLerp> _move = new Dictionary<int, MoveLerp>();

    float Fixer(int raw) => new Q24_8(raw).ToFloat();
    float Fixed16_16ToFloat(int raw) => raw * (1.0f / 65536.0f);

    Dictionary<int, Vector2> _forDebugLastMove = new();


    [SerializeField]
    [Header("UnitId 미정 슬롯")]
    readonly List<GameObject> _pendingLeft = new(3);
    readonly List<GameObject> _pendingRight = new(3);

    readonly Dictionary<int, List<(int actType, int dir)>> _pendingAct = new();

    [SerializeField]
    public enum Ground { XY, XZ }
    public Ground ground = Ground.XY;
    readonly List<int> _tempMoveKeys = new(16);

    SpawnUnit[] inUnits = new SpawnUnit[6];
    InitialUnitInfo[] outUnits = new InitialUnitInfo[6];

    [Header("Pre Pipeline, intro line")]
    [SerializeField] bool _useIntroSequenceFlag = true;
    [SerializeField] float standDuration = 0.6667f;
    /// <summary>
    /// Idle -> pipeline delay Time
    /// </summary>
    [SerializeField] float delayAfterIdle = 3.0f;

    /// <summary>
    /// 2D 월드 포지션을 3D 월드 포지션으로 변환하여 설정하는 유틸리티 메서드. ground 설정에 따라 XY 또는 XZ 평면에 위치하도록 조정.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    void SetWorldPos(Transform t, float x, float y)
    {
        if (ground == Ground.XY) t.position = new Vector3(x, y, -1f);
        else t.position = new Vector3(x, groundY, y);
    }

    /// <summary>
    /// 딕셔너리와 리스트를 초기화하여 버퍼와 상태를 클리어하는 용도의 메소드. 
    /// 해당 스크립트가 시작될 때 반드시 가장 처음 호출되어야 함. 
    /// Init()에 기본적으로 추가되어 있음.
    /// </summary>
    void Clean()
    {
        _oneTickSnapShotBuffer.Clear();
        //prefabMaps.Clear();
        _unitBaseStatByUnitId.Clear();
        _lastCastByUnitId.Clear();
        _battleObjs.Clear();
        _pendingBattleObjMoves.Clear();
        _battleObjLastPos.Clear();
        _prefabByStatID.Clear();
        battleObjDebugView.Clear();
        _unitGameObjs.Clear();
        _move.Clear();
        _pendingLeft.Clear();
        _pendingRight.Clear();
        _pendingAct.Clear();
        _tempMoveKeys.Clear();

    }

    /// <summary>
    /// 파라메터의 게임오브젝트를 받아 캐릭터 인덱스 enum 번호를 반환함.
    /// 시작 시점에서의 Init 용
    /// </summary>
    /// <param name="character"> Init 해야 하는 캐릭터의 게임 오브젝트</param>
    /// <returns></returns>
    CharacterIndex ResolveRowCharacerInList(GameObject character)
    {
        string characterType = character.GetComponent<SkeletonAnimation>().skeletonDataAsset.name;

        switch (characterType)
        {
            case "11001_SkeletonData":
                return CharacterIndex.Lv1Def;

            case "21001_SkeletonData":
                return CharacterIndex.Lv1Assert;

            case "31001_SkeletonData":
                return CharacterIndex.Lv1Ranger;

            case "41001_SkeletonData":
                return CharacterIndex.Lv1Magican;

            case "51001_SkeletonData":
                return CharacterIndex.Lv1Sup;

            case "12001_SkeletonData":
                return CharacterIndex.Lv2Def;

            case "22001_SkeletonData":
                return CharacterIndex.Lv2Assert;

            case "32001_SkeletonData":
                return CharacterIndex.Lv2Ranger;

            case "42001_SkeletonData":
                return CharacterIndex.Lv2Magican;

            case "52001_SkeletonData":
                return CharacterIndex.Lv2Sup;

            default:
                //Debug.LogError("unexpected error");
                break;
        }
        return CharacterIndex.Lv1Def;
    }

    /// <summary>
    /// 파라메터의 게임오브젝트, 반전 플레그, 해당 유닛의 액트를 받아 캐릭터리스트 enum 번호를 반환함.
    /// 사운드 출력용
    /// 현재 발터는 여기서 스킬이 출력이 되고 있음. 
    /// 스파게티 코드의 위험이 있으나, 중복으로 발생하는 스킬 출력을 막기 위해 발터의 스킬 이펙트는 여기서 출력됨.
    /// </summary>
    /// <param name="character"> 사운드를 출력해야 하는 캐릭터 게임 오브젝트 </param>
    /// <param name="reverse"> 캐릭터가 앞을 보고 있는지, 뒤를 보고 있는지에 대한 bool 값 (발터를 위해 사용 중) </param>
    /// <param name="act"> 캐릭터가 지금 어떤 행동인지에 대한 플래그 값 </param>
    /// <returns></returns>
    CharList ResolveSoundCharacerInList(GameObject character, bool reverse, UnitAct act)
    {
        string characterType = character.GetComponent<SkeletonAnimation>().skeletonDataAsset.name;

        switch (characterType)
        {
            case "11001_SkeletonData":
                return CharList.LV1_GUARDIAN;

            case "21001_SkeletonData":
                return CharList.LV1_ASSAULTER;

            case "31001_SkeletonData":
                return CharList.LV1_RANGER;

            case "41001_SkeletonData":
                return CharList.LV1_WIZARD;

            case "51001_SkeletonData":
                return CharList.LV1_SUPPORTER;

            case "12001_SkeletonData":
                return CharList.LV2_GUARDIAN;

            case "22001_SkeletonData":
                return CharList.LV2_ASSAULTER;

            case "32001_SkeletonData":
                {
                    if(act == UnitAct.Skill)
                        EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Walter, new Vector3(character.transform.position.x, -3.95f, -0.04f), 1.3f, reverse, EffectManager.EndMode.NonEmission);
                    return CharList.LV2_RANGER;
                }
            case "42001_SkeletonData":
                return CharList.LV2_WIZARD;

            case "52001_SkeletonData":
                return CharList.LV2_SUPPORTER;

            default:
                //Debug.LogError("unexpected error");
                break;
        }
        return CharList.LV1_GUARDIAN;
    }

    /// <summary>
    /// UnitAct를 SoundStateType으로 인버팅하여 반환하는 메소드.
    /// AudioManager에서 사운드 출력을 담당하는 메소드를 사용하기 위해 사용.
    /// </summary>
    /// <param name="act">해당 유닛의 UnitAct</param>
    /// <returns></returns>
    SoundStateType ResolveSoundSoundStateTypeInList(UnitAct act)
    {
        switch (act)
        {
            case UnitAct.Attack:
                return SoundStateType.ATTACK;

            case UnitAct.Attack2:
                return SoundStateType.ATTACK;

            case UnitAct.Move:
                return SoundStateType.WALK;

            case UnitAct.Skill:
                return SoundStateType.SKILL;

            case UnitAct.Idle:
                return SoundStateType.IDLE;

            case UnitAct.Die:
                return SoundStateType.DEATH;

            case UnitAct.Channeling:
                return SoundStateType.STAND;

            default:
                //Debug.LogError("unexpected error");
                break;
        }
        return SoundStateType.FREEZE;
    }

    /// <summary>
    /// 전투 오브젝트들을 전투가 끝난 이후 모두 클리어하고 남은 잔여 게임 오브젝트를 제거하는 메소드.
    /// 한 라운드가 끝난 이후 반드시 호출되어야 함.
    /// </summary>
    public void BOCleaner()
    {
        foreach(KeyValuePair<int, GameObject> gameobj in _battleObjs)
        {
            Destroy(gameobj.Value.gameObject);
        }

        _battleObjs.Clear();
        _pendingBattleObjMoves.Clear();
        _battleObjLastPos.Clear();
    }

    /// <summary>
    /// 초기화 메소드. 좌우 양쪽의 파이프라인과 유닛 프리팹, 행 변환 및 캐릭터 슬롯을 지정된 각 트랜스폼으로 받아와 초기화함.
    /// 초기화가 끝난 이후, 인트로 시퀀스를 시작함.
    /// </summary>
    public void Init()
    {
        _pipe = new TickBlockPipeline();
        Clean();

        foreach (var m in prefabMaps)
        {
            if(m.prefab != null) _prefabByStatID[m.baseStatID] = m.prefab;
        }

        _unitInfoPanel = FindFirstObjectByType<UnitInfoPanel>(FindObjectsInactive.Include);

        leftRows[0] = GameObject.Find("L0").transform;
        leftRows[1] = GameObject.Find("L1").transform;
        leftRows[2] = GameObject.Find("L2").transform;


        rightRows[0] = GameObject.Find("R0").transform;
        rightRows[1] = GameObject.Find("R1").transform;
        rightRows[2] = GameObject.Find("R2").transform;


        //new WaitForSeconds(3f);

        // 좌측 캐릭터 초기화
        for (int i = 0; i < 3; i++)
        {
            var lCharacter = leftRows[i].GetChild(0);
            //Debug.Log(lCharacter.name);
            leftSlotOverrides[i] = lCharacter.gameObject;
            inUnits[i] = new SpawnUnit(0, (int)ResolveRowCharacerInList(lCharacter.gameObject), (byte)(3 - i));

            var leftPrefab = ResolveUnitPrefabForSlot(true, i, inUnits[i].BaseStatId);
            var metaTagL = lCharacter.GetComponent<UnitMetaTag>() ?? lCharacter.AddComponent<UnitMetaTag>();
            metaTagL.baseStatId = inUnits[i].BaseStatId;
            lCharacter.name = $"L_{i}";
            //lCharacter.GetComponent<SnapPosition>().enabled = false;
            _pendingLeft.Add(lCharacter.gameObject);

            _leftSlotObjs[i] = lCharacter.gameObject;
        }

        // 우측 캐릭터 초기화
        for (int i = 0; i < 3; i++)
        {
            var RCharacter = rightRows[i].GetChild(0);
            //Debug.Log(RCharacter.name);
            rightSlotOverrides[i] = RCharacter.gameObject;
            inUnits[i+3] = new SpawnUnit(1, (int)ResolveRowCharacerInList(RCharacter.gameObject), (byte)(10 + i));

            var rightPrefab = ResolveUnitPrefabForSlot(false, i, inUnits[i + 3].BaseStatId);
            var metaTagR = RCharacter.GetComponent<UnitMetaTag>() ?? RCharacter.AddComponent<UnitMetaTag>();
            metaTagR.baseStatId = inUnits[i + 3].BaseStatId;
            RCharacter.name = $"R_{i}";
            //RCharacter.GetComponent<SnapPosition>().enabled = false;
            _pendingRight.Add(RCharacter.gameObject);

            _rightSlotObjs[i] = RCharacter.gameObject;
            _unitInfoPanel.SearchSecondAttackCharacterList(RCharacter.gameObject);
        }

        Span<SpawnUnit> temp = inUnits;

        bool test = _pipe.Init(seed, temp, outUnits);
        //Debug.Log(test);
        //Debug.Log($"RowL0 world={leftRows[0].position} RowL1 world={leftRows[1].position} RowL2 world={leftRows[2].position}");
        //Debug.Log($"Spawned L0 world={_pendingLeft[0].transform.position} parent={_pendingLeft[0].transform.parent.name}");

        _canDecode = !_useIntroSequenceFlag;
        
        if(_useIntroSequenceFlag)
        {
            StartCoroutine(CoruIntro());
            //Debug.Log("Intro In?");
        }
    }


    #region Intro

    /// <summary>
    /// 생성된 모든 앤티티에 상태머신을 적용 (Stand -> Idle) 시간 지연을 변수로 적용하여 인트로 시퀀스를 실행함.
    /// </summary>
    /// <returns>코루틴 실행을 위한 enumerator</returns>
    IEnumerator CoruIntro()
    {
        ApplyStanToAllSpawned();

        if (standDuration > 0f)
        {
            yield return new WaitForSeconds(standDuration);
        }

        ApplyIdleToAllSpawned();

        if (delayAfterIdle > 0f)
        {
            yield return new WaitForSeconds(delayAfterIdle);
        }

        _canDecode = true;
    }

    /// <summary>
    /// 생성된 유닛들을 전부 Idle 상태로 초기화하기 위한 메소드
    /// ApplyIdle(GameObject) 메소드를 활용하여, 현재 pending 상태인 유닛들과 이미 생성되어 있는 유닛들 모두에게 Idle 상태를 적용함.
    /// </summary>
    private void ApplyIdleToAllSpawned()
    {
        for (int i = 0; i < _pendingLeft.Count; i++)
        {
            //Debug.Log($"{_pendingLeft.Count} Idle");
            ApplyIdle(_pendingLeft[i]);
        }
        for (int i = 0; i < _pendingRight.Count; i++)
        {
            //Debug.Log($"{_pendingRight.Count} Idle");
            ApplyIdle(_pendingRight[i]);
        }

        foreach(var kv in _unitGameObjs)
        {
            ApplyIdle(kv.Value);
        }
    }

    /// <summary>
    /// 캐릭터 게임 오브젝트를 받아, 받아온 파라메터의 캐릭터 게임 오브젝트에 Idle을 적용하는 메소드.
    /// 인트로 시퀀스에 사용되며, ApplyIdleToAllSpawned() 메소드에서 활용됨.
    /// </summary>
    /// <param name="gameObject">Idle 상태로 바꾸고 싶은 캐릭터 게임 오브젝트</param>
    private void ApplyIdle(GameObject gameObject)
    {
        if (!gameObject) return;
        var ch = gameObject.GetComponent<Character>();
        if(!ch) return;

        ch.ApplyState(UnitAct.Idle);
    }

    /// <summary>
    /// ApplyStand(GameObject) 메소드를 활용하여 양측의 모든 소환 유닛의 상태를 전부 Stand 상태로 바꾸는 동시에, 
    /// 단상의 알파값과 캐릭터의 체력바 마나바의 알파값을 바꾸는 용도의 메소드
    /// </summary>
    private void ApplyStanToAllSpawned()
    {

        for (int i = 0; i < _pendingLeft.Count; i++)
        {
            //Debug.Log($"{_pendingLeft.Count} {i} Left Stand");
            _pendingLeft[i].GetComponentInChildren<AlphaChanger>().ToZeroAlphaSR(0.6f);
            _pendingLeft[i].GetComponentInChildren<CharacterMPHPFade>().FadeIn(0.6f);
            ApplyStand(_pendingLeft[i]);
        }
        for (int i = 0; i < _pendingRight.Count; i++)
        {
            //Debug.Log($"{_pendingRight.Count} {i} Right Stand");
            _pendingRight[i].GetComponentInChildren<AlphaChanger>().ToZeroAlphaSR(0.6f);
            _pendingRight[i].GetComponent<Character>().SetDir(-1);
            _pendingRight[i].GetComponentInChildren<CharacterMPHPFade>().FadeIn(0.6f);
            ApplyStand(_pendingRight[i]);
        }

        foreach (var kv in _unitGameObjs)
        {
            ApplyStand(kv.Value);
        }
    }

    /// <summary>
    /// 캐릭터 게임 오브젝트를 받아, 받아온 파라메터의 캐릭터 게임 오브젝트에 Stand를 적용하는 메소드.
    /// 인트로 시퀀스에 사용되며, ApplyStanToAllSpawned() 메소드에서 활용됨.
    /// </summary>
    private void ApplyStand(GameObject gameObject)
    {
        if (!gameObject) return;
        var ch = gameObject.GetComponent<Character>();
        if (!ch) return;

        ch.ApplyState(UnitAct.Idle, true); // temp
    }

    #endregion

    private void Update()
    {
        //Debug.Log(IsPlaybackFinished);
        if (_isFinishedBlock)
        {
            IsPlaybackFinished = true;
            //Debug.Log("_isFinishied Flag");
            return;
        }
        if (!_canDecode)
        {
            //Debug.Log("_canDecode Flag");
            return;
        }
        _curBlockDurSec += Time.unscaledDeltaTime * TICKS_PER_SEC;

        if (_curBlockDurSec >= 1f)
        {
            DecodeOneTick();
            _curBlockDurSec %= 1f;
         }
    }

    /// <summary>
    /// 반드시 LateUpdate를 사용하여야 함.
    /// 최종 위치 업데이트 및 기타 동작에 있어 기존 행동들이 저장이 되어 있는 _oneTickSnapShotBuffer의 데이터를 기반으로 행동이 적용되어야 하는데, 
    /// Update에서 바로 적용이 될 경우, 기존 행동들이 저장이 되어 있는 _oneTickSnapShotBuffer의 데이터가 업데이트 도중에 변경이 되거나 꼬일 수 있기 때문.
    /// Update는 틱을 해석하는 용도로 사용.
    /// </summary>
    private void LateUpdate()
    {
        if (_oneTickSnapShotBuffer.Count == 0) return;

        _tempMoveKeys.Clear();
        _tempMoveKeys.AddRange(_oneTickSnapShotBuffer.Keys);
        //float now = Time.unscaledTime;

        float t = Mathf.Clamp01(_curBlockDurSec);
        // Debug
        //battleObjDebugView.Clear();
        //foreach (var kv in _battleObjs)
        //    battleObjDebugView.Add(new BattleObjPair { key = kv.Key, value = kv.Value });

        foreach (var id in _tempMoveKeys)
        {
            if(!_unitGameObjs.TryGetValue(id, out var unitObj) || !unitObj)
            {
                _oneTickSnapShotBuffer.Remove(id);
                continue;
            }

            var tr = unitObj.transform;
            var sc_char = unitObj.GetComponent<Character>();
            var otssb = _oneTickSnapShotBuffer[id];

            // set state line
            if(otssb.hasChangeState && sc_char)
            {
                sc_char.SetDir(Mathf.Clamp(otssb.dir, -1, 1));
                sc_char.ApplyState(otssb.afterState);
                otssb.hasChangeState = false;
            }

            // set Move Line
            if (otssb.hasMove)
            {
                var point = Vector3.Lerp(otssb.before, otssb.after, t);
                if (ground == Ground.XY) point.z = 0f;
                else point.y = groundY;

                tr.position = point;

                // is it end?
                if(t >= 0.999f) otssb.hasMove = false;
                //Debug.Log($"[Debug APPLY] id={id} before={otssb.before} after={otssb.after} point={tr.position} ground={ground}");
            }

            if (!otssb.hasChangeState && !otssb.hasMove)
            {
                _oneTickSnapShotBuffer.Remove(id);
            }
            else _oneTickSnapShotBuffer[id] = otssb;
        }        

    }

    /// <summary>
    /// 틱을 하나 해석하는 메소드.
    /// 틱 해석이 완료되면, 해당 틱에 저장되어 있는 행동들(이동, 방향, 스테이트만 포함)을 _oneTickSnapShotBuffer에 저장하여, LateUpdate에서 행동이 적용될 수 있도록 함.
    /// </summary>
    /// <returns> False: 남아 있는 틱이 없음. True: 남아 있는 틱이 있음 </returns>
    bool DecodeOneTick()
    {
        // if finished block return
        if(_isFinishedBlock) return false;

        // if block come yet, get new block
        if(!_ishasBlock)
        {
            if (!_pipe.TryGetNext(out _curBlock))
            {
                 //Debug.Log("_pipe Flag");
                _isFinishedBlock = true;
                //_canDecode = false;
                //IsPlaybackFinished = false;
                return false;
            }
            _ishasBlock = true;
            curTick = 0;

            curOffset = (_curBlock.TickOffset != null && _curBlock.TickOffset.Length > 0) ? _curBlock.TickOffset[0] : 0;

        }

        // if block still remained, but used all cur block. request next block
        else if(curTick >= _curBlock.TickCount)
        {
            if(!_pipe.TryGetNext(out _curBlock))
            {
                _ishasBlock = false;
                _isFinishedBlock = true;
                return false;
            }

            _ishasBlock = true;
            curTick = 0;
            curOffset = (_curBlock.TickOffset != null && _curBlock.TickOffset.Length > 0) ? _curBlock.TickOffset[0] : 0;
        }
        //if (!_ishasBlock || curTick >= _curBlock.TickCount)
        //{
        //    if(_pipe == null) return false;
        //    if(!_pipe.TryGetNext(out _curBlock)) return false;
        //    _ishasBlock = true;
        //    curTick = 0;
        //    curOffset = (_curBlock.TickOffset != null && _curBlock.TickOffset.Length > 0) ? _curBlock.TickOffset[0] : 0;
        //}

        var span = _curBlock.EventBytes;
        if(span == null)
            span = ReadOnlySpan<byte>.Empty;

        int tickStart = (_curBlock.TickOffset != null && _curBlock.TickOffset.Length > curTick)
            ? _curBlock.TickOffset[curTick] : curOffset;
        int tickEnd = (_curBlock.TickOffset != null && _curBlock.TickOffset.Length > curTick + 1)
            ? _curBlock.TickOffset[curTick + 1] : span.Length;

        if (curOffset < tickStart) curOffset = tickStart;

        while (curOffset < tickEnd)
        {
            var type = SinkHeaderCodec.Read(span, ref curOffset);

            // update yet. will update after fix bugs
            switch (type)
            {
                // Done
                case SinkEventType.ActChange:
                    {
                        var e = ActChangeEvent.Decode(span, ref curOffset);
                        Debug.Log($"[ActChangeEvent] || Type = {type} UnitId = {e.UnitId} Unit act = {e.Act} Unit Dir = {e.Dir}");
                        HandleActChange(e.UnitId, e.Act, e.Dir);
                        break;
                    }
                // Done
                case SinkEventType.UnitMove:
                    {
                        var e = UnitMoveEvent.Decode(span, ref curOffset);
                        Debug.Log($"[UnitMoveEvent] || Type = {type} UnitId = {e.UnitId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");
                        HandleUnitMove(e.UnitId, e.NewX1616, e.NewY1616 - Q16_16.ONE);
                        break;
                    }
                case SinkEventType.StatusEffectEnd:
                    {
                        var e = UnitMoveEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[StatusEffectEnd] || Type = {type} UnitId = {e.UnitId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");

                        break;
                    }
                case SinkEventType.StatusEffectChange:
                    {
                        var e = UnitMoveEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[StatusEffectChange] || Type = {type} UnitId = {e.UnitId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");
                        break;
                    }
                // NewHp, NewShield's return value = 0.
                // Need to set by damage
                // ObjId = skill obj (battle obj)
                // If, Attacker is Lv.2 Ranger, Lv.2 Wizard, Lv.2 Guardian, need to use AttackerId
                // using AttacterId for hit effect.
                // Done(Temp)
                case SinkEventType.UnitHit:
                    {
                        var e = UnitHitEvent.Decode(span, ref curOffset);
                        Debug.Log($"[UnitHitEvent] || Type = {type} AttackerId = {e.AttackerId} Unit Damage = {Fixer(e.Damage248)} Unit IsCritical = {e.IsCritical}" +
                            $"TargetId = {e.TargetId} NewHp = {Fixer(e.NewHp248)} NewShield = {Fixer(e.NewShield248)} ObjId = {e.ObjId}");
                        HandleUnitHitHPShield(e.AttackerId, Fixer(e.Damage248), e.IsCritical, e.TargetId, Fixer(e.NewHp248), Fixer(e.NewShield248), e.ObjId);
                        break;
                    }
                // Done, but it doesn't in simulater
                case SinkEventType.Heal:
                    {
                        var e = HealEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[HealEvent] || Type = {type} TargetId = {e.TargetId} Unit NewHp = {Fixer(e.NewHp248)} Unit CasterId = {e.CasterId}");
                        HandleUnitHealHP(e.TargetId, Fixer(e.NewHp248), e.CasterId);
                        break;
                    }
                case SinkEventType.ShieldIncrease:
                    {
                        var e = ShieldIncreaseEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[ShieldIncreaseEvent] || Type = {type} TargetId = {e.TargetId} Unit NewShield = {e.NewShield} Unit CasterId = {e.CasterId}");
                        break;
                    }
                // Done
                case SinkEventType.MpChange:
                    {
                        var e = MpChangeEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[MpChangeEvent] || Type = {type} UnitId = {e.UnitId} Unit NewMp = {e.NewMp}");
                        HandleUnitSetMP(e.UnitId, e.NewMp);
                        break;
                    }

                case SinkEventType.BattleObjectSpawn:
                    {
                        var e = BattleObjectSpawnEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[BattleObjectSpawnEvent] || Type = {type} BO Type = {e.Type} ObjectId = {e.ObjId} Unit OwnerId = {e.OwnerId} Unit HalfSizeXRaw = {Fixed16_16ToFloat(e.HalfSizeX1616)}" +
                            //$"HalfSizeYRaw = {Fixed16_16ToFloat(e.HalfSizeY1616)} FacingXRaw = {Fixed16_16ToFloat(e.FacingX1616)} FacingYRaw = {Fixed16_16ToFloat(e.FacingY1616)} CenterXRaw = {Fixed16_16ToFloat(e.CenterX1616)} CenterYRaw = {Fixed16_16ToFloat(e.CenterY1616)}");
                        HandleBattleObjectSpawn(e.ObjId, e.OwnerId, e.Type, e.HalfSizeX1616, e.HalfSizeY1616, e.FacingX1616, e.FacingY1616, e.CenterX1616, e.CenterY1616);
                        break;
                    }
                // Done
                case SinkEventType.BattleObjectMove:
                    {
                        var e = BattleObjectMoveEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[BattleObjectMoveEvent] || Type = {type} ObjectId = {e.ObjId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");
                        HandleBOMove(e.ObjId, e.NewX1616, e.NewY1616);
                        break;
                    }
                // Done
                case SinkEventType.BattleObjectDespawn:
                    {
                        var e = BattleObjectDespawnEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[BattleObjectDespawnEvent] || Type = {type} ObjectId = {e.ObjId}");
                        HandleBODeSpawn(e.ObjId);
                        break;
                    }
                // Done
                case SinkEventType.DelayedBOSpawn:
                    {
                        var e = DelayedBOSpawnEvent.Decode(span, ref curOffset);
                        //Debug.Log($"[DelayedBOSpawn] || Type = {type} ObjType = {e.ObjId} {e.Type} Unit Delay = {e.Delay} Unit XRaw = {Fixed16_16ToFloat(e.X1616)} Unit YRaw = {Fixed16_16ToFloat(e.Y1616)}");
                        HandleDelayedBOSpawn(e.Type, e.X1616, e.Y1616, e.Delay);
                        break;
                    }
                default:
                    break;
            }
        }

        curTick++;
        if(curTick < _curBlock.TickCount && _curBlock.TickOffset != null)
        {
            curOffset = _curBlock.TickOffset[curTick];
        }
        return true;
    }

    /// <summary>
    /// 해당 행동이 Skill인 경우, 지정된 유닛의 스킬 시전 시각을 저장하는 함수
    /// </summary>
    /// <param name="casterId">해당 스킬을 시전하는 유닛의 고유 식별자.</param>
    /// <param name="act">해당 유닛이 수행하는 동작.</param>
    void TriggerSkillCastVFX(int casterId, UnitAct act)
    {
        if (act != UnitAct.Skill) return;

        _lastCastByUnitId[casterId] = (act, Time.unscaledTime);
    }

    /// <summary>
    /// 지정된 슬롯에 대한 유닛 프리팹을 생성, 
    /// 슬롯 오버라이드가 존재하는 경우에 그 프리팹을 우선적으로 반환하고, 
    /// 없는 경우엔 BaseStatID를 기반으로 기본 유닛 프리팹을 반환하는 함수.
    /// </summary>
    /// <param name="isLeft">Left 슬롯 오버라이드라면 True, 오른쪽이라면 False</param>
    /// <param name="slotIdx">해결할 슬롯의 인덱스 넘버/param>
    /// <param name="baseStatID">유닛의 ID (오버라이드가 없는 경우 기본 유닛 프리펩 반환)</param>
    /// <returns>지정된 슬롯에 대한 해결된 유닛 프리팹 GameObject</returns>
    GameObject ResolveUnitPrefabForSlot(bool isLeft, int slotIdx, int baseStatID)
    {
        var overrides = isLeft ? leftSlotOverrides : rightSlotOverrides;
        if(overrides != null && slotIdx >= 0 &&
            slotIdx < overrides.Length &&
            overrides[slotIdx] != null)
            return overrides[slotIdx];
        return ResolveUnitPrefabBystatID(baseStatID);
    }

    /// <summary>
    /// 지정된 ID와 연결된 유닛 프리팹을 가져오거나, 
    /// 그것이 없는 경우엔 기본 프리팹을 반환하는 함수
    /// ResolveUnitPrefabForSlot 함수에서 사용됨.
    /// </summary>
    /// <param name="basedID">유닛 프리팹을 조회하는데 사용되는 ID</param>
    /// <returns>유닛 프리팹이 발견되면 그것을 사용, 그렇지 않다면 기본 프리팹을 리턴</returns>
    GameObject ResolveUnitPrefabBystatID(int basedID)
    {
        if (_prefabByStatID.TryGetValue(basedID, out var unit) && unit) return unit;
        return defaultProjectilePrefab;
    }

    /// <summary>
    /// 지정된 타입과 포지션을 기반으로한 딜레이 배틀 오브젝트 이펙트의 생성을 처리
    /// </summary>
    /// <param name="boSE">배틀 오브젝트의 타입 (Enum)</param>
    /// <param name="delayedBOxRaw">딜레이 배틀 오브젝트의 x 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함. </param>
    /// <param name="delayedBOyRaw">딜레이 배틀 오브젝트의 Y 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함.</param>
    /// <param name="delay">효과가 생성되기까지의 지연 시간</param>
    private void HandleDelayedBOSpawn(BattleObjectType boSE, int delayedBOxRaw, int delayedBOyRaw, int delay)
    {
        float tickSec = 1f / 30f;
        switch (boSE)
        {
            case BattleObjectType.AndreaSkill:
                if(Fixer(delayedBOxRaw) > 0)
                    EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Andrea, new Vector3(Fixed16_16ToFloat(delayedBOxRaw), Fixed16_16ToFloat(delayedBOyRaw + Q16_16.ONE), -2f), delay * tickSec, false, EffectManager.EndMode.Emission);
                else
                    EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Andrea, new Vector3(Fixed16_16ToFloat(delayedBOxRaw), Fixed16_16ToFloat(delayedBOyRaw + Q16_16.ONE), -2f), delay * tickSec, true, EffectManager.EndMode.Emission);
                //Debug.Log("[HandleHit] Switch AndreaSkill In?");
                break;

                //case BattleObjectType.
            default:
                //Debug.Log("[HandleHit] Switch Defalut In?");
                break;
        }

    }

    /// <summary>
    /// 배틀 오브젝트에 대한 Move 이벤트를 처리하는 함수. 
    /// 해당 오브젝트가 존재하는 경우에는 바로 ApplyBOMove 함수를 활용하여 위치를 업데이트하고, 
    /// 존재하지 않는 경우에는 해당 이동 정보를 _pendingBattleObjMoves 딕셔너리에 저장하여, 
    /// 나중에 오브젝트가 생성되었을 때 이동이 적용될 수 있도록 함.
    /// </summary>
    /// <param name="battleObjID">배틀 오브젝트의 고유 ID</param>
    /// <param name="xBORaw">배틀 오브젝트의 이동된 x 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함.</param>
    /// <param name="yBORaw">배틀 오브젝트의 이동된 Y 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함.</param>
    private void HandleBOMove(int battleObjID, int xBORaw, int yBORaw)
    {
        if(!_battleObjs.TryGetValue(battleObjID, out var obj) || !obj)
        {
            if(!_pendingBattleObjMoves.TryGetValue(battleObjID, out var list))
            {
                _pendingBattleObjMoves[battleObjID] = list = new List<(int, int)>(4);
            }
            list.Add((xBORaw, yBORaw));
            return;
        }
        ApplyBOMove(battleObjID, xBORaw, yBORaw);
    }

    /// <summary>
    /// Fixed-point로 표현된 좌표를 받아 해당 배틀 오브젝트의 위치를 업데이트하는 함수.
    /// 만약 해당 배틀 오브젝트가 아직 생성되지 않은 상태라면,
    /// 해당 이동 정보를 _pendingBattleObjMoves 딕셔너리에 저장하여, 나중에 오브젝트가 생성되었을 때 이동이 적용될 수 있도록 함.
    /// HandleBOMove 함수에서 사용됨. 
    /// 또한, _pendingBattleObjMoves에 _HandleBOMove에서 저장하는 부분이 있으나, 
    /// 혹시 모를 문제를 방지하기 위해 한 번 더 체크하여 저장하는 형태로 구현 되어 있음.
    /// </summary>
    /// <param name="objID">배틀 오브젝트 ID.</param>
    /// <param name="xRaw">배틀 오브젝트의 이동된 x 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함.</param>
    /// <param name="yRaw">배틀 오브젝트의 이동된 Y 포지션. Fixer()를 활용하여 Float 값으로 바꿔 주어야 함.</param>
    private void ApplyBOMove(int objID, int xRaw, int yRaw)
    {
        if(!_battleObjs.TryGetValue(objID, out var gameObject))
        {
            if(!_pendingBattleObjMoves.TryGetValue(objID, out var list))
            {
                _pendingBattleObjMoves[objID] = list = new List<(int, int)>(4);
            }
            list.Add((xRaw, yRaw));
            return;
        }

        float x = Fixed16_16ToFloat(xRaw);
        float y = Fixed16_16ToFloat(yRaw);

        var pos = gameObject.transform.position;
        //gameObject.transform.position = new Vector3(x, pos.y, y);
        
        SetWorldPos(gameObject.transform, x, y);

        _battleObjLastPos[objID] = new Vector2(x, y);
    }

    /// <summary>
    /// 파라메터로 받아온 인덱스 값을 기반으로 하여, 해당 배틀 오브젝트를 제거하는 함수.
    /// 동시에 배틀 오브젝트와 관련된 모든 컬렉션에서 해당 오브젝트를 제거함.
    /// </summary>
    /// <param name="despawnObjIdx">제거할 배틀 오브젝트 ID</param>
    private void HandleBODeSpawn(int despawnObjIdx)
    {
        //Debug.Log($"[BODebug][HandleBODespawn] ID Number = {despawnObjIdx}");

        if (_battleObjs.TryGetValue(despawnObjIdx, out var obj))
        {
            //Debug.Log($"[BODebug][HandleBODespawn] Destroy check");
            Destroy(obj.gameObject);
        }
        _battleObjs.Remove(despawnObjIdx);
        _pendingBattleObjMoves.Remove(despawnObjIdx);
        _battleObjLastPos.Remove(despawnObjIdx);
    }

    /// <summary>
    /// 지정된 파라메터를 기반으로 배틀 오브젝트를 생성하고 초기화하며,
    /// 위치와 방향을 설정하고, 대기 중인 Move 이벤트가 있다면 그것도 적용하는 함수.
    /// </summary>
    /// <param name="objectID">배틀 오브젝트 ID 값</param>
    /// <param name="ownerID">배틀 오브젝트의 오너 ID 값</param>
    /// <param name="type">생성된 배틀 오브젝트의 타입 (BattleObjectType Enum)</param>
    /// <param name="halfSizeX"> </param>
    /// <param name="halfSizeY"></param>
    /// <param name="facingX">향하는 방향의 X 고정 소수점 값</param>
    /// <param name="facingY">향하는 방향의 Y 고정 소수점 값</param>
    /// <param name="centerX">고정 소숫점 형식의 객체 중앙의 X 포지션 값</param>
    /// <param name="centerY">고정 소숫점 형식의 객체 중앙의 Y 포지션 값</param>
    void HandleBattleObjectSpawn(int objectID, int ownerID, BattleObjectType type, int halfSizeX, int halfSizeY, int facingX, int facingY, int centerX, int centerY)
    {
        if (_battleObjs.ContainsKey(objectID)) return;


        switch (type)
        {
            case BattleObjectType.AndreaAttack:
                bao = BattleAttackObj.AndreaAttack;
                break;

            case BattleObjectType.EmiliaAttack:
                bao = BattleAttackObj.EmiliaAttack;
                break;

            case BattleObjectType.HNunAttack:
                bao = BattleAttackObj.HNunAttack;
                break; 

            case BattleObjectType.HArcherAttack:
                bao = BattleAttackObj.HArcherAttack;
                break;

            case BattleObjectType.EmiliaAttack2:
                bao = BattleAttackObj.EmiliaAttack2;
                break;

            case BattleObjectType.HMagicionAttack:
                bao = BattleAttackObj.HMagicionAttack;
                break;

            case BattleObjectType.WalterAttack:
                bao = BattleAttackObj.WalterAttack;
                break;
        }

        //var prefab = ResolveProjectilePrefab_Fallback();
        var prefab = ResolveProjectilePrefab(bao);

        var gameobj = prefab ? Instantiate(prefab) : new GameObject($"BattleObj_{objectID}");
        _battleObjs[objectID] = gameobj;
        //Debug.Log($"[_BattleObjs Log] objectID name = {objectID} ownerID = {ownerID} gameobj Name = {gameobj.name}");

        float fixedcenterX = Fixed16_16ToFloat(centerX);
        float fixedcenterY = Fixed16_16ToFloat(centerY);

        float fixedFacingX = Fixed16_16ToFloat(facingX);
        float fixedFacingY = Fixed16_16ToFloat(facingY);

        float fixedHalfSizeX = Fixed16_16ToFloat(halfSizeX);
        float fixedHalfSizeY = Fixed16_16ToFloat(halfSizeY);

        //gameobj.transform.position = new Vector3(fixedcenterX, gameobj.transform.position.z, fixedcenterY);
        SetWorldPos(gameobj.transform, fixedcenterX, fixedcenterY);

        var localScaleOfgameobj = gameobj.transform.localScale;
        float abs = Mathf.Abs(localScaleOfgameobj.x);
        localScaleOfgameobj.x = (fixedFacingX >= 0f) ? abs : -abs;
        gameobj.transform.localScale = localScaleOfgameobj;

        if(_pendingBattleObjMoves.TryGetValue(objectID, out var move))
        {
            foreach(var (xRaw, yRaw) in move)
            {
                ApplyBOMove(objectID, xRaw, yRaw);
            }
            _pendingBattleObjMoves.Remove(objectID);
        }

        _battleObjLastPos[objectID] = new Vector2(centerX, centerY);
    }

    /// <summary>
    /// 게임 오브젝트 컬렉션에 지정된 유닛이 존재하는 경우, 
    /// 해당 유닛의 MP를 새로운 값으로 업데이트하는 함수.
    /// </summary>
    /// <param name="unitID">지정할 유닛의 ID 값</param>
    /// <param name="setMp">새로운 MP 벨류</param>
    private void HandleUnitSetMP(int unitID, int setMp)
    {
        if (!_unitGameObjs.TryGetValue(unitID, out var gameObj)) return;

        var character = gameObj.GetComponent<Character>();
        if (character) character.SetHpMp((int)character.nowHP, setMp);
        //character.GetComponent<CharacterDamageTextUI>().ShowDamage((int)(setMp - character.nowMP), DamageTextType.MP);
    }

    /// <summary>
    /// 지정된 오브젝트(캐릭터에 적용하기 위해 생성해둔 메소드. 주로, 캐릭터에 사용될 것으로 보임.)에
    /// 짧은 시간 동안 MeshRenderer의 MaterialPropertyBlock을 활용하여 색이 변하는 히트 이펙트를 적용하는 코루틴.
    /// 주석처리가 된 부분은, MeshRenderer의 MaterialPropertyBlock이 아닌, 
    /// Spine의 SkeletonAnimation의 Skeleton의 Color를 활용하여 색이 변하는 히트 이펙트 방식임.
    /// </summary>
    /// <param name="targetObj">히트 이펙트를 출력할 게임 오브젝트</param>
    /// <returns>코루틴 실행을 위한 enumerator.</returns>
    IEnumerator CoPlayHitEffect(GameObject targetObj)
    {
        MeshRenderer mr = targetObj.GetComponent<MeshRenderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        MaterialPropertyBlock mpb2 = new MaterialPropertyBlock();

        mpb.SetColor("_FillColor", new Color(1f, 1f, 1f, 1f));
        mpb.SetFloat("_FillPhase", 1f);

        mpb2.SetColor("_FillColor", new Color(1f, 1f, 1f, 1f));
        mpb2.SetFloat("_FillPhase", 0f);

        mr.SetPropertyBlock(mpb2);

        mr.SetPropertyBlock(mpb);
        yield return new WaitForSeconds(0.2f);

        mr.SetPropertyBlock(mpb2);

        //SkeletonAnimation targetSkel = targetObj.GetComponent<SkeletonAnimation>();
        //targetSkel.skeleton.SetColor(new Color(1f, 1f, 1f, 1f));

        //targetSkel.skeleton.SetColor(new Color32(142, 0, 28, 150));
        //yield return new WaitForSeconds(0.2f);

        //targetSkel.skeleton.SetColor(new Color(1f, 1f, 1f, 1f));
    }

    /// <summary>
    /// 유닛이 피격되었을 때 발생하는 효과와 상태 업데이트, 등을 처리하는 함수.
    /// 피해량 표시, 피격 효과, 체력 조정, 등이 포함되어 있음.
    /// </summary>
    /// <param name="attackerId">공격자 유닛의 ID 값.</param>
    /// <param name="damage">데미지 값.</param>
    /// <param name="isCritical">치명타라면 true, 아니라면 false</param>
    /// <param name="hitUnitID">피격자 유닛의 ID 값</param>
    /// <param name="newHp">피격자 유닛에 적용될 새로운 HP 값</param>
    /// <param name="newShield">피격자 유닛에 적용될 새로운 Shield 값</param>
    private void HandleUnitHitHPShield(int attackerId, float damage, bool isCritical, int hitUnitID, float newHp, float newShield, int objId)
    {
        if (!_unitGameObjs.TryGetValue(hitUnitID, out var targetObj) || !targetObj) return;

        // Hit effect Instantiate at target position. but it need to modify for each character's hit effect.
        //EffectManager._instance.PlayNormalHitEffectPrefab(new Vector3(targetObj.transform.position.x, targetObj.transform.position.y + 1.5f, targetObj.transform.position.z));

        // write down hit effect at this line.
        if (isCritical)
            targetObj.GetComponent<CharacterDamageTextUI>().ShowDamage(damage, DamageTextType.Critical);
        else
        {
            targetObj.GetComponent<CharacterDamageTextUI>().ShowDamage(damage, DamageTextType.Normal);
        }

        // Hit effect character's color change. but it need to modify for each character's hit effect.
        //StartCoroutine(CoPlayHitEffect(targetObj));

        if (!_unitGameObjs.TryGetValue(attackerId, out var attackerObj) || !attackerObj) return;
        else
        {
            SkeletonAnimation skel = attackerObj.GetComponent<SkeletonAnimation>();
            if (skel != null && skel.AnimationState.GetCurrent(0).Animation.Name == "Skill")
            {
                string name = attackerObj.GetComponent<SkeletonAnimation>().SkeletonDataAsset.name;
                bool reverse = targetObj.transform.position.x >= 0 ? false : true;
                switch (name)
                {
                    case "12001_SkeletonData":
                        EffectManager._instance.HitSkillEffectOnPos(EffectManager.HitEffectList.Therion, new Vector3(targetObj.transform.position.x, targetObj.transform.position.y + 3.5f, targetObj.transform.position.z - 1f), 3f, reverse);
                        break;

                    case "31001_SkeletonData":
                        EffectManager._instance.HitSkillEffectOnPos(EffectManager.HitEffectList.Hranger, new Vector3(targetObj.transform.position.x, targetObj.transform.position.y + 1.5f, targetObj.transform.position.z - 1f), 3f, reverse);
                        break;

                    case "32001_SkeletonData":
                        //EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Walter, new Vector3(attackerObj.transform.position.x, -3.95f, -0.04f), 2f, reverse, EffectManager.EndMode.NonEmission);
                        EffectManager._instance.HitSkillEffectOnPos(EffectManager.HitEffectList.Walter, new Vector3(targetObj.transform.position.x, targetObj.transform.position.y + 1.5f, targetObj.transform.position.z - 1f), 3f, reverse);
                        break;

                    case "41001_SkeletonData":
                        if (!reverse)
                            EffectManager._instance.HitSkillEffectOnPos(EffectManager.HitEffectList.Hmagician, new Vector3(targetObj.transform.position.x - 3.0f, targetObj.transform.position.y + .5f, 4f), 3f, reverse);
                        else
                            EffectManager._instance.HitSkillEffectOnPos(EffectManager.HitEffectList.Hmagician, new Vector3(targetObj.transform.position.x + 3.0f, targetObj.transform.position.y + .5f, 4f), 3f, reverse);
                        break;
                    default:
                        break;
                }
            }
        }

        attackerObj.GetComponent<UnitAttackRangeDebugger>().SetRange(targetObj.transform.position);
        var hitCharacter = targetObj.GetComponent<Character>();
        float getNewHp = hitCharacter.nowHP;
        getNewHp = getNewHp - damage;
        if (hitCharacter) hitCharacter.SetHpMp(Mathf.FloorToInt(getNewHp), (int)hitCharacter.nowHP);
    }

    /// <summary>
    /// 지정된 유닛의 체력을 설정된 HP 값으로 회복시키는 함수.
    /// 여기에서 힐 이펙트 또한 출력하며, 힐양에 대한 메세지를 표시하기도 함.
    /// </summary>
    /// <param name="unitID">힐이 적용될 유닛 ID.</param>
    /// <param name="setHp">유닛에 적용될 새로운 HP 값.</param>
    /// <param name="casterID">힐을 시전한 유닛의 ID.</param>
    private void HandleUnitHealHP(int unitID, float setHp, int casterID)
    {
        if(!_unitGameObjs.TryGetValue(unitID, out var gameObj)) return;

        var character = gameObj.GetComponent<Character>();
        EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Heal, new Vector3(gameObj.transform.position.x, gameObj.transform.position.y, -17f), 2f, false, EffectManager.EndMode.Emission);
        //Debug.LogWarning("Instantiate Heal");
        if (character) character.SetHpMp((int)setHp, (int)character.nowHP);
        character.GetComponent<CharacterDamageTextUI>().ShowDamage((int)(setHp - character.nowHP), DamageTextType.Heal);

        AudioManager._instance.HealSFXPlay();
    }
    
    /// <summary>
    /// 고정 소숫점 좌표를 받아 유닛의 위치와 _oneTickSnapShotBuffer를 업데이트하여, 해당 유닛의 이동을 처리하는 함수.
    /// </summary>
    /// <param name="unitID">이동 시킬 유닛의 ID 값</param>
    /// <param name="xRaw">유닛의 새로운 위치에 대한 고정 소수점 X 좌표.</param>
    /// <param name="yRaw">유닛의 새로운 위치에 대한 고정 소수점 Y 좌표.</param>
    private void HandleUnitMove(int unitID, int xRaw, int yRaw)
    {
        float x = Fixed16_16ToFloat(xRaw);
        float y = Fixed16_16ToFloat(yRaw);

        // Use OneTickSnapShotBuffer
        if (!_unitGameObjs.ContainsKey(unitID))
        {
            NewUnitBinder(unitID, x, y);
            //UnitBinder(unitID, x, y);
        }
        if (!_unitGameObjs.TryGetValue(unitID, out var unitGameObj)) return;

        var tr = unitGameObj.transform;

        //var before = tr.position;
        var after = (ground == Ground.XY) ?
            new Vector3(x, y, 0f) : new Vector3(x, groundY, y);

        var otssb = _oneTickSnapShotBuffer.TryGetValue(unitID, out var temp) ? temp : default;

        var start = otssb.hasMove ? otssb.after : tr.position;

        otssb.hasMove = true;
        otssb.before = start;
        otssb.after = after;

        _oneTickSnapShotBuffer[unitID] = otssb;

        //SetWorldPos(unitGameObj.transform, x, y);
        // Before
        //var tr = unitGameObj.transform;

        //var before = tr.position;
        //var after = (ground == Ground.XY) ? 
        //    new Vector3(x, y, 0f) : new Vector3(x, groundY, y);

        //// save move data line
        //var otssb = _oneTickSnapShotBuffer.TryGetValue(unitID, out var temp) ? temp : default;
        //otssb.hasMove = true;
        //otssb.before = before;
        //otssb.after = after;

        //otssb.startTime = Time.unscaledTime; // frame start time
        //otssb.duration = FIXED_INTERPOLARTION_TICK_DURATION; // Fixed interpolation time
        //_oneTickSnapShotBuffer[unitID] = otssb;
    }

    /// <summary>
    /// 유닛의 State(Act)가 변경될 때, State를 _oneTickSnapShotBuffer에 저장하고, 해당 State에 맞는 사운드 이펙트를 재생하여, 상태 변경을 처리하는 함수.
    /// 유닛이 존재하지 않는 경우에는, 해당 행동을 _pendingAct 딕셔너리에 저장하여, 나중에 유닛이 생성되었을 때 행동이 적용될 수 있도록 함.
    /// </summary>
    /// <param name="unitID">캐릭터의 고유 ID 값</param>
    /// <param name="act">캐릭터의 행동(State) 값 (UnitAct enum의 값으로 넣어야 함)</param>
    /// <param name="dir">유닛이 바라보고 있는 방향</param>
    private void HandleActChange(int unitID, UnitAct act, int dir)
    {
        if (!_unitGameObjs.ContainsKey(unitID))
        {
            if(!_pendingAct.TryGetValue(unitID, out var list))
            {
                _pendingAct[unitID] = list = new List<(int actType, int dir)>(4);
            }
            list.Add(((int)act, dir));
            return;
        }

        if (act != UnitAct.Move && _oneTickSnapShotBuffer.TryGetValue(unitID, out var moveBuf))
        {
            if (moveBuf.hasMove && _unitGameObjs.TryGetValue(unitID, out var obj) && obj)
            {
                obj.transform.position = moveBuf.after;
                moveBuf.hasMove = false;
                _oneTickSnapShotBuffer[unitID] = moveBuf;
            }
        }

        // save State data line
        var otssb = _oneTickSnapShotBuffer.TryGetValue(unitID, out var temp) ? temp : default;
        otssb.hasChangeState = true; 
        otssb.dir = dir;
        otssb.afterState = act;
        _oneTickSnapShotBuffer[unitID] = otssb;
        //Debug.Log($"[Debug STATE] id={unitID} act={act} dir={dir} time={Time.frameCount}");

        _unitGameObjs.TryGetValue(unitID, out GameObject targetSoundObj);

        bool reverse = dir == 1 ? false : true;

        if (act == UnitAct.Attack || act == UnitAct.Attack2 || act == UnitAct.Skill)
            AudioManager.PlayCharacterSound(ResolveSoundSoundStateTypeInList(act), ResolveSoundCharacerInList(targetSoundObj, reverse, act));

        TriggerSkillCastVFX(unitID, act);

        if (!_unitGameObjs.TryGetValue(unitID, out var unitGameObj)) return;
        else
        {
            if(act == UnitAct.Die)
            {
                unitGameObj.GetComponentInChildren<CharacterMPHPFade>().FadeOut(0.5f);
            }
        }
    }

    void UnitBinder(int unitId, float worldX, float worldY)
    {
        var can_did_dates = (worldX < 0f) ? _pendingLeft : _pendingRight;
        var chosen = FindClosest(can_did_dates, worldX, worldY);

        if (!chosen)
        {
            var leftside = FindClosest(_pendingLeft, worldX, worldY);
            var rightside = FindClosest(_pendingRight, worldX, worldY);
            chosen = ChooseCloser(leftside, rightside, worldX, worldY);
        }

        if (!chosen) return;

        _unitGameObjs[unitId] = chosen;

        if(_pendingAct.TryGetValue(unitId, out var act))
        {
            foreach(var (actType, dir) in act)
            {
                HandleActChange(unitId, (UnitAct)actType, dir);
            }
            _pendingAct.Remove(unitId);
        }

        var metaTag = chosen.GetComponent<UnitMetaTag>();
        if(metaTag != null) _unitBaseStatByUnitId[unitId] = metaTag.baseStatId;

        _pendingLeft.Remove(chosen);
        _pendingRight.Remove(chosen); 
    }
    /// <summary>
    /// 유닛이 처음으로 등장할 때, 해당 유닛의 위치를 기반으로 왼쪽 혹은 오른쪽 슬롯에서 유닛 프리펩을 선택하여 바인딩하는 함수.
    /// 해당 유닛이 존재하지 않는 경우에만 바인딩이 이루어지며, 이미 존재하는 유닛의 경우에는 바인딩이 수행되지 않음.
    /// 대기 중인 행동이 있다면, 그것도 함께 적용하여 유닛의 초기 상태를 설정함.
    /// </summary>
    /// <param name="unitId">바인딩 할 유닛의 고유 ID.</param>
    /// <param name="worldx">월드 공간에서 유닛의 X 좌표.</param>
    /// <param name="worldy">월드 공간에서 유닛의 Y 좌표.</param>
    void NewUnitBinder(int unitId, float worldx, float worldy)
    {
        bool isLeft = worldx < 0f;

        GameObject chosen = null;
        if (isLeft)
        {
            if (_nextLeftBind >= 3) return;
            chosen = _leftSlotObjs[_nextLeftBind++];
        }
        else
        {
            if (_nextRightBind >= 3) return;
            chosen = _rightSlotObjs[_nextRightBind++];
        }

        if (!chosen) return;

        _unitGameObjs[unitId] = chosen;

        var metaTag = chosen.GetComponent<UnitMetaTag>();
        if (metaTag != null) _unitBaseStatByUnitId[unitId] = metaTag.baseStatId;

        if (_pendingAct.TryGetValue(unitId, out var act))
        {
            foreach (var (actType, dir) in act)
                HandleActChange(unitId, (UnitAct)actType, dir);
            _pendingAct.Remove(unitId);
        }
    }

    /// <summary>
    /// 현재 게임이 진행되고 있는 지형이 XY 평면인지, XZ 평면인지에 따라, Transform의 y값을 반환할지 z값을 반환할지 결정하는 함수.
    /// </summary>
    /// <param name="tr">계산하기 위한 위치 값</param>
    /// <returns>지면이 XY인 경우, y, 그게 아니라면 z 좌표 </returns>
    float GetYorZ(Transform tr) => (ground == Ground.XY) ? tr.position.y : tr.position.z;

    /// <summary>
    /// 목록에 있는 GameObject 중에서, 지정된 x, y 좌표와 가장 가까운 GameObject를 찾아 반환하는 함수.
    /// </summary>
    /// <param name="list">서칭할 GameObject List</param>
    /// <param name="x">비교 대상인 x좌표.</param>
    /// <param name="y">비교 대상인 y좌표.</param>
    /// <returns>지정된 좌표에서 가장 가까운 GameObject를 리턴, 또는 없는 경우 null을 리턴.</returns>
    GameObject FindClosest(List<GameObject> list, float x, float y)
    {
        GameObject best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < list.Count; ++i) 
        {
            var pos = list[i].transform;

            float yy = GetYorZ(pos);
            float dx = pos.position.x - x;
            float dy = yy - y;
            float dist = dx * dx + dy * dy;

            if(dist < bestDistance)
            {
                best = list[i];
                bestDistance = dist;
            }

            //float distance = (pos.x - x) * (pos.x - x) + (pos.z - y) * (pos.z - y);
            //if(distance < bestDistance)
            //{
            //    best = list[i];
            //    bestDistance = distance;
            //}
        }
        return best;
    }
    /// <summary>
    /// 제곱된 거리 기준으로 GameObject obj1과 obj2 중에서, 지정된 x, y 좌표에 더 가까운 GameObject를 반환하는 함수.
    /// </summary>
    /// <param name="obj1">비교할 첫 번째 게임 오브젝트</param>
    /// <param name="obj2">비교할 두 번째 게임 오브젝트</param>
    /// <param name="x">기준 위치의 x좌표.</param>
    /// <param name="y">기준 위치의 y좌표.</param>
    /// <returns>지정된 위치에서 더 가까운 GameObject를 리턴하거나, 둘 중 하나가 null인 경우, null을 리턴</returns>
    GameObject ChooseCloser(GameObject obj1, GameObject obj2, float x, float y)
    {
        if (!obj1) return obj2;
        if(!obj2) return obj1;   
        
        float y1 = GetYorZ(obj1.transform);
        float y2 = GetYorZ(obj2.transform);

        float dx1 = obj1.transform.position.x - x;
        float dx2 = obj2.transform.position.x - x;

        float dy1 = y1 - y;
        float dy2 = y2 - y;

        float d1 = dx1 * dx1 + dy1 * dy1;
        float d2 = dx2 * dx2 + dy2 * dy2;

        return (d1 <= d2) ? obj1 : obj2;

        //var pos_obj1 = obj1.transform.position;
        //var pos_obj2 = obj2.transform.position;

        //float sqr_Dis_Obj1 = (pos_obj1.x - x) * (pos_obj1.x - x) + (pos_obj1.z - y) * (pos_obj1.z - y);
        //float sqr_Dis_Obj2 = (pos_obj2.x - x) * (pos_obj2.x - x) + (pos_obj2.z - y) * (pos_obj2.z - y);
        //// sqrt 안 쓰고 좀 더 빠르게

        //if(sqr_Dis_Obj2 <= sqr_Dis_Obj1) return obj1; // obj 1 우선
        //return obj2;
    }

    /// <summary>
    /// for basic Battle OBJ Spawn
    /// </summary>
    /// <returns>defalutProjectilePrefab (basic Obj)</returns>
    GameObject ResolveProjectilePrefab_Fallback()
    {
        return defaultProjectilePrefab != null ? defaultProjectilePrefab : (projecttilePrefabs != null && projecttilePrefabs.Length > 0 ? projecttilePrefabs[0] : null);
    }
    // before


    /// <summary>
    /// 지정된 공격 타입에 해당하는 투사체 프리펩을 반환하는 함수
    /// </summary>
    /// <param name="type">해당 투사체 프리팹을 선택하는데 사용되는 BattleAttackObj enum의 파라메터</param>
    /// <returns>있거나 사용이 가능한 경우 지정된 투사체 프리펩을 리턴하며, 그렇지 않은 경우 기본 투사체 프리펩을 리턴함</returns>
    GameObject ResolveProjectilePrefab(BattleAttackObj type)
    {
        int index = (int)type;
        if (projecttilePrefabs != null && index >= 0 && index < projecttilePrefabs.Length && projecttilePrefabs[index] != null) 
        {
            return projecttilePrefabs[index];
        } 
        return defaultProjectilePrefab;
    }

}