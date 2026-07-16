using TMPro;
using UnityEngine;

// 상점 아이템 버튼 한 개
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;  // 아이템 이름 (영구템이면 레벨도)
    [SerializeField] TMP_Text priceText; // 가격 (못 사면 SOLD OUT)

    ShopView _view;
    ItemDefinition _item;

    // 만들어질 때 view 가 채워줌
    public void Set(ShopView view, ItemDefinition item)
    {
        _view = view;
        _item = item;

        nameText.text = item.itemName;
        if (item.kind == ItemKind.Permanent)
        {
            int level = GameManager.instance.inventory.GetItemLevel(item.id);
            nameText.text = item.itemName + " lv" + level + " / " + item.maxLevel;
        }

        if (GameManager.instance.shop.CanBuy(item) == false)
        {
            priceText.text = "SOLD OUT";
        }
        else
        {
            priceText.text = item.price + " cr";
        }
    }

    // 버튼 OnClick 에 연결
    public void OnClick()
    {
        _view.OnClickItem(_item);
    }
}
