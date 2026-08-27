using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class VolumController : MonoBehaviour
{
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    public GameObject settingPanel;
    public Image settingPanelImageSript;

    bool _isSettingPanelOpen = false;
    bool _isSettingPanelInAction = false;

    private void Start()
    {
        bgmVolumeSlider.onValueChanged.AddListener(AudioManager._instance.SetBgmVolume);
        sfxVolumeSlider.onValueChanged.AddListener(AudioManager._instance.SetSFXVolume);
        settingPanelImageSript = settingPanel.GetComponent<Image>();
    }

    public void OnClickSettingBtn()
    {
        if (!_isSettingPanelInAction)
        {
            if (_isSettingPanelOpen)
            {
                _isSettingPanelInAction = true;
                settingPanelImageSript.DOFade(0, 0.5f).OnComplete(() =>
                {
                    _isSettingPanelInAction = false;
                    settingPanel.SetActive(false);
                });

                _isSettingPanelOpen = false;
            }
            else
            {
                Color color = settingPanelImageSript.color;
                color.a = 0.39f;
                settingPanelImageSript.color = color;

                settingPanel.SetActive(true);
                _isSettingPanelOpen = true;
            }
        }
    }
}
