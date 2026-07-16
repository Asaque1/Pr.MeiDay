using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 밤 상점 화면 - 상점 네 곳을 골라가며 아이템 사게 해줌
public class ShopView : MonoBehaviour
{
    [SerializeField] Transform itemParent;  // 아이템 버튼들 놓이는 자리
    [SerializeField] ShopItemUI itemPrefab; // 아이템 버튼 프리팹
    [SerializeField] TMP_Text infoText;     // 안내 문구 (샀다 / 돈 부족)
    [SerializeField] TMP_Text shopNameText; // 지금 보는 상점 이름

    public ShopType currentShop = ShopType.Bookstore; // 지금 보고 있는 상점

    List<ShopItemUI> _itemUis = new List<ShopItemUI>();

    // 패널 켜질 때 목록 만들어줌
    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        infoText.text = "";
        Rebuild();
    }

    // 지금 상점의 아이템 목록 지우고 다시 만들기
    void Rebuild()
    {
        for (int i = 0; i < _itemUis.Count; i++)
        {
            Destroy(_itemUis[i].gameObject);
        }
        _itemUis.Clear();

        if (shopNameText != null)
        {
            shopNameText.text = ShopName(currentShop);
        }

        List<ItemDefinition> items = GameManager.instance.content.items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].shopType != currentShop)
            {
                continue; // 다른 상점 물건
            }
            if (GameManager.instance.shop.IsUnlocked(items[i]) == false)
            {
                continue; // 아직 해금 안 된 물건은 안 보여줌
            }

            ShopItemUI itemUi = Instantiate(itemPrefab, itemParent);
            itemUi.Set(this, items[i]);
            _itemUis.Add(itemUi);
        }
    }

    // 상점 이름 (화면 표시용)
    string ShopName(ShopType type)
    {
        if (type == ShopType.Bookstore)
        {
            return "서점";
        }
        if (type == ShopType.Theater)
        {
            return "연극단";
        }
        if (type == ShopType.PartyShop)
        {
            return "파티용품점";
        }
        return "브로커";
    }

    // 아이템 버튼이 눌렸다고 알려줌
    public void OnClickItem(ItemDefinition item)
    {
        // 품절(SOLD OUT)은 눌러도 반응 없음
        if (GameManager.instance.shop.CanBuy(item) == false)
        {
            return;
        }

        if (GameManager.instance.shop.Buy(item) == true)
        {
            infoText.text = item.itemName + " 구매 완료";
            Rebuild(); // 레벨이랑 품절 표시 갱신
        }
        else
        {
            infoText.text = "크레딧이 부족함";
        }
    }

    // ---- 아래는 상점 탭 버튼 OnClick 에 연결해서 씀 ----

    // 탭: 서점
    public void OnClickBookstore()
    {
        ChangeShop(ShopType.Bookstore);
    }

    // 탭: 연극단
    public void OnClickTheater()
    {
        ChangeShop(ShopType.Theater);
    }

    // 탭: 파티용품점
    public void OnClickPartyShop()
    {
        ChangeShop(ShopType.PartyShop);
    }

    // 탭: 브로커
    public void OnClickBroker()
    {
        ChangeShop(ShopType.Broker);
    }

    // 다른 상점으로 바꾸기
    void ChangeShop(ShopType type)
    {
        currentShop = type;
        infoText.text = "";
        Rebuild();
    }
}
