using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class InteractUIManager : MonoBehaviour
{
    public UISFX uiID;
    public BGMLIST bgmID;
    public SYSTEMSFX sysID;
    public ENVIRONMENTALSFX envID;
    public SoundStateType sstID;
    public CharList clID;

    [Header("For SFX Test Only")]
    public Transform DummyPos;
    SkeletonAnimation skeleton;
    public TMP_InputField index;
    int value;

    int nowPlayerPrefab = 0;
    public List<GameObject> playerPrefabs;

    [SerializeField] TMP_Dropdown bgmDropDown;
    [SerializeField] TMP_Dropdown envDropDown;
    [SerializeField] TMP_Dropdown uiDropDown;
    [SerializeField] TMP_Dropdown sysDropDown;
    [SerializeField] TMP_Dropdown characterDropDown;
    [SerializeField] TMP_Dropdown characterStateDropDown;

    [SerializeField] Slider uiVolumeSlider;
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Slider sysVolumeSlider;
    [SerializeField] Slider envVolumeeSlider;
    [SerializeField] Slider characterVolumeSlider;

    // Start is called before the first frame update
    void Start()
    {
        bgmDropDown.ClearOptions();
        envDropDown.ClearOptions();
        sysDropDown.ClearOptions();
        uiDropDown.ClearOptions();
        characterDropDown.ClearOptions();
        characterStateDropDown.ClearOptions();

        bgmDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(BGMLIST))
            ));
        envDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(ENVIRONMENTALSFX))
            ));
        sysDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(SYSTEMSFX))
            ));
        uiDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(UISFX))
            ));
        characterDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(CharList))
            ));
        characterStateDropDown.AddOptions(new System.Collections.Generic.List<string>(
            System.Enum.GetNames(typeof(SoundStateType))
            ));
    }
    public void OnClickCharListBtn()
    {
        var id = characterDropDown.value;
        nowPlayerPrefab = characterDropDown.value;
        for (int i = 0; i < playerPrefabs.Count; i++)
        {
            if(i == id) playerPrefabs[i].SetActive(true);
            else playerPrefabs[i].SetActive(false);
        }
    }
    public void OnClickHitEffectBtn()
    {
        if(int.TryParse(index.text, out value))
        {
            skeleton = DummyPos.gameObject.GetComponent<SkeletonAnimation>();
            if (skeleton == null)
            {
                Debug.LogWarning("SkeletonAnimation ÄÄÆ÷³ÍÆ® X");
                return;
            }

            var current = skeleton.AnimationState.GetCurrent(0);

            skeleton.AnimationState.SetAnimation(0, "Stand_Stop", true);

            //EffectManager._instance.RunEffectOnCharacterPosition(int.Parse(index.text), DummyPos, true);
        }
        else
        {
            Debug.LogError("[InputFeild] Error: Input is not number. Fix the value");
            return;
        }

    }
    public void OnClickStatusListBtn()
    {
        playerPrefabs[nowPlayerPrefab].GetComponent<Character>().TestState((SoundStateType)characterStateDropDown.value);
    }
    public void OnClickUIBtn()
    {
        AudioManager._instance.PlayUISound((UISFX)uiDropDown.value, uiVolumeSlider.value);
    }
    public void OnClickSysBtn()
    {
        AudioManager._instance.PlaySysSound((SYSTEMSFX)sysDropDown.value, sysVolumeSlider.value);
    }
    public void OnClickPlayEnv()
    {
        var id = (ENVIRONMENTALSFX)envDropDown.value;
        AudioManager._instance.PlayEnvSound(id, envVolumeeSlider.value);
    }
    public void OnClickStopEnv()
    {
        AudioManager._instance.StopEnv();
    }

    public void OnClickPlayBGM()
    {
        var id = (BGMLIST)bgmDropDown.value;
        //AudioManager._instance.PlayBGMSound(id, bgmVolumeSlider.value);
    }
    public void OnClickStopBGM()
    {
        AudioManager._instance.StopBGM();
    }
    public void OnBGMVolumeValueChagned(float volume)
    {
        AudioManager._instance.SetBgmVolume(volume);
    }
    public void OnEnvVolumeValueChagned(float volume)
    {
        AudioManager._instance.SetEnvVolume(volume);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
