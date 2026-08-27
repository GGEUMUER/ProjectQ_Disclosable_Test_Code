using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveAx
{
    X, Y, Z
}
public class RotateEffect : MonoBehaviour
{
    public float rotateSpeed = .0f;
    public MoveAx moveAX;

    // Update is called once per frame
    void Update()
    {
        switch (moveAX)
        {
            case MoveAx.X:
                this.gameObject.transform.Rotate(Vector3.right * Time.deltaTime * rotateSpeed);
                break;
            
            case MoveAx.Y:
                this.gameObject.transform.Rotate(Vector3.up * Time.deltaTime * rotateSpeed);
                break;
            
            case MoveAx.Z:
                this.gameObject.transform.Rotate(Vector3.forward * Time.deltaTime * rotateSpeed);
                break;
            
            default:
                break;

        }        
    }
}
