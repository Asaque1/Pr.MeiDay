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

        // 우주선표는 한 번만 살 수 있음 (또 사면 돈만 날림)
        if (item.isSpaceshipTicket == true && GameManager.instance.hasSpaceship == true)
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

        // 1회성 효과 - 의심도 바로 내려감 (변장 용품 같은 거)
        if (item.suspicionReduce > 0)
        {
            GameManager.instance.suspicion.ReduceSuspicion(item.suspicionReduce);
        }

        // 우주선표면 탈출 가능 표시
        if (item.isSpaceshipTicket == true)
        {
            GameManager.instance.hasSpaceship = true;
        }

        return true;
    }
}
