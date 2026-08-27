using UnityEngine;
using UnityEngine.UI;
using System;

public class TimerBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private float duration;
    private float remainingTime;
    public bool isRunning = false;

    public void SetTimer(float durationTime,float remainTime)
    {
        duration = durationTime;
        remainingTime = remainTime;
        isRunning = true;

        slider.interactable = false; // 드래그 방지
        slider.maxValue = durationTime;
        slider.value = remainTime;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (isRunning)
        {
            remainingTime -= Time.deltaTime;
            slider.value = Mathf.Clamp(remainingTime, 0f, duration);

            if (remainingTime <= 0f)
            {
                isRunning = false;
            }
        }
    }
}