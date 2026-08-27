using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangerParticle : MonoBehaviour
{
    public GameObject bullet;
    public LineRenderer upLine;
    public LineRenderer downLine;
    public TrailRenderer trail;
    public float delayTime = 0.5f;
    
    private float lerpX;
    public void FirstSet()
    {
        upLine.SetPosition(0,bullet.transform.position+Vector3.up*1);
        downLine.SetPosition(0,bullet.transform.position-Vector3.up*1);
        upLine.SetPosition(1,bullet.transform.position+Vector3.up*1);
        downLine.SetPosition(1,bullet.transform.position-Vector3.up*1);
        if (bullet.GetComponent<Bullet>().speed > 0)
        {
            upLine.textureScale = new Vector2(1,-0.8f);
            downLine.textureScale = new Vector2(1,0.8f);
            trail.textureScale=new Vector2(1,1);
        }
        else
        {
            upLine.textureScale = new Vector2(1,0.8f);
            downLine.textureScale = new Vector2(1,-0.8f);
            trail.textureScale=new Vector2(1,-1);
        }
        trail.Clear();
    }
    // Update is called once per frame
    void Update()
    {
        trail.transform.position=bullet.transform.position;
        lerpX = upLine.GetPosition(0).x;
        lerpX = Mathf.Lerp(lerpX,upLine.GetPosition(1).x,Time.deltaTime*delayTime);
        
        upLine.SetPosition(0,new Vector3(lerpX,upLine.GetPosition(0).y,upLine.GetPosition(0).z));
        downLine.SetPosition(0,new Vector3(lerpX,downLine.GetPosition(0).y,downLine.GetPosition(0).z));
        upLine.SetPosition(1,bullet.transform.position+Vector3.up*0.2f);
        downLine.SetPosition(1,bullet.transform.position-Vector3.up*0.4f);
    }
}
