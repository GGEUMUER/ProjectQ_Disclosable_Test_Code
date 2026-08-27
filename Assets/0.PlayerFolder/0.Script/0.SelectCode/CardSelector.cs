using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CardInfo
{
    public string type;
    public Sprite sprite;
}
public class CardSelector : MonoBehaviour
{
   public Transform container;
   public Transform lookContatiner;
   public GameObject lookPannel;
   public List<CardInfo> cardImage = new List<CardInfo>();
   private Dictionary<string, Sprite> cardImageDic = new Dictionary<string, Sprite>();
   private List<Card> myCards=new List<Card>();
   private List<Card> enemyCards=new List<Card>();
   public GameObject cardPrefab;
   private int? localIndex=-1;

    List<string> remainingCardList = new(); // 클릭 후 남은 카드 목록
   private void Start()
   {
       foreach (var cardInfo in cardImage)
       {
           cardImageDic.Add(cardInfo.type, cardInfo.sprite);
       }
   }

    public void DebugCardList()
    {
        Debug.LogWarning(myCards);
        Debug.LogWarning(enemyCards);
    }
 // 아래는 사용하지 않는 코드
    /*
     * 이전 코드
   public void InstantiateCard(PickFirstCards data)
   {
       string[] myCardTypes; 
       string[] enemyCardtypes;
       if (data.isOwner)
       {
           myCardTypes = data.firstCardTypes;
           enemyCardtypes = data.secondCardtypes;
       }
       else
       {
           myCardTypes = data.secondCardtypes;
           enemyCardtypes = data.firstCardTypes;
       }
       
       
       lookPannel.SetActive(false);
       localIndex = -1;
       if (container.childCount != 0)
       {
           foreach (Transform child in container)
           {
               Destroy(child.gameObject);
           }
           foreach (Transform e_child in lookContatiner)
           {
               Destroy(e_child.gameObject);
           }
           myCards.Clear();
           enemyCards.Clear();
       }
      for (int i=0; i< myCardTypes.Length;i++)
      { 
          int index = i;
          GameObject card=Instantiate(cardPrefab,container);
          Card cardComponent = card.GetComponent<Card>();
          string cardType=myCardTypes[index];
          if (data.progress %2 == 1)
          {
              if(i==0) cardComponent.Init(() => OnCardClicked(index), cardType, cardImageDic[cardType]);
              else cardComponent.Init(() => OnCardClicked(index), cardType, cardImageDic["Mystery"]);
          }
          else
          {
              cardComponent.Init(() => OnCardClicked(index), cardType, cardImageDic[cardType]);
          }

          myCards.Add(cardComponent);
          UpdateFieldCards(data.isMyTurn);
      }

      if (data.progress%2 ==1)
      {
          lookPannel.SetActive(true);
          for (int i = 0; i < enemyCardtypes.Length; i++)
          {
              int index = i;
              GameObject card = Instantiate(cardPrefab, lookContatiner);
              Card cardComponent = card.GetComponent<Card>();
              string cardType=enemyCardtypes[index];

              if(i==0) cardComponent.Init(() => OnCardClicked(index), cardType, cardImageDic[cardType]);
              else cardComponent.Init(() => OnCardClicked(index), cardType, cardImageDic["Mystery"]);

              cardComponent.SetOnlyLook();
              enemyCards.Add(cardComponent);
          }
      }

      if (data.progress != 0 && data.progress % 2 == 0)
      {
          if (myCards[0].ReturnType() == myCards[1].ReturnType())
          {
              for (int i = 0; i < myCards.Count; i++)
              {
                  myCards[i].SetInteractive(false);
                  myCards[i].SetSelected(true,Color.yellow);
              }
          }
      }
   }
    */

    /// <summary>
    /// UpdateFieldCards(isMyTurn, null, step); InstantiateCard()에서
    /// 카드를 생성하는 로직으로 변경
    /// UpdateFieldCards(isMyTurn, selectedCardIndexes, currentStep);
    /// </summary>
    /// <param name="isMyTurn"></param>
    /// <param name="selectedIndexes"></param>
    /// <param name="step"></param>

    public void UpdateFieldCards(bool isMyTurn, List<int?> selectedIndexes = null, int step = -1)
    {
        for(int i=0; i < myCards.Count; i++)
        {
            if(isMyTurn) // 내 턴일 경우 상호작용이 가능하도록
            {
                myCards[i].SetInteractive(true);
            }
            else
            {
                myCards[i].SetInteractive(false);
            }
        }

        if (selectedIndexes != null) // 이미 선택이 되었는가?
        {
            foreach(int? selected in selectedIndexes)
            {
                if (selected is int index && index >= 0 && index < myCards.Count)
                {
                    if(index == localIndex)
                    {
                        myCards[index].SetSelected(true, Color.cyan);
                    }
                    else
                    {
                        myCards[index].SetSelected(true, Color.red);
                    }
                }
            }
        }
    }

    // 이전 코드 UpdateFieldCards with @Override
    /* 이전 코드
    public void UpdateFieldCards(bool myTurn)
   {
       for (int i = 0; i < myCards.Count; i++)
       {
           if (myTurn)
           {
               myCards[i].SetInteractive(true);
               myCards[i].SetSelected(false,Color.red);
           }
           else
           {
               myCards[i].SetInteractive(false);
               myCards[i].SetSelected(false,Color.red);
           }
       }
   }
   public void UpdateFieldCards(bool myTurn,List<int?> selectedCards)
   {
       UpdateFieldCards(myTurn);
       for (int i = 0; i < myCards.Count; i++)
       {
           for (int j = 0; j < selectedCards.Count; j++)
           {
               if (i == selectedCards[j])
               {
                   myCards[i].SetInteractive(false);
                   if(selectedCards[j]==localIndex)
                       myCards[i].SetSelected(true,Color.red);
                   else
                       myCards[i].SetSelected(true,Color.cyan);
               }
           }
       }
   }
   public void UpdateFieldCards(bool myTurn,List<int?> selectedCards, int progress)
   {
       UpdateFieldCards(myTurn,selectedCards);
       if (progress != 0 && progress % 2 == 0)
       {
           if (myCards[0].ReturnType() == myCards[1].ReturnType())
           {
               for (int i = 0; i < myCards.Count; i++)
               {
                   myCards[i].SetInteractive(false);
                   myCards[i].SetSelected(true,Color.red);
               }
           }
       }
   }
    */

    /*
     * 이전코드
    public void UpdateSelectedCard(CardSelectedpayload data)
    {
        int? mySelectedIndex;
        int? enemySelectedIndex;
        if (data.isFirst)
        {
            mySelectedIndex = data.firstSelectedIndex;
            enemySelectedIndex = data.secondSelectedIndex;
        }
        else
        {
            mySelectedIndex = data.secondSelectedIndex;
            enemySelectedIndex = data.firstSelectedIndex;
        }


        List<int?> selectedCards = new List<int?>();

        if (mySelectedIndex != null)
        {
            localIndex = mySelectedIndex;
            selectedCards.Add(mySelectedIndex);
        }

        if (enemySelectedIndex != null)
        {
            switch (data.progress)
            {
                case 0:
                    selectedCards.Add(enemySelectedIndex); //enemyIndex
                    break;
                case 1:
                case 3:
                    UpdateLookCard(enemySelectedIndex);
                    break;
            }

        }
        UpdateFieldCards(data.isMyTurn, selectedCards, data.progress);
    }*/

    /// <summary>
    /// 선택된 카드가 아닌, 카드를 선택하는 파트. 중요: 첫 카드를 선택하는 파트와 다름
    /// </summary>
    /// <param name="index"></param>
   void OnCardClicked(int index)
   {
        List<int?> selectedCards=new List<int?>();
         for (int i = 0; i < myCards.Count; i++)
         {
             if (myCards[i].ReturnButton().interactable == false)
             {
                 if(localIndex!=i)
                 selectedCards.Add(i);
             }
         }
         localIndex = index;


         selectedCards.Add(localIndex);
         UpdateFieldCards(true, selectedCards);
        
       GameSession.Instance.Sender.SendPacket("CardSelected",index);
   }

    /// <summary>
    /// 카드 덱 중 하나를 고를때
    /// </summary>
    /// <param name="idx"></param>
    private void OnCardClicked_ChooseOne(int idx)
    {
        localIndex = idx;
        Debug.LogWarning($"[OnCardClicked Debug] Count: {myCards.Count}");

        for (int i = 0; i < myCards.Count; i++)
        {
            if(i == idx)
            {
                myCards[i].SetSelected(true, Color.cyan);
            }
        }

        GameSession.Instance.Sender.SendPacket("CardSelected", idx);
    }
    /// <summary>
    /// 카드 제거할 때
    /// </summary>
    /// <param name="idx"></param>
    private void OnCardClicked_RemoveOne(int idx)
    {
        localIndex = idx;
        Debug.LogWarning($"[OnCardClicked Debug] Count: {myCards.Count}");
        
        for (int i = 0; i < myCards.Count; i++)
        {
            if (i == idx)
            {
                myCards[i].SetSelected(true, Color.cyan);
            }
        }

        GameSession.Instance.Sender.SendPacket("CardSelected", idx);
    }

    void UpdateLookCard(int? index)
   {
       for (int i = 0; i < enemyCards.Count; i++)
       {
            if (i ==index)
            {
                enemyCards[i].SetInteractive(false);
                enemyCards[i].SetSelected(true,Color.cyan);
            }
            else
            {
                enemyCards[i].SetInteractive(true);
                enemyCards[i].SetSelected(false,Color.cyan);
            }
       }
   }

    public void ShowDealtCards(string myCard, string opponentCard, int step)
    {
        ClearCardUI();
        localIndex = -1;

        List<string> dealt = new List<string>
        {
            myCard,
            "Mystery",
            "Mystery"
        }; 

        for(int i=0; i < dealt.Count; i++)
        {
            string type = i == 0 ? myCard : "Mystery";
            int idx = i;

            var obj = Instantiate(cardPrefab, container);
            var card = obj.GetComponent<Card>();
            card.Init(() => OnCardClicked_RemoveOne(idx), type, cardImageDic[type]);
            card.SetInteractive(true);

            myCards.Add(card);
        }

        lookPannel.SetActive(true);
        foreach (Transform child in lookContatiner)
        {
            Destroy(child.gameObject);
        }

        enemyCards.Clear();

        var enemyObj = Instantiate(cardPrefab, lookContatiner);
        var enemyCard = enemyObj.GetComponent<Card>();
        enemyCard.Init(null, opponentCard, cardImageDic[opponentCard]);
        enemyCard.SetOnlyLook();
        enemyCards.Add(enemyCard);

        /*
        List<string> copy = new List<string> { myCard}; // 내 카드는 확정적, 안에서는 미스터리로 취급되어 있음. 남은건 미스터리 2장

        var random2pick = new System.Random();
        List<string> picked2 = new();
        
        for(int i = 0; i <2; i++)
        {
            if (copy.Count == 0) break;
            int randIdx = random2pick.Next(copy.Count);
            picked2.Add(copy[randIdx]);
            copy.RemoveAt(randIdx);
        }

        List<string> dealt = new List<string>(picked2);
        dealt.Add(myCard);

        dealt = dealt.OrderBy(_ => UnityEngine.Random.value).ToList();

        for(int i = 0; i < dealt.Count; i++)
        {
            string type = dealt[i];
            int idx = i;

            var obj = Instantiate(cardPrefab, container);
            var card = obj.GetComponent<Card>();

            bool isRevealed = (type == myCard);
            card.Init(() =>
                        OnCardClicked(idx), type,
                                    isRevealed ? cardImageDic[type] : cardImageDic["Mystery"]);
            card.SetInteractive(true);
            myCards.Add(card);
        }

        lookPannel.SetActive(true);
        foreach(Transform child in lookContatiner)
        {
            Destroy(child.gameObject);
        }

        enemyCards.Clear();

        var enemyObj = Instantiate(cardPrefab, lookContatiner);
        var enemyCard = enemyObj.GetComponent<Card>();
        enemyCard.Init(null, opponentCard, cardImageDic[opponentCard]);
        enemyCard.SetOnlyLook();
        enemyCards.Add(enemyCard);
        */
    }

    public void ShowRemainingCards(string[] units, int step)
    {
        ClearCardUI();
        localIndex = -1;

        if(units == null || units.Length != 2)
        {
            Debug.LogError($"PickTwoCards: 함수 ShowRemainingCards, 카드 수가 2개가 아님. 현재: {units?.Length}");
            return;
        }

        string typeA = units[0];
        string typeB = units[1];

        if(typeA == typeB)
        {
            Debug.Log($"자동선택됩니다");

            var obj = Instantiate(cardPrefab, container);
            var card = obj.GetComponent<Card>();
            card.Init(null, typeA, cardImageDic[typeA]);

            var obj2 = Instantiate(cardPrefab, container);
            var card2 = obj2.GetComponent<Card>();
            card2.Init(null, typeB, cardImageDic[typeA]);

            card.SetSelected(true, Color.yellow);
            card.SetInteractive(false);
            myCards.Add(card);

            GameSession.Instance.Sender.SendPacket("CardSelected", 0);
            return;
        }

        for(int i = 0; i < units.Length; i++)
        {
            string cardType = units[i];
            int idx = i;
            
            var obj = Instantiate(cardPrefab, container);
            var card = obj.GetComponent<Card>();
            card.Init(() => OnCardClicked_ChooseOne(idx), cardType, cardImageDic[cardType]);
            card.SetInteractive(true);
            myCards.Add(card);
        }
    }

    public void InstantiateFirstPick(string[] allTypes, bool isMyTurn, int step)
    {
        ClearCardUI();
        localIndex = -1;
        
        remainingCardList = new List<string>(allTypes);

        for(int i = 0; i < allTypes.Length; i++)
        {
            string cardType = allTypes[i];
            int idx = i;

            var cardObj = Instantiate(cardPrefab, container);
            var cardCmp = cardObj.GetComponent<Card>();

            bool interactable = isMyTurn && step == 0; // 내턴인가?

            cardCmp.Init(() => OnCardClicked(idx), cardType, cardImageDic[cardType]);
            cardCmp.SetInteractive(interactable);

            myCards.Add(cardCmp);
        }

        if (!isMyTurn)
        {
            Debug.Log("상대 턴입니다!!!!!!!!!!!");
        }
    }

    private void ClearCardUI()
    {
        foreach(Transform child in container)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in lookContatiner)
        {
            Destroy(child.gameObject);
        }

        myCards.Clear();
        enemyCards.Clear();

        lookPannel.SetActive(false);
    }

    public void ReflectSelectedCard(string unitType, bool isOwner, int step)
    {
        int? matchedIdx = null;
        for(int i=0; i< myCards.Count; i++)
        {
            if (myCards[i].ReturnType() == unitType)
            {
                matchedIdx = i;
                break;
            }
        }

        if(matchedIdx == null)
        {
            Debug.LogError($"State: UpdateSelectedCard || 매칭 카드 타입 없음. 에러임. {unitType}");
            return; ;
        }

        if(remainingCardList.Contains(unitType))
        {
            remainingCardList.Remove(unitType);
        }
        else
        {
            Debug.LogError($"리스트에 {unitType} 없는데? 에러임");
        }

        for(int i = 0; i < myCards.Count;i++)
        {
            if (i == matchedIdx) // 선택된 놈 false + 내가 선택했는지 남이 선택했는지 헷갈려서 색깔로 구분
            {
                myCards[i].SetSelected(true, isOwner ? Color.cyan : Color.gray);
                myCards[i].SetInteractive(false);
            }
            else // 선택된 놈이 아니라면? (회색 처리)
            {
                bool enableOther = (step == 2 && isOwner);
                myCards[i].SetInteractive(enableOther);
            }
        }

        localIndex = isOwner ? matchedIdx : localIndex;
    }
}