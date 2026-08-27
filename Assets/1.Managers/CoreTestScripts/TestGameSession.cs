using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameSession : MonoBehaviour
{
    public static TestGameSession _intance {  get; private set; }
    public int seed; // not use now
    public int[] leftBaseStatIds = new int[3];
    public int[] rightBaseStatIds = new int[3];

    private void Awake()
    {
        if(_intance && _intance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    public void SetTestRound(int[] left, int[] right)
    {
        left.CopyTo(leftBaseStatIds, 0);
        right.CopyTo(rightBaseStatIds, 0);
    }

    public void SetRound(int seed, int[] left, int[] right)
    {
        this.seed = seed;
        left.CopyTo(leftBaseStatIds, 0);
        right.CopyTo(rightBaseStatIds, 0);
    }
}
