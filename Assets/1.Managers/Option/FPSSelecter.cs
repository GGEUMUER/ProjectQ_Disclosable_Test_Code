using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSSelecter : MonoBehaviour
{

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    public void SetFPS30(bool isOn)
    {
        if (!isOn) return;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }
    public void SetFPS60(bool isOn)
    {
        if (!isOn) return;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
    public void SetFPS120(bool isOn)
    {
        if (!isOn) return;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }
    public void SetFPS144(bool isOn)
    {
        if (!isOn) return;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;
    }
}
