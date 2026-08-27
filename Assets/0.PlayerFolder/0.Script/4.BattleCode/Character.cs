using Core.Units;
using Newtonsoft.Json.Linq;
using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

[System.Serializable]
public struct CharacterStat
{
    public float maxHP;//최대 체력
    public int maxMP;//최대 마나
    public int attack;//공격력
    public int defense;//방여력
    public int attackSpeed;//선공권
    public int attackRange;//일반 공격 범위
    public int skillRange;//스킬 공격 범위
    public int plusMP;//마나 회복량
    public int critical;//치명타 확률
}
public class Character : MonoBehaviour
{
    [HideInInspector]
    public bool smoothMove = true;
    [HideInInspector]
    public GameSceneManager gameManager;
    public SkeletonAnimation _testSkeleton;

    [HideInInspector]
    public StateMachine stateMachine;
    public Image HPSlider;
    public Image MPSlider;
    public Canvas canva;
    public float nowHP;
    public float nowMP;
    public int direction = 1; // left = -1 | none = 0 | right = 1
    [HideInInspector]
    public float tickDuration = 0.033f; // 30Hz 기준 ( 1틱 == 33ms)
    // 1틱이 몇 초인가에 대한 변수
    [HideInInspector]
    public float tick = 0; // 현재 캐릭터가 몇 번째 틱인가? 
    public object data; // Character에 대한 임시 객체
    [HideInInspector]
    public bool moveEvnet = false;
    public CharacterStat stat = new CharacterStat();
    public CharList charid;

    Vector3 _last;

    void Awake()
    {
        stateMachine = new StateMachine(this);
        canva = GetComponentInChildren<Canvas>();
    }
    void Start()
    {
        ChangeState("Batch"); // 초기 상태
        _testSkeleton = GetComponent<SkeletonAnimation>();
    }
    void Update()
    {
        stateMachine.Update();
    }
    private void LateUpdate()
    {
        //if (transform.localPosition != _last)
        //    Debug.LogWarning($"[POS CHANGED] {name} localPos={transform.localPosition} frame={Time.frameCount}");
        _last = transform.localPosition;
    }

    public void ApplyState(UnitAct act, bool debuging = false)
    {
        if (debuging) { ChangeState("Stand"); return; }
        switch (act)
        {
            case UnitAct.Idle:          ChangeState("Idle");   break;
            case UnitAct.Move:          ChangeState("Move");   break;
            case UnitAct.Attack:        ChangeState("Attack"); break; // Meele
            case UnitAct.Attack2:       ChangeState("Attack2"); break; // Non-Meele ex) Lv2 Magican
            case UnitAct.Die:           ChangeState("Death");  break;
            // at Core version 0.4.4 doesnt use
            //case UnitAct.Stun:          ChangeState("Stun");   break;
            //case UnitAct.Hit:           ChangeState("Hit");    break;
            case UnitAct.Skill:         ChangeState("Skill");  break;
            
            
            default: Debug.Log("[State Set Error] : Character.ApplyState"); break;
        }
    }
    public void TestState(SoundStateType stat)
    {
        switch (stat)
        {
            case SoundStateType.IDLE:
                _testSkeleton.AnimationState.SetAnimation(0, "Idle", true); 
                break;

            case SoundStateType.STAND:
                _testSkeleton.AnimationState.SetAnimation(0, "Stand", true);
                break;
            case SoundStateType.WALK:
                _testSkeleton.AnimationState.SetAnimation(0, "Walk", true);
                break;
            case SoundStateType.ATTACK:
                _testSkeleton.AnimationState.SetAnimation(0, "Attack", true);
                break;
            case SoundStateType.SKILL: _testSkeleton.AnimationState.SetAnimation(0, "Skill", true); break;
            case SoundStateType.STAND_HAND: _testSkeleton.AnimationState.SetAnimation(0, "Stand_Hand", true); break;
            case SoundStateType.STAND_STOP: _testSkeleton.AnimationState.SetAnimation(0, "Stand_Stop", true); break;
            case SoundStateType.FREEZE: _testSkeleton.AnimationState.SetAnimation(0, "CC_Freeze", true); break;
            case SoundStateType.STUN: _testSkeleton.AnimationState.SetAnimation(0, "CC_Stun", true); break;
            case SoundStateType.DEATH: _testSkeleton.AnimationState.SetAnimation(0, "Death", true); break;


            default: Debug.Log("[State Set Error] : Character.ApplyState"); break;
        }
    }
    /// <summary>
    /// State 변경 함수 (Batch: 배치(초기 상태), Idle: 아이들 상태(코드 내 엔터와 익싯 존재), Move: 이동 상태, Attack: 공격상태
    /// </summary>
    /// <param name="state">변환할 스테이트를 스트링 파라메터로 받고, 이를 스위치 케이스 문으로 교환</param>
    public void ChangeState(string state) 
    {
        switch (state)
        {
            case "Batch":
                stateMachine.ChangeState(new BatchState());
                break;
            case "Idle":
                stateMachine.ChangeState(new IdleState());
                break;
            case "Attack":
                stateMachine.ChangeState(new AttackState());
                break;
            case "Attack2":
                stateMachine.ChangeState(new Attack2State());
                break;
            case "Move":
                stateMachine.ChangeState(new MoveState());
                break;
            case "Dying":
                stateMachine.ChangeState(new MoveState());
                break;
            case "Death":
                stateMachine.ChangeState(new DeathState());
                break;
            case "Test":
                stateMachine.ChangeState(new CharacterGhostTail());
                break;
            case "Stun":
                stateMachine.ChangeState(new StunState());
                break;
            case "Hit":
                stateMachine.ChangeState(new HitState());
                break;
            case "Skill":
                stateMachine.ChangeState(new SkillState());
                break;
            case "Stand":
                stateMachine.ChangeState(new StandState());
                break;
        }
      
        stateMachine.Update();
    }
    // UnitState -> 0: Stop, 1: Move, 2: Attack, 10: Dying, 11: Death
    public void ChangeStateInt(int state)
    {
        switch (state)
        {
            case 0:
                ChangeState("Idle");
                break;
            case 1:
                ChangeState("Move");
                break;
            case 2:
                ChangeState("Attack");
                break;
            case 10:
                ChangeState("Dying");
                break;
            case 11:
                ChangeState("Death");
                break;
            default:
                ChangeState("Idle");
                break;
        }
    }

    public void SetTestValue(int hp, int mp)
    {
        stat.maxHP = hp;
        stat.maxMP = mp;
        
        nowHP = hp;
        nowMP = mp;
    }
    public void SetDir(int dir)
    {
        dir = Mathf.Clamp(dir, -1, 1);
        if (dir == 0 || dir == direction) return;
        direction = dir;

        var s = transform.localScale;
        float abs = Mathf.Abs(s.x);
        transform.localScale = new Vector3(dir < 0 ? -abs : abs, s.y, s.z);
        if (dir == -1) RotateCanvasMinusDir();
        else RotateCanvasPlusDir();
        //dir = Mathf.Clamp(dir, -1, 1);
        //if (dir == 0 || dir == direction)
        //{
        //    return;
        //}
        //direction = dir;

        //var s = transform.localScale;
        //float abs = Mathf.Abs(s.x);
        //transform.localScale = new Vector3(dir < 0 ? -abs : abs, s.y, s.x);
    }
    /// <summary>
    /// 슬라이더 함수
    /// </summary>
    /// <param name="hp">현재 피</param>
    /// <param name="mp">현재 마나</param>
    /// <param name="maxHp">최대 피</param>
    /// <param name="maxMp">최대 마나</param>
    public void SetHpMp(int hp, int mp, int? maxHp = null, int? maxMp = null)
    {
        if (maxHp.HasValue) stat.maxHP = Mathf.Max(1, maxHp.Value);
        if (maxMp.HasValue) stat.maxMP = Mathf.Max(1, maxMp.Value);

        nowHP = Mathf.Clamp(hp, 0, (int)Mathf.Max(1, stat.maxHP));
        nowMP = Mathf.Clamp(mp, 0, Mathf.Max(1, stat.maxMP));

        if (HPSlider != null)
        {
            UpdateHPSlider();
        }
        if (MPSlider != null)
        {
            UpdateMPSlider();
        }
    }

    public void SetLocalPosition(Vector3 localPos)
    {
        transform.localPosition = localPos;
    }

    public void RotateCanvasMinusDir()
    {
        //canva.transform.rotation = Quaternion.Euler(0, 180f, 0);
        HPSlider.fillOrigin = 1;
        MPSlider.fillOrigin = 1;
    }
   
    public void RotateCanvasPlusDir()
    {
        //canva.transform.rotation = Quaternion.Euler(0, 0, 0);
        HPSlider.fillOrigin = 0;
        MPSlider.fillOrigin = 0;
    }
    public void UpdateHPSlider()
    {
        HPSlider.fillAmount = nowHP / stat.maxHP;
    }

    public void UpdateMPSlider()
    {
        MPSlider.fillAmount = nowMP / stat.maxMP;
    }
    
}
