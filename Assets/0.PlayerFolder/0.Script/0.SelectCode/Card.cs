using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    private System.Action onClick;

    [SerializeField] private Image cardImage;
    [SerializeField] private GameObject selectionOutline; 
    [SerializeField] private Button cardButton;
    [SerializeField] private Image onlyLook;
    [SerializeField] private string Cardtype;
    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning("[Card] Button 컴포넌트가 없습니다!");
        }
    }
    public void Init(System.Action onClickCallback, string type, Sprite sprite)
    {
        onClick = onClickCallback;
        SetImage(sprite);
        Cardtype = type;
        SetSelected(false,Color.red);
    }

    public void SetInteractive(bool value)
    {
        if (cardButton != null)
        {
            cardButton.interactable = value;
        }
    }


    public void SetSelected(bool selected,Color color)
    {
        // 디버깅 또 해보자.
        Debug.Log($"[selectionOutline Debug] selectionOutline: {(selectionOutline == null ? "NULL" : "OK")}, activeSelf: {selectionOutline?.activeSelf}, activeInHierarchy: {selectionOutline?.activeInHierarchy}");

        if (selectionOutline != null)
        {
            selectionOutline.SetActive(selected);

            // 디버깅 해보자.
            var image = selectionOutline.GetComponent<Image>();
            image.color = color;
            Debug.Log($"[selectionOutline Debug] SetSelected → selected: {selected}, color: {color}, alpha: {image.color.a}, image.enabled: {image.enabled}, active: {selectionOutline.activeSelf}");

            selectionOutline.GetComponent<Image>().color = color;
        }
    }
    public void OnClick()
    {
        onClick?.Invoke(); 
    }
    
    public Button ReturnButton()
    {
        return cardButton;
    }

    public string ReturnType()
    {
        return Cardtype;
    }
    public void SetImage(Sprite sprite)
    {
        cardImage.sprite = sprite;
    }

    public void SetOnlyLook()
    {
        onlyLook.enabled = true;
    }
}