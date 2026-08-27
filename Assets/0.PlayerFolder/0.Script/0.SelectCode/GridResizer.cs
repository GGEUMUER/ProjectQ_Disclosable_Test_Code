using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridResizer : MonoBehaviour
{
    public Camera myCamera; // 자신 전용 카메라
    public int columns = 10;
    public int rows = 6;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying == false)
        {
            if (myCamera == null) myCamera = Camera.main; // fallback
            FitToCamera(myCamera);
        }
    }
#endif

    public void Awake()
    {
        if (myCamera == null) myCamera = Camera.main; // fallback
        FitToCamera(myCamera);
    }

    public void FitToCamera(Camera cam)
    {
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float cellWidth = camWidth / columns;
        float cellHeight = camHeight / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        GetComponent<Grid>().cellSize = new Vector3(cellSize, cellSize, 0f);
    }
}

