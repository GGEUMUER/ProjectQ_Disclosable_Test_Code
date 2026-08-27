using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DamageTextType
{
    Normal,
    Critical,
    Heal,
    MP,
    Shield
}

public class CharacterDamageTextUI : MonoBehaviour
{
    public Canvas textCanvas;
    TextMeshProUGUI damageText;

    Sequence sequence;
    CanvasGroup canvasGroup;

    public void ShowDamage(float damage, DamageTextType dtt)
    {
        Canvas targetCanvas = Instantiate(textCanvas, transform.position, Quaternion.identity);
        targetCanvas.transform.SetParent(this.gameObject.transform);
        SetTMPAlphaToZero(1.5f, damage, dtt, targetCanvas);
    }

    void SetTMPAlphaToZero(float time, float damage, DamageTextType dtt, Canvas target)
    {
        sequence = DOTween.Sequence();
        canvasGroup = target.GetComponentInChildren<CanvasGroup>();
        damageText = target.GetComponentInChildren<TextMeshProUGUI>();
        switch (dtt)
        {
            // Done
            case DamageTextType.Normal:
                damageText.color = Color.white;
                target.transform.transform.position = new Vector3(target.transform.position.x, -1.4f, target.transform.position.z);
                damageText.text = ((int)damage).ToString();
                break;

            // Done
            case DamageTextType.Critical:
                damageText.color = Color.red;
                target.transform.transform.position = new Vector3(target.transform.position.x, -0.9f, target.transform.position.z);
                damageText.text = "CRIT! " + ((int)damage).ToString();
                break;

            case DamageTextType.Heal:
                damageText.color = Color.green;
                target.transform.transform.position = new Vector3(target.transform.position.x, -0.4f, target.transform.position.z);
                damageText.text = "Heal! " + ((int)damage).ToString();
                break;

            // excepted. To many infomation.
            case DamageTextType.MP:
                damageText.color = Color.blue;
                target.transform.transform.position = new Vector3(target.transform.position.x, 0.1f, target.transform.position.z);
                damageText.text = "MP! " + ((int)damage).ToString();
                break;

            // No Event
            case DamageTextType.Shield:
                damageText.color = Color.yellow;
                target.transform.transform.position = new Vector3(target.transform.position.x, 0.6f, target.transform.position.z);
                damageText.text = "Shield! " + ((int)damage).ToString();
                break;
        }

        canvasGroup.alpha = 1f;

        DOTween.To(() => target.transform.position, x =>
            target.transform.position = x, new Vector3(target.transform.position.x, target.transform.position.y + 1f, target.transform.position.z), time);
        sequence.Append(canvasGroup.DOFade(0.0f, time)).OnComplete(() => Destroy(target.gameObject));
    }
}
