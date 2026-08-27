using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CharacterMPHPFade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    
    private void Start()
    {
        canvasGroup = this.gameObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    public void FadeIn(float time)
    {
        canvasGroup.DOFade(1f, time);
    }
    public void FadeOut(float time)
    {
        canvasGroup.DOFade(0f, time);
    }
}
