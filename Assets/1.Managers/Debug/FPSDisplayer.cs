using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSDisplayer : MonoBehaviour
{
    float delatTime = .0f;

    // Update is called once per frame
    void Update()
    {
        delatTime = (Time.deltaTime - delatTime * 0.1f);
    }

    private void OnGUI()
    {
        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();

        style.fontSize = height / 50;
        style.alignment = TextAnchor.LowerLeft; 
        style.normal.textColor = Color.green;

        float msec = delatTime * 1000.0f;
        float fps = 1.0f / delatTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        float paddingg = 10.0f;
        Rect ract = new Rect(paddingg, height - height / 50 - paddingg, width, height / 100);
        GUI.Label(ract, text, style);
    }
}
