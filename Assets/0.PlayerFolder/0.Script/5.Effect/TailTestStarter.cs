using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailTestStarter : MonoBehaviour
{
    public Camera zoom;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<SnapshotGhostTailTest>().PlayBurst(Vector2.left);
        zoom.GetComponent<ZoomInCamera>().KickZoomPercent(0.36f, 0.62f, 0.86f, 0.36f, overshoot: true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
