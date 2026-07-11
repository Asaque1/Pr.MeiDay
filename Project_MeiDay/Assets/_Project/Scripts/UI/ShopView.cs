using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 밤 상점 화면 - 아이템 목록 보여주고 사게 해줌
public class ShopView : MonoBehaviour
{
    [SerializeField] Transform itemParent;  // 아이템 버튼들 놓이는 자리
    [SerializeField] ShopItemUI itemPrefab; // 아이템 버튼 프리팹
    [SerializeField] TMP_Text infoText;     // 안내 문구 (샀다 / 돈 부족)

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

    // 아이템 목록 지우고 다시 만들기
    void Rebuild()
    {
        for (int i = 0; i < _itemUis.Count; i++)
        {
            Destroy(_itemUis[i].gameObject);
        }
        _itemUis.Clear();

        List<ItemDefinition> items = GameManager.instance.content.items;
        for (int i = 0; i < items.Count; i++)
        {
            ShopItemUI itemUi = Instantiate(itemPrefab, itemParent);
            itemUi.Set(this, items[i]);
            _itemUis.Add(itemUi);
        }
    }

    // 아이템 버튼이 눌렸다고 알려줌
    public void OnClickItem(ItemDefinition item)
    {
        // 우주선표는 한 번 샀으면 끝
        if (item.isSpaceshipTicket == true && GameManager.instance.hasSpaceship == true)
        {
            infoText.text = "이미 샀음";
            return;
        }

        if (GameManager.instance.shop.Buy(item) == true)
        {
            infoText.text = item.itemName + " 구매 완료";
        }
        else
        {
            infoText.text = "크레딧이 부족함";
        }
    }
}
