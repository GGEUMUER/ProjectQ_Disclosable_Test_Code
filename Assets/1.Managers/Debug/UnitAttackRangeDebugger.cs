using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttackRangeDebugger : MonoBehaviour
{
    public enum ColorType
    {
        White,
        Black,
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        Gray,
        RomaRed,
        Clear
    }
    
    public ColorType unitDebugRangeColorType;
    [SerializeField] float _range = 0;

    private void OnDrawGizmos()
    {
        if(_range <= 0)
        {
            return;
        }

        switch (unitDebugRangeColorType)
        {
            case ColorType.White:
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Black:
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Red:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Green:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Blue:    
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Yellow:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Cyan:
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Magenta:
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Gray:
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;

            case ColorType.RomaRed:
                Gizmos.color = new Color32(142, 0, 28, 150);
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            case ColorType.Clear:
                Gizmos.color = Color.clear;
                Gizmos.DrawWireSphere(new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z), _range);
                break;
            default:
            break;
        }
    }

    public void SetRange(Vector3 targetPos)
    {
        _range = Vector3.Distance(transform.position, targetPos);

    }
}
