using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectStartAlphaChange : MonoBehaviour
{
    AlphaChanger alphaChanger;

    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<AlphaChanger>() != null) alphaChanger = GetComponent<AlphaChanger>();
        alphaChanger.ToFullAlphaSR(0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
