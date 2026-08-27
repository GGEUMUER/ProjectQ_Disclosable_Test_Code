using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterImageList 
{ 
    Lv1Default,
    Lv2Default,
    Lv1Guardian,
    Lv1Assaulter,
    Lv1Ranger,
    Lv1Wizard,
    Lv1Supporter,
    Lv2Guardian,
    Lv2Assaulter,
    Lv2Ranger,
    Lv2Wizard,
    Lv2Supporter
}

public class UnitInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject unitInfo_BG;
    [SerializeField] GameObject playerInfoPanel;
    [SerializeField] GameObject enemyInfoPanel;
    [SerializeField] GameObject[] _enemyUnits;
    [SerializeField] Sprite[] characterImage;

    private void Start()
    {
        if (unitInfo_BG != null)
        {
            unitInfo_BG.SetActive(false);
        }
    }

    public void TogglePanel() // 버튼 활성화
    {
        if (unitInfo_BG != null)
        {
            unitInfo_BG.SetActive(!unitInfo_BG.activeSelf);
        }
    }

    public void SearchSecondAttackCharacterList(GameObject character)
    {
        // 나중에 좌 우 변환이 들어가면, 조건문 플래그로 바꿔 진행
        string characterName = character.GetComponent<SkeletonAnimation>().skeletonDataAsset.name;
                
        switch (characterName)
        {
            case "11001_SkeletonData":
                _enemyUnits[5].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv1Guardian];
                break;
            case "21001_SkeletonData":
                _enemyUnits[6].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv1Assaulter];
                break;
            case "31001_SkeletonData":
                _enemyUnits[7].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv1Ranger];
                break;
            case "41001_SkeletonData":
                _enemyUnits[8].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv1Wizard];
                break;
            case "51001_SkeletonData":
                _enemyUnits[9].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv1Supporter];
                break;
            case "12001_SkeletonData":
                _enemyUnits[0].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv2Guardian];
                break;
            case "22001_SkeletonData":
                _enemyUnits[1].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv2Assaulter];
                break;

            case "32001_SkeletonData":
                _enemyUnits[2].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv2Ranger];
                break;

            case "42001_SkeletonData":
                _enemyUnits[3].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv2Wizard];
                break;  

            case "52001_SkeletonData":
                _enemyUnits[4].GetComponent<Image>().sprite = characterImage[(int)CharacterImageList.Lv2Supporter];
                break;

            default:
                Debug.LogError("unexpected error");
                break;
        }
    }

    public void OnRightBtnClicked()
    {
        if(playerInfoPanel != null && enemyInfoPanel != null)
        {
            playerInfoPanel.SetActive(false);
            enemyInfoPanel.SetActive(true);
        }
    }

    public void OnLeftBtnClicked()
    {
        if (playerInfoPanel != null && enemyInfoPanel != null)
        {
            playerInfoPanel.SetActive(true);
            enemyInfoPanel.SetActive(false);
        }
    }
}

