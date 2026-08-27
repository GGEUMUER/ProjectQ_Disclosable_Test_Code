using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

public class AlphaChanger : MonoBehaviour
{
    [SerializeField] 
    SpriteRenderer sr;

    // Start is called before the first frame update
    void Awake()
    {
        if(GetComponent<SpriteRenderer>() != null) sr = GetComponent<SpriteRenderer>();
        //ToZeroAlphaSR(3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToZeroAlphaSR(float time)
    {
        StartCoroutine(SetAlphaToZero(time));
    }
    public void ToFullAlphaSR(float time)
    {
        StartCoroutine(SetAlphaToFull(time));
    }

    IEnumerator SetAlphaToZero(float time)
    {
        float calculator = 0f;

        if (sr != null) // works well (for spriteRenderer), Must do not Use this Method at Awake. 
        {
            while (calculator < time)
            {
                var color = sr.color;
                color.a = Mathf.Lerp(color.a, 0f, calculator / time);
                sr.color = color;
                yield return null;

                calculator += Time.deltaTime;
            }
            var finalcolor = sr.color;
            finalcolor.a = 0f;
            sr.color = finalcolor;
        }
        else // works well (for UI)
        {
            while (calculator < time)
            {
                foreach (var gameObj in this.gameObject.transform.GetComponentsInChildren<Graphic>(true))
                {
                    var color = gameObj.color;
                    color.a = Mathf.Lerp(color.a, 0f, calculator / time);
                    gameObj.color = color;
                    yield return null;
                }
                calculator += Time.deltaTime;
            }
            foreach (var gameObj in this.gameObject.transform.GetComponentsInChildren<Graphic>(true))
            {
                var color = gameObj.color;
                color.a = 0f;
            }
        }
    }
    IEnumerator SetAlphaToFull(float time)
    {
        float calculator = 0f;

        if (sr != null) // works well (for spriteRenderer), Must do not Use this Method at Awake. 
        {
            while (calculator < time)
            {
                var color = sr.color;
                color.a = Mathf.Lerp(0f, color.a, calculator / time);
                sr.color = color;
                yield return null;

                calculator += Time.deltaTime;
            }
            var finalcolor = sr.color;
            finalcolor.a = 0f;
            sr.color = finalcolor;
        }
        else // works well (for UI)
        {
            while (calculator < time)
            {
                foreach (var gameObj in this.gameObject.transform.GetComponentsInChildren<Graphic>(true))
                {
                    var color = gameObj.color;
                    color.a = Mathf.Lerp(0f, color.a, calculator / time);
                    gameObj.color = color;
                    yield return null;
                }
                calculator += Time.deltaTime;
            }
            foreach (var gameObj in this.gameObject.transform.GetComponentsInChildren<Graphic>(true))
            {
                var color = gameObj.color;
                color.a = 1f;
            }
        }
    }
}
