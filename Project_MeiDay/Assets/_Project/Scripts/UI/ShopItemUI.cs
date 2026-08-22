using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 아이템 한 칸
// 칸에는 그림이랑 값만 보여주고, 이름이랑 설명은 눌렀을 때 뜨는 창에서 보여줌
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] Image iconImage;        // 아이템 그림
    [SerializeField] TMP_Text priceText;     // 값 (또는 왜 못 사는지)
    [SerializeField] GameObject priceLabel;  // 값이 붙는 딱지
    [SerializeField] GameObject dim;         // 못 사는 물건을 덮는 어두운 판
    [SerializeField] GameObject soldOutMark; // 품절 딱지
    [SerializeField] Button button;

    ShopView _view;
    ItemDefinition _item;

    // 만들어질 때 view 가 채워줌
    public void Set(ShopView view, ItemDefinition item)
    {
        _view = view;
        _item = item;

        SetOn(priceLabel, true);
        if (button != null)
        {
            button.interactable = true;
        }

        // 그림은 지금 레벨에 맞는 걸로 (만렙이면 달라짐)
        int level = GameManager.instance.inventory.GetItemLevel(item.id);
        if (iconImage != null)
        {
            Sprite icon = item.IconFor(level);
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        string reason = GameManager.instance.shop.BuyBlockReason(item);
        bool blocked = (reason.Length > 0);

        // 못 사는 물건은 어둡게 덮음
        SetOn(dim, blocked);

        // 진짜 품절(더 살 게 없음)일 때만 품절 딱지
        // 레벨이 모자라서 못 사는 건 나중에 살 수 있으니 딱지 대신 조건을 적어줌
        bool soldOut = (reason == "SOLD OUT" || reason == "이미 삼");
        SetOn(soldOutMark, soldOut);
        SetOn(priceLabel, soldOut == false);

        if (priceText != null)
        {
            if (blocked == true)
            {
                priceText.text = reason;
            }
            else
            {
                // 영구템은 다음 레벨 값, 1회성은 그동안 산 만큼 오른 값이라 상점에 물어봄
                priceText.text = GameManager.instance.shop.PriceFor(item) + " cr";
            }
        }
    }

    // 물건이 모자랄 때 남는 빈 칸 (누르면 아무 일도 안 함)
    public void SetEmpty()
    {
        _view = null;
        _item = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
        }
        SetOn(priceLabel, false);
        SetOn(soldOutMark, false);
        SetOn(dim, false);

        if (button != null)
        {
            button.interactable = false;
        }
    }

    // 켜고 끄기 (아직 안 꽂아뒀어도 안 터지게)
    void SetOn(GameObject target, bool on)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(on);
    }

    // 버튼 OnClick 에 연결
    public void OnClick()
    {
        if (_view == null || _item == null)
        {
            return; // 빈 칸
        }

        _view.OnClickItem(_item);
    }
}
