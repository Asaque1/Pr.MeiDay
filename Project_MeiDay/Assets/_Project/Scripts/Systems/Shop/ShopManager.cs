using UnityEngine;

// 브로커 상점 매니저 (아이템 사기)
public class ShopManager : MonoBehaviour
{
    // 아이템 사기. 성공하면 true, 돈 없으면 false
    public bool Buy(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        // 돈 내보기
        bool paid = GameManager.instance.economy.SpendCredits(item.price);
        if (paid == false)
        {
            return false; // 돈 부족해서 못 삼
        }

        // 샀으니까 인벤토리에 넣기
        GameManager.instance.inventory.AddItem(item.id);

        // 우주선표면 탈출 가능 표시
        if (item.isSpaceshipTicket == true)
        {
            GameManager.instance.hasSpaceship = true;
        }

        return true;
    }
}
