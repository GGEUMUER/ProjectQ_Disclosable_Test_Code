using Core;
using Core.BattleObject;
using Core.Client;
using Core.FxMath;
using Core.Generators.Sink;
using Core.Payloads;
using Core.Payloads.Requests;
using Core.Sink;
using Core.Sink.Events;
using Core.Units;
using Newtonsoft.Json.Bson;
using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Unity.Collections.AllocatorManager;

public enum BattleAttackObj
{
    HArcherAttack = 0,
    HMagicionAttack,
    HNunAttack,
    WalterAttack,
    AndreaAttack,
    EmiliaAttack,
    EmiliaAttack2,
}

public enum CharacterIndex
{
    Lv1Def = 0,
    Lv1Assert,
    Lv1Ranger,
    Lv1Magican,
    Lv1Sup,
    Lv2Def,
    Lv2Assert,
    Lv2Ranger,
    Lv2Magican,
    Lv2Sup,
}
public class CoreBattleTest : MonoBehaviour
{
    BattleAttackObj bao;

    [Header("For Debug: Left Character Set")]
    public CharacterIndex L0;
    public CharacterIndex L1;
    public CharacterIndex L2;

    [Header("For Debug: Right Character Set")]
    public CharacterIndex R0;
    public CharacterIndex R1;
    public CharacterIndex R2;

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
    struct OneTickSnapShotBuffer
    {
        public bool hasMove;
        public Vector3 before, after;
        public float startTime, duration;

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
    bool _canDecode = false;

    void SetWorldPos(Transform t, float x, float y)
    {
        if (ground == Ground.XY) t.position = new Vector3(x, y, -1f);
        else t.position = new Vector3(x, groundY, y);
    }

    Vector2 GetWorldVec2(Transform t)
    {
        if (ground == Ground.XY) return new Vector2(t.position.x, t.position.y);
        else return new Vector2(t.position.x, t.position.z);
    }
    
    private void Start()
    {
        _pipe = new TickBlockPipeline();

        _prefabByStatID.Clear();
        foreach(var m in prefabMaps)
        {
            if(m.prefab != null) _prefabByStatID[m.baseStatID] = m.prefab;
        }

        byte b0 = (byte)(batchIndexIsOneBased ? 1 : 0);
        byte b1 = (byte)(batchIndexIsOneBased ? 2 : 1);
        byte b2 = (byte)(batchIndexIsOneBased ? 3 : 2);
        
        //var initLeft = new InitUnits(
        //    new SpawnUnit(leftBaseStatIds[0], b0),
        //    new SpawnUnit(leftBaseStatIds[1], b1),
        //    new SpawnUnit(leftBaseStatIds[2], b2)
        //);

        //var initRight = new InitUnits(
        //    new SpawnUnit(rightBaseStatIds[0], r0),
        //    new SpawnUnit(rightBaseStatIds[1], r1),
        //    new SpawnUnit(rightBaseStatIds[2], r2)
        //);

        //Span<SpawnUnit> inUnits = stackalloc SpawnUnit[6];
        inUnits[0] = new SpawnUnit(0, (int)L0, 1);
        inUnits[1] = new SpawnUnit(0, (int)L1, 2);
        inUnits[2] = new SpawnUnit(0, (int)L2, 3);

        inUnits[3] = new SpawnUnit(1, (int)R0, 10);
        inUnits[4] = new SpawnUnit(1, (int)R1, 11);
        inUnits[5] = new SpawnUnit(1, (int)R2, 12);

        Span<SpawnUnit> temp = inUnits;
        //Span<InitialUnitInfo> outUnits = stackalloc InitialUnitInfo[6];


        //bool test = _pipe.Init(seed, temp, outUnits);
        //Debug.Log(test);
        //_pipe.init(new BattleInit(seed, initLeft, initRight));

        for (int i = 0; i < 3; i++)
        {
            var leftPrefab = ResolveUnitPrefabForSlot(true, i, inUnits[i].BaseStatId);
            var charL = Instantiate(leftPrefab, leftRows[i], false);
            var metaTagL = charL.GetComponent<UnitMetaTag>() ?? charL.AddComponent<UnitMetaTag>();
            metaTagL.baseStatId = inUnits[i].BaseStatId;
            charL.name = $"L_{i}";
            charL.GetComponent<SnapPosition>().enabled = false;
            //charL.GetComponent<Character>().SetTestValue(1000, 100);
            _pendingLeft.Add(charL);

            var rightPrefab = ResolveUnitPrefabForSlot(false, i, inUnits[i + 3].BaseStatId);
            var charR = Instantiate(rightPrefab, rightRows[2 - i], false);
            var metaTagR = charR.GetComponent<UnitMetaTag>() ?? charR.AddComponent<UnitMetaTag>();
            metaTagR.baseStatId = inUnits[i + 3].BaseStatId;
            charR.name = $"R_{i}";
            charR.GetComponent<SnapPosition>().enabled=false;
            //charR.GetComponent<Character>().SetTestValue(1000, 100);
            _pendingRight.Add(charR);

            _leftSlotObjs[i] = charL;
            _rightSlotObjs[i] = charR;
            // 일단 먼저 L, R 유니티 하이어러키 상에서 지정한 유닛 프리펩으로 좌우 생성
            // 바인딩은 아래 함수에서 진행
        }

        bool test = _pipe.Init(seed, temp, outUnits);
        Debug.Log(test);
        Debug.Log($"RowL0 world={leftRows[0].position} RowL1 world={leftRows[1].position} RowL2 world={leftRows[2].position}");
        Debug.Log($"Spawned L0 world={_pendingLeft[0].transform.position} parent={_pendingLeft[0].transform.parent.name}");

        _canDecode = !_useIntroSequenceFlag;
        
        if(_useIntroSequenceFlag)
        {
            StartCoroutine(CoruIntro());
        }
    }

    #region Intro
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

    private void ApplyIdleToAllSpawned()
    {
        for (int i = 0; i < _pendingLeft.Count; i++)
        {
            Debug.Log($"{_pendingLeft.Count} Idle");
            ApplyIdle(_pendingLeft[i]);
        }
        for (int i = 0; i < _pendingRight.Count; i++)
        {
            Debug.Log($"{_pendingRight.Count} Idle");
            ApplyIdle(_pendingRight[i]);
        }

        foreach(var kv in _unitGameObjs)
        {
            ApplyIdle(kv.Value);
        }
    }

    private void ApplyIdle(GameObject gameObject)
    {
        if (!gameObject) return;
        var ch = gameObject.GetComponent<Character>();
        if(!ch) return;

        ch.ApplyState(UnitAct.Idle);
    }

    private void ApplyStanToAllSpawned()
    {

        for (int i = 0; i < _pendingLeft.Count; i++)
        {
            Debug.Log($"{_pendingLeft.Count} {i} Left Stand");
            _pendingLeft[i].GetComponentInChildren<AlphaChanger>().ToZeroAlphaSR(0.6f);
            ApplyStand(_pendingLeft[i]);
        }
        for (int i = 0; i < _pendingRight.Count; i++)
        {
            Debug.Log($"{_pendingRight.Count} {i} Right Stand");
            _pendingRight[i].GetComponentInChildren<AlphaChanger>().ToZeroAlphaSR(0.6f);
            _pendingRight[i].GetComponent<Character>().SetDir(-1);
            ApplyStand(_pendingRight[i]);
        }

        foreach (var kv in _unitGameObjs)
        {
            ApplyStand(kv.Value);
        }
    }
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
        if (_isFinishedBlock) return;
        if(!_canDecode) return;

        _curBlockDurSec += Time.deltaTime * TICKS_PER_SEC;

        while (_curBlockDurSec >= 1f)
        {
            if(!DecodeOneTick()) break;
            _curBlockDurSec -= 1f;
        }
    }

    /// <summary>
    /// Must Use LateUpdate.
    /// The most stable timing for final position updates and other movements is LateUpdate. 
    /// To account for potential bugs, the state machine is implemented in LateUpdate.
    /// Store it in a buffer and retrieve it later.
    /// </summary>
    private void LateUpdate()
    {
        if (_oneTickSnapShotBuffer.Count == 0) return;

        _tempMoveKeys.Clear();
        _tempMoveKeys.AddRange(_oneTickSnapShotBuffer.Keys);
        float now = Time.time;

        // Debug
        battleObjDebugView.Clear();
        foreach (var kv in _battleObjs)
            battleObjDebugView.Add(new BattleObjPair { key = kv.Key, value = kv.Value });

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
                // Point interpolation by time
                float t = (otssb.duration <= 0f) ?
                    1f : Mathf.Clamp01((now - otssb.startTime) / otssb.duration);
                var point = Vector3.Lerp(otssb.before, otssb.after, t);
                if (ground == Ground.XY) point.z = 0f;
                else point.y = groundY;

                tr.position = point;

                // is it end?
                if(t >= 1f) otssb.hasMove = false;
                Debug.Log($"[Debug APPLY] id={id} before={otssb.before} after={otssb.after} point={tr.position} ground={ground}");
            }

            if (!otssb.hasChangeState && !otssb.hasMove)
            {
                _oneTickSnapShotBuffer.Remove(id);
            }
            else _oneTickSnapShotBuffer[id] = otssb;
        }        

    }

    /// <summary>
    /// Decode Tick
    /// </summary>
    /// <returns> false = no remain tick, true remain tick </returns>
    bool DecodeOneTick()
    {
        // if finished block return
        if(_isFinishedBlock) return false;

        // if block come yet, get new block
        if(!_ishasBlock)
        {
            if (!_pipe.TryGetNext(out _curBlock))
            {
                _isFinishedBlock = true;
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
                        Debug.Log($"[StatusEffectEnd] || Type = {type} UnitId = {e.UnitId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");

                        break;
                    }
                case SinkEventType.StatusEffectChange:
                    {
                        var e = UnitMoveEvent.Decode(span, ref curOffset);
                        Debug.Log($"[StatusEffectChange] || Type = {type} UnitId = {e.UnitId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");
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
                        Debug.Log($"[HealEvent] || Type = {type} TargetId = {e.TargetId} Unit NewHp = {Fixer(e.NewHp248)} Unit CasterId = {e.CasterId}");
                        HandleUnitHealHP(e.TargetId, Fixer(e.NewHp248), e.CasterId);
                        break;
                    }
                case SinkEventType.ShieldIncrease:
                    {
                        var e = ShieldIncreaseEvent.Decode(span, ref curOffset);
                        Debug.Log($"[ShieldIncreaseEvent] || Type = {type} TargetId = {e.TargetId} Unit NewShield = {e.NewShield} Unit CasterId = {e.CasterId}");
                        break;
                    }
                // Done
                case SinkEventType.MpChange:
                    {
                        var e = MpChangeEvent.Decode(span, ref curOffset);
                        Debug.Log($"[MpChangeEvent] || Type = {type} UnitId = {e.UnitId} Unit NewMp = {e.NewMp}");
                        HandleUnitSetMP(e.UnitId, e.NewMp);
                        break;
                    }

                case SinkEventType.BattleObjectSpawn:
                    {
                        var e = BattleObjectSpawnEvent.Decode(span, ref curOffset);
                        Debug.Log($"[BattleObjectSpawnEvent] || Type = {type} BO Type = {e.Type} ObjectId = {e.ObjId} Unit OwnerId = {e.OwnerId} Unit HalfSizeXRaw = {Fixed16_16ToFloat(e.HalfSizeX1616)}" +
                            $"HalfSizeYRaw = {Fixed16_16ToFloat(e.HalfSizeY1616)} FacingXRaw = {Fixed16_16ToFloat(e.FacingX1616)} FacingYRaw = {Fixed16_16ToFloat(e.FacingY1616)} CenterXRaw = {Fixed16_16ToFloat(e.CenterX1616)} CenterYRaw = {Fixed16_16ToFloat(e.CenterY1616)}");
                        HandleBattleObjectSpawn(e.ObjId, e.OwnerId, e.Type, e.HalfSizeX1616, e.HalfSizeY1616, e.FacingX1616, e.FacingY1616, e.CenterX1616, e.CenterY1616);
                        break;
                    }
                // Done
                case SinkEventType.BattleObjectMove:
                    {
                        var e = BattleObjectMoveEvent.Decode(span, ref curOffset);
                        Debug.Log($"[BattleObjectMoveEvent] || Type = {type} ObjectId = {e.ObjId} Unit NewXRaw = {Fixed16_16ToFloat(e.NewX1616)} Unit NewYRaw = {Fixed16_16ToFloat(e.NewY1616)}");
                        HandleBOMove(e.ObjId, e.NewX1616, e.NewY1616);
                        break;
                    }
                // Done
                case SinkEventType.BattleObjectDespawn:
                    {
                        var e = BattleObjectDespawnEvent.Decode(span, ref curOffset);
                        Debug.Log($"[BattleObjectDespawnEvent] || Type = {type} ObjectId = {e.ObjId}");
                        HandleBODeSpawn(e.ObjId);
                        break;
                    }
                // Done
                case SinkEventType.DelayedBOSpawn:
                    {
                        var e = DelayedBOSpawnEvent.Decode(span, ref curOffset);
                        Debug.Log($"[DelayedBOSpawn] || Type = {type} ObjType = {e.ObjId} {e.Type} Unit Delay = {e.Delay} Unit XRaw = {Fixed16_16ToFloat(e.X1616)} Unit YRaw = {Fixed16_16ToFloat(e.Y1616)}");
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

    void TriggerSkillCastVFX(int casterId, UnitAct act)
    {
        if (act != UnitAct.Skill) return;

        _lastCastByUnitId[casterId] = (act, Time.time);
    }
    GameObject ResolveUnitPrefabForSlot(bool isLeft, int slotIdx, int baseStatID)
    {
        var overrides = isLeft ? leftSlotOverrides : rightSlotOverrides;
        if(overrides != null && slotIdx >= 0 &&
            slotIdx < overrides.Length &&
            overrides[slotIdx] != null)
            return overrides[slotIdx];
        return ResolveUnitPrefabBystatID(baseStatID);
    }

    GameObject ResolveUnitPrefabBystatID(int basedID)
    {
        if (_prefabByStatID.TryGetValue(basedID, out var unit) && unit) return unit;
        return defaultProjectilePrefab;
    }
    private void HandleDelayedBOSpawn(BattleObjectType boSE, int destroyXBOxRaw, int destroyBOyRaw, int delay)
    {
        float tickSec = 1f / 30f;
        switch (boSE)
        {
            case BattleObjectType.AndreaSkill:
                if(Fixer(destroyXBOxRaw) > 0)
                    EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Andrea, new Vector3(Fixed16_16ToFloat(destroyXBOxRaw), Fixed16_16ToFloat(destroyBOyRaw + Q16_16.ONE), -2f), delay * tickSec, false, EffectManager.EndMode.Emission);
                else
                    EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Andrea, new Vector3(Fixed16_16ToFloat(destroyXBOxRaw), Fixed16_16ToFloat(destroyBOyRaw + Q16_16.ONE), -2f), delay * tickSec, true, EffectManager.EndMode.Emission);
                Debug.Log("[HandleHit] Switch AndreaSkill In?");
                break;

                //case BattleObjectType.
            default:
                Debug.Log("[HandleHit] Switch Defalut In?");
                break;
        }

    }
    IEnumerator CoSpwanDelayed(BattleObjectType objType, float x, float y, float delay)
    {

        yield return new WaitForSeconds(delay);

        //var prefab = ResolveProjectilePrefab(objType);
        //var gameObj = prefab ? Instantiate(prefab) : new GameObject($"BattleObj_{objType}_Delay_{delay}");
        ////gameObj.transform.position = new Vector3(x, gameObj.transform.position.y, y);
        //SetWorldPos(gameObj.transform, x, y);
    }

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
    private void HandleBODeSpawn(int despawnObjIdx)
    {
        Debug.Log($"[BODebug][HandleBODespawn] ID Number = {despawnObjIdx}");

        if (_battleObjs.TryGetValue(despawnObjIdx, out var obj))
        {
            Debug.Log($"[BODebug][HandleBODespawn] Destroy check");
            Destroy(obj.gameObject);
        }
        _battleObjs.Remove(despawnObjIdx);
        _pendingBattleObjMoves.Remove(despawnObjIdx);
        _battleObjLastPos.Remove(despawnObjIdx);
    }

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
        Debug.Log($"[_BattleObjs Log] objectID name = {objectID} ownerID = {ownerID} gameobj Name = {gameobj.name}");

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

    private void HandleUnitSetMP(int unitID, int setMp)
    {
        if (!_unitGameObjs.TryGetValue(unitID, out var gameObj)) return;

        var character = gameObj.GetComponent<Character>();
        if (character) character.SetHpMp((int)character.nowHP, setMp);
    }

    private void HandleUnitHitHPShield(int attackerId, float damage, bool isCritical, int hitUnitID, float newHp, float newShield, int objId)
    {
        if (!_unitGameObjs.TryGetValue(hitUnitID, out var targetObj) || !targetObj) return;

        // write down hit effect at this line.

        if (!_unitGameObjs.TryGetValue(attackerId, out var attackerObj) || !attackerObj) return;
        else
        {
            SkeletonAnimation skel = attackerObj.GetComponent<SkeletonAnimation>();
            if(skel != null && skel.AnimationState.GetCurrent(0).Animation.Name == "Skill")
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

                    default:
                        break;
                }
            }
        }

        var hitCharacter = targetObj.GetComponent<Character>();
        float getNewHp = hitCharacter.nowHP;
        getNewHp = getNewHp - damage;
        if (hitCharacter) hitCharacter.SetHpMp(Mathf.FloorToInt(getNewHp), (int)hitCharacter.nowHP);
    }

    private void HandleUnitHealHP(int unitID, float setHp, int casterID)
    {
        if(!_unitGameObjs.TryGetValue(unitID, out var gameObj)) return;

        var character = gameObj.GetComponent<Character>();
        EffectManager._instance.PlaySkillOnPos2(EffectManager.SkillList.Heal, new Vector3(gameObj.transform.position.x, gameObj.transform.position.y, -17f), 2f, false, EffectManager.EndMode.Emission);
        Debug.LogWarning("Instantiate Heal");
        if (character) character.SetHpMp((int)setHp, (int)character.nowHP);
    }

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

        var before = tr.position;
        var after = (ground == Ground.XY) ? 
            new Vector3(x, y, 0f) : new Vector3(x, groundY, y);

        // save move data line
        var otssb = _oneTickSnapShotBuffer.TryGetValue(unitID, out var temp) ? temp : default;
        otssb.hasMove = true;
        otssb.before = before;
        otssb.after = after;

        otssb.startTime = Time.time; // frame start time
        otssb.duration = FIXED_INTERPOLARTION_TICK_DURATION; // Fixed interpolation time
        _oneTickSnapShotBuffer[unitID] = otssb;
    }

    /// <summary>
    /// State Changer
    /// </summary>
    /// <param name="unitdAct"> 유닛의 ID </param>
    /// <param name="act"> 바뀔 유닛의 스테이트 </param>
    /// <param name="dir"> 유닛이 바라보고 있는 방향 </param>
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
        
        // save State data line
        var otssb = _oneTickSnapShotBuffer.TryGetValue(unitID, out var temp) ? temp : default;
        otssb.hasChangeState = true; 
        otssb.dir = dir;
        otssb.afterState = act;
        _oneTickSnapShotBuffer[unitID] = otssb;
        Debug.Log($"[Debug STATE] id={unitID} act={act} dir={dir} time={Time.frameCount}");

        TriggerSkillCastVFX(unitID, act);
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

    float GetYorZ(Transform tr) => (ground == Ground.XY) ? tr.position.y : tr.position.z;

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