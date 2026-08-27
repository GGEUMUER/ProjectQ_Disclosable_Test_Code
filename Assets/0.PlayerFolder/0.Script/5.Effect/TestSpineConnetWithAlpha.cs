using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpineConnetWithAlpha : MonoBehaviour
{
    public GameObject stand_test;
    public GameObject death_test;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(this.gameObject.GetComponent<SkeletonAnimation>().AnimationState.GetCurrent(0).Animation.Duration);

        if(this.gameObject.GetComponent<SkeletonAnimation>().AnimationState.GetCurrent(0).Animation.Name == "Death")
        {
            death_test.GetComponent<AlphaChanger>().ToZeroAlphaSR(this.gameObject.GetComponent<SkeletonAnimation>().AnimationState.GetCurrent(0).Animation.Duration);
        }
        if (this.gameObject.GetComponent<SkeletonAnimation>().AnimationState.GetCurrent(0).Animation.Name == "Stand")
        {
            stand_test.GetComponent<AlphaChanger>().ToZeroAlphaSR(this.gameObject.GetComponent<SkeletonAnimation>().AnimationState.GetCurrent(0).Animation.Duration);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
