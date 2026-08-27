using Core.Units;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTokenData", menuName = "ScriptableObjects/TokenData", order = 1)]
public class TokenData : ScriptableObject
{
    public string tokenName;
    public GameObject token;
    public Sprite icon_1;
    public Sprite icon_2;
    public Sprite icon_3;
    public Sprite icon_4;
    public int tokenId;
    public UnitType unitType;
}
