using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFXSpineManager : MonoBehaviour
{
    [SerializeField] SkeletonAnimation _skel;
    Character _character;
    public Slider slider;
    private void Awake()
    {
        if(!_skel) _skel = GetComponent<SkeletonAnimation>();
        _character = GetComponent<Character>();

        //_skel.AnimationState.Event += OnSpineEvent;
    }
    /*
    void OnSpineEvent(TrackEntry entry, Spine.Event e)
    {
        if (_character == null) return;

        switch (e.Data.Name) 
        {
            case "Idle":
                AudioManager.PlayCharacterSound(SoundStateType.IDLE, _character.charid, slider.value);
                break;
            case "Attack_sfx":
                AudioManager.PlayCharacterSound(SoundStateType.ATTACK, _character.charid, slider.value);
                break;
            case "CC_Stun":
                AudioManager.PlayCharacterSound(SoundStateType.STUN, _character.charid, slider.value);
                break;
            case "CC_Freeze":
                AudioManager.PlayCharacterSound(SoundStateType.FREEZE, _character.charid, slider.value);
                break;
            case "Death":
                AudioManager.PlayCharacterSound(SoundStateType.DEATH, _character.charid, slider.value);
                break;
            case "Skill_sfx":
                AudioManager.PlayCharacterSound(SoundStateType.SKILL, _character.charid, slider.value);
                break;
            case "Box_evt":
                AudioManager.PlayCharacterSound(SoundStateType.STAND, _character.charid, slider.value);
                break;
            case "Stand_Hand":
                AudioManager.PlayCharacterSound(SoundStateType.STAND_HAND, _character.charid, slider.value);
                break;
            case "Stand_Stop":
                AudioManager.PlayCharacterSound(SoundStateType.STAND_STOP, _character.charid, slider.value);
                break;
            case "Walk":
                AudioManager.PlayCharacterSound(SoundStateType.WALK, _character.charid, slider.value);
                break;

        }

    }
    */
    private void OnEnable()
    {
        //if (_skel != null) _skel.AnimationState.Event += OnSpineEvent;
    }
    private void OnDisable()
    {
        //if (_skel != null) _skel.AnimationState.Event -= OnSpineEvent;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
