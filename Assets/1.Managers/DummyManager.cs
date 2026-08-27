using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class DummyManager : MonoBehaviour
{
    [Header("DummyPos")]
    [SerializeField]
    Transform[] dummyPoses;

    [Header("Dummy")]
    [SerializeField]
    GameObject dummy;

    int dummyTileCount = 2;

    private void Start()
    {
        dummy.transform.position = dummyPoses[dummyTileCount].position;
        dummy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    public void OnClickDummyRightBtn()
    {
        if (dummyTileCount >= 4) return;
        dummyTileCount++;
        dummy.transform.position = dummyPoses[dummyTileCount].position;

    }

    public void OnClickDummyLeftBtn()
    {
        if (dummyTileCount <= 0) return;
        dummyTileCount--;
        dummy.transform.position = dummyPoses[dummyTileCount].position;

    }
}
