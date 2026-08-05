using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 아이템 버튼 한 개
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;  // 아이템 이름 (영구템이면 레벨도)
    [SerializeField] TMP_Text priceText; // 가격 (못 사면 그 이유)
    [SerializeField] TMP_Text descText;  // 설명 (레벨 오르면 바뀔 수 있음)
    [SerializeField] Image iconImage;    // 그림 (레벨 오르면 바뀔 수 있음)

    ShopView _view;
    ItemDefinition _item;

    // 만들어질 때 view 가 채워줌
    public void Set(ShopView view, ItemDefinition item)
    {
        _view = view;
        _item = item;

        // 영구템은 다음에 살 게 몇 레벨짜리인지 같이 보여줌
        int level = GameManager.instance.inventory.GetItemLevel(item.id);
        nameText.text = item.itemName;
        if (item.kind == ItemKind.Permanent)
        {
            nameText.text = item.itemName + " lv" + level + " / " + item.maxLevel;
        }

        // 그림이랑 설명은 지금 레벨에 맞는 걸로 (만렙이면 달라짐)
        if (descText != null)
        {
            descText.text = item.DescriptionFor(level);
        }
        if (iconImage != null)
        {
            Sprite icon = item.IconFor(level);
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        // 품절인지, 아직 조건이 안 된 건지 구별해서 보여줌
        string reason = GameManager.instance.shop.BuyBlockReason(item);
        if (reason.Length > 0)
        {
            priceText.text = reason;
        }
        else
        {
            // 영구템은 다음 레벨 값, 1회성은 그동안 산 만큼 오른 값이라 상점에 물어봄
            priceText.text = GameManager.instance.shop.PriceFor(item) + " cr";
        }
    }

    // 버튼 OnClick 에 연결
    public void OnClick()
    {
        _view.OnClickItem(_item);
    }
}
