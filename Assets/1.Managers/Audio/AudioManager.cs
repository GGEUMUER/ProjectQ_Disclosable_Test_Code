using Spine.Unity.Examples;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum SoundStateType
{
    IDLE,
    STAND,
    WALK,
    ATTACK,
    SKILL,
    STAND_HAND,
    STAND_STOP,
    FREEZE,
    STUN,
    DEATH
}
public enum CharList
{
    LV1_ASSAULTER,
    LV1_GUARDIAN,
    LV1_RANGER,
    LV1_SUPPORTER,
    LV1_WIZARD,
    LV2_ASSAULTER,
    LV2_GUARDIAN,
    LV2_RANGER,
    LV2_SUPPORTER,
    LV2_WIZARD
}

public enum UISFX
{
    BTNCLICK,
    FLIPVIEW,
    POPUP,
    EARNED
}

public enum BGMLIST
{
    LOBBY,
    BattleStart,
    Batch,
    FIGHT,
    BattleEnd,
    BOSS,
    WIN,
    LOSE
}
public enum SYSTEMSFX 
{
    ALERT,
    COUNTDOWN,
    ROUNDSTART,
    ROUNDEND,
}

public enum ENVIRONMENTALSFX
{
    WIND,
    RAIN,
    SNOW,
    CROWD,
    FOREST,
    CAVE
}

[System.Serializable]
public class SoundRaw
{
    public List<AudioClip> clips;
}

#if UNITY_EDITOR
[CustomEditor(typeof(AudioManager))]
public class AudioMangerEditor : Editor 
{
    //ReorderableList _charList;
    SerializedProperty _soundListProp;

    string[] _charNames;
    string[] _stateNames;
    bool[] _folds;

    Dictionary<string, bool> _groupFoldouts = new();
    private void OnEnable()
    {
        _soundListProp = serializedObject.FindProperty("_soundList");

        _charNames = Enum.GetNames(typeof(CharList));
        _stateNames = Enum.GetNames(typeof(SoundStateType));

        if (_soundListProp.arraySize != _charNames.Length)
            _soundListProp.arraySize = _charNames.Length;

        _folds = new bool[_charNames.Length];


        foreach(var name in _charNames)
        {
            string group = GetGroupName(name);
            if (!_groupFoldouts.ContainsKey(group))
                _groupFoldouts[group] = true;
        }
    }

    private string GetGroupName(string enumName)
    {
        int underScore = enumName.IndexOf('_');
        return underScore > 0 ? enumName.Substring(0, underScore) : "The Other";
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        serializedObject.Update();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All Non-Fold"))
        {
            for (int i = 0; i < _folds.Length; i++)
            {
                _folds[i] = true;
            }
            var keys = new List<string>(_groupFoldouts.Keys);
            foreach (var key in keys) 
            {
                _groupFoldouts[key] = true;
            }
        }
        if(GUILayout.Button("All Fold"))
        {
            for(int i =0; i<_folds.Length; i++)
            {
                _folds[i] = false;
            }
            var keys = new List<string>(_groupFoldouts.Keys);
            foreach (var key in keys)
            {
                _groupFoldouts[key] = true;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(0);

        foreach(string group in GetGropsInOrder())
        {
            _groupFoldouts[group] = EditorGUILayout.Foldout(_groupFoldouts[group], GroupDisplayName(group), true);
            if (!_groupFoldouts[group]) continue;

            EditorGUI.indentLevel++;

            for (int charIdx = 0; charIdx < _charNames.Length; charIdx++)
            {
                if (GetGroupName(_charNames[charIdx]) != group) continue;
                DrawCharacterRow(charIdx);
                EditorGUILayout.Space(4);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCharacterRow(int charIdx)
    {
        var rowProp = _soundListProp.GetArrayElementAtIndex(charIdx);
        var clipsProp = rowProp.FindPropertyRelative("clips");

        if(clipsProp.arraySize != _stateNames.Length)
            clipsProp.arraySize = _stateNames.Length;

        string charLabel = _charNames[charIdx];

        _folds[charIdx] = EditorGUILayout.Foldout(_folds[charIdx], charLabel, true);
        if (!_folds[charIdx]) return;

        EditorGUI.indentLevel++;

        for (int i = 0; i < _stateNames.Length; i++)
        {
            var clipElement = clipsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(clipElement, new GUIContent(_stateNames[i]));
        }
        EditorGUI.indentLevel--;
    }

    private string GroupDisplayName(string group)
    {
        return group switch
        {
            "LV1" => "Level 1s",
            "LV2" => "Level 2s",
            _ => group
        };
    }

    private IEnumerable<object> GetGropsInOrder()
    {
        if (_groupFoldouts.ContainsKey("LV1")) yield return "LV1";
        if (_groupFoldouts.ContainsKey("LV2")) yield return "LV2";
        
        foreach(var key in _groupFoldouts.Keys)
        {
            if(key != "LV1" && key != "LV2") yield return key;
        }
    }
}
#endif

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager _instance { get; private set; }

    [Header("Channels")]
    AudioSource _audioSourceBGM;
    AudioSource _audioSourceUISFX;
    AudioSource _audioSourceSysSFX;
    AudioSource _audioSourceEnvSFX;
    AudioSource _audioSourceCharSFX;

    [Header("BGM")]
    [SerializeField]
    AudioClip[] _audioClipsBGM;

    [Header("UI")]
    [SerializeField]
    AudioClip[] _uiAudioClip;

    [Header("SYSTEMSFX")]
    [SerializeField]
    AudioClip[] _audioClipSYSTEMSFX;

    [Header("ENVIRONMENTALSFX")]
    [SerializeField]
    AudioClip[] _audioClipENVIRONMENTALSFX;

    [Header("Character")]
    [SerializeField, HideInInspector] List<SoundRaw> _soundList;

    [Header("Heal")]
    public AudioClip healClip;

    [Header("Values")]
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;

    Coroutine _coLoop;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject); return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSourceBGM = CreateChannels("BGM", true);
        _audioSourceUISFX = CreateChannels("UISFX", false);
        _audioSourceSysSFX = CreateChannels("SYSTEM SFX", false);
        _audioSourceEnvSFX = CreateChannels("Env SFX", true);
        _audioSourceCharSFX = CreateChannels("Character_SFX", false);

        System.Array.Resize(ref _audioClipsBGM, Enum.GetValues(typeof(BGMLIST)).Length);
        System.Array.Resize(ref _uiAudioClip, Enum.GetValues(typeof(UISFX)).Length);
        System.Array.Resize(ref _audioClipSYSTEMSFX, Enum.GetValues(typeof(SYSTEMSFX)).Length);
        System.Array.Resize(ref _audioClipENVIRONMENTALSFX, Enum.GetValues(typeof(ENVIRONMENTALSFX)).Length);
    }

    AudioSource CreateChannels(string name, bool loop)
    {
        var gameObj = new GameObject(name);
        gameObj.transform.SetParent(transform, false);
        var audioSource = gameObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        return audioSource;
    }
    void Start()
    {

    }

    //for TestScene
    public void TestPlayCharacterSoundLoopDelay(SoundStateType sound, CharList charList, float delay, bool loop, float volume = 1)
    {
        if (loop)
        {
            _coLoop = StartCoroutine(IntervalSFX(_instance._soundList[(int)charList].clips[(int)sound], delay, volume));
        }
        else
            StopCoroutine(_coLoop);
    }

    public void TestPlayCharacterSound(SoundStateType sound, CharList charList, float delay, float volume = 1)
    {
        var clip = _instance._soundList[(int)charList].clips[(int)sound];
        if (clip == null) return;

        _instance._audioSourceCharSFX.loop = false;
        _instance._audioSourceCharSFX.volume = volume;
        _instance._audioSourceCharSFX.clip = clip;
        _instance._audioSourceCharSFX.Play();
    }
    public void PlayUISound(UISFX sound, float volume = 1)
    {
        _instance._audioSourceUISFX.PlayOneShot(_uiAudioClip[(int)sound], volume);
    }
    public void PlaySysSound(SYSTEMSFX sound, float volume = 1)
    {
        _instance._audioSourceSysSFX.PlayOneShot(_audioClipSYSTEMSFX[(int)sound], volume);
    }
    public void PlayBGMSound(BGMLIST id)
    {
        var clip = _audioClipsBGM[(int)id];
        if (clip == null) return;
        
        // 현재 재생 중인 BGM과 동일한 경우, 불필요한 재시작 방지
        if (_instance._audioSourceBGM.isPlaying && _instance._audioSourceBGM.clip == clip)
        {
            return;
        }

        _instance._audioSourceBGM.clip = clip;
        _instance._audioSourceBGM.volume = _instance.bgmVolume;
        _instance._audioSourceBGM.loop = true;
        _instance._audioSourceBGM.Play();
    }

    public void SetBGMVolume(float volume)
    {
        _instance.bgmVolume = volume;
        _instance._audioSourceBGM.volume = volume;
    } 

    public void StopBGM()
    {
        _instance._audioSourceBGM.Stop();
    }
    public void PlayEnvSound(ENVIRONMENTALSFX id, float volume = 1)
    {
        var clip = _audioClipENVIRONMENTALSFX[(int)id];
        if (clip == null) return;

        _instance._audioSourceEnvSFX.clip = clip;
        _instance._audioSourceEnvSFX.volume = volume;
        _instance._audioSourceEnvSFX.loop = true;
        _instance._audioSourceEnvSFX.Play();
    }

    public void StopEnv()
    {
        _instance._audioSourceEnvSFX.Stop();
    }
    //for manager
    public static void PlayCharacterSound(SoundStateType sound, CharList charList)
    {
        _instance._audioSourceCharSFX.PlayOneShot(_instance._soundList[(int)charList].clips[(int)sound], _instance.sfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        _instance.sfxVolume = volume;
        _instance._audioSourceCharSFX.volume = volume;
    }

    // for State Test scene
    public void SetBgmVolume(float volume)
    {
        _audioSourceBGM.volume = volume;
    }

    public void SetEnvVolume(float volume)
    {
        _audioSourceEnvSFX.volume = volume;
    }

    public void HealSFXPlay()
    {
        _audioSourceCharSFX.PlayOneShot(healClip, _instance.sfxVolume);
    }

    IEnumerator IntervalSFX(AudioClip clip, float interval, float volum)
    {
        yield return new WaitForSeconds(interval);

        while (true)
        {
            _audioSourceCharSFX.PlayOneShot(clip, volum);
            yield return new WaitForSeconds(interval);
        }
    }
}
