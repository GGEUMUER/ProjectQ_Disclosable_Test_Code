using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject cardSelectCanvas;
    [SerializeField]
    private GameObject unitBatchCanvas;
    private TimerBar timer;
    private GameObject CurrentCanvas;
    private Dictionary<string, GameObject> Canvas = new Dictionary<string, GameObject>();
    public void Awake()
    {
        Canvas.Add("SelectCanvas",cardSelectCanvas);
        Canvas.Add("BatchCanvas",unitBatchCanvas);
    }

    public void SetCurrentCanvas(string canvasName)
    {
        CurrentCanvas = Canvas[canvasName];
        if (CurrentCanvas.transform.GetChild(0).GetComponent<TimerBar>() != null)
        {
            timer = CurrentCanvas.transform.GetChild(0).GetComponent<TimerBar>();
        }
        else
        {
            timer = null;
        }
    }

    public TimerBar ReturnTimerBar()
    {
        return timer;
    }

    public GameObject ReturnCurrentCanvas()
    {
        return CurrentCanvas;
    }
}
