using UnityEngine;
using System.Collections.Generic;

// 상점 매니저 (아이템 사기 + 해금 확인 + 상점 레벨)
public class ShopManager : MonoBehaviour
{
    // 상점별 경험치 (서점, 연극단, 파티, 브로커 순. 저장됨)
    public List<int> shopExp = new List<int>() { 0, 0, 0, 0 };

    // 아이템 사기. 성공하면 true, 돈 없거나 못 사는 물건이면 false
    public bool Buy(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        if (CanBuy(item) == false)
        {
            return false; // 품절이거나 아직 해금 안 됨
        }

        // 돈 내보기
        bool paid = GameManager.instance.economy.SpendCredits(item.price);
        if (paid == false)
        {
            return false; // 돈 부족해서 못 삼
        }

        // 구매 소리
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.buyItem);
        }

        // 우주선표는 탈출 표시만 하고 끝 (레벨이나 즉시효과, 경험치에 안 섞음)
        if (item.isSpaceshipTicket == true)
        {
            GameManager.instance.hasSpaceship = true;
            return true;
        }

        if (item.kind == ItemKind.Permanent)
        {
            // 영구템은 살 때마다 레벨이 오름
            GameManager.instance.inventory.LevelUpItem(item.id);
        }
        else
        {
            // 1회성이랑 랜덤박스는 사자마자 효과
            GameManager.instance.inventory.AddItem(item.id);
            ApplyInstantEffect(item);
        }

        // 사면 그 상점 경험치가 참
        AddShopExp(item.shopType, GameManager.instance.config.shopExpPerBuy);

        return true;
    }

    // 지금 이 물건을 살 수 있는 상태인지 (돈은 여기서 안 봄)
    public bool CanBuy(ItemDefinition item)
    {
        return BuyBlockReason(item).Length == 0;
    }

    // 못 사는 이유 (살 수 있으면 빈 글자)
    // 품절이랑 "아직 조건이 안 됨"을 구별해서 화면에 그대로 보여주려고 만듦
    public string BuyBlockReason(ItemDefinition item)
    {
        if (item == null)
        {
            return "물건 없음";
        }

        // 우주선표는 한 번만
        if (item.isSpaceshipTicket == true && GameManager.instance.hasSpaceship == true)
        {
            return "이미 삼";
        }

        // 영구템은 레벨 다 차면 진짜 품절
        if (item.kind == ItemKind.Permanent
            && GameManager.instance.inventory.GetItemLevel(item.id) >= item.maxLevel)
        {
            return "SOLD OUT";
        }

        // 우주선은 상점을 어느 정도 키워야 팔아줌
        if (item.isSpaceshipTicket == true)
        {
            int total = GetShopLevel(ShopType.Bookstore)
                + GetShopLevel(ShopType.Theater)
                + GetShopLevel(ShopType.PartyShop);

            if (total < GameManager.instance.config.spaceshipShopLevel)
            {
                return "상점 레벨 합 " + total
                    + " / " + GameManager.instance.config.spaceshipShopLevel;
            }
        }

        // 그 상점을 더 키워야 파는 물건
        if (item.shopType != ShopType.Broker
            && GetShopLevel(item.shopType) < item.requiredShopLevel)
        {
            return "상점 레벨 " + item.requiredShopLevel + " 필요";
        }

        // 다른 물건을 끝까지 사야 풀리는 물건
        if (item.requiresMaxed != null)
        {
            int level = GameManager.instance.inventory.GetItemLevel(item.requiresMaxed.id);
            if (level < item.requiresMaxed.maxLevel)
            {
                return item.requiresMaxed.itemName + " 만렙이어야 함";
            }
        }

        return "";
    }

    // 해금됐는지 (요구하는 영구템을 끝까지 샀는지)
    public bool IsUnlocked(ItemDefinition item)
    {
        if (item.shopType != ShopType.Broker && GetShopLevel(item.shopType) < item.requiredShopLevel)
        {
            return false;
        }
        if (item.requiresMaxed == null)
        {
            return true; // 조건 없는 물건
        }

        int level = GameManager.instance.inventory.GetItemLevel(item.requiresMaxed.id);
        return level >= item.requiresMaxed.maxLevel;
    }
    // 상점 경험치 주기 (구매나 알바로)
    public void AddShopExp(ShopType type, int amount)
    {
        shopExp[(int)type] += amount;
    }

    // 상점 레벨 = 경험치로 계산 (기획서상 최대 5레벨)
    public int GetShopLevel(ShopType type)
    {
        int perLevel = GameManager.instance.config.shopExpPerLevel;
        if (perLevel <= 0)
        {
            return 0;
        }

        int level = shopExp[(int)type] / perLevel;
        if (level > 5)
        {
            level = 5;
        }
        return level;
    }

    // 1회성/랜덤박스 효과 바로 적용
    void ApplyInstantEffect(ItemDefinition item)
    {
        GameManager gm = GameManager.instance;

        if (item.kind == ItemKind.RandomBox)
        {
            // 범위 내 랜덤 크레딧 (본전일 수도, 대박일 수도)
            int reward = Random.Range(item.boxRewardMin, item.boxRewardMax + 1);
            gm.economy.AddCredits(reward);
            return;
        }

        if (item.effect == ItemEffect.ReduceSuspicion)
        {
            gm.suspicion.ReduceSuspicion(item.power);
        }
        else if (item.effect == ItemEffect.PayoutBuff)
        {
            gm.pendingPayoutBuff += (int)item.power; // 내일 성과금 +%
        }
        else if (item.effect == ItemEffect.ExtraComplaints)
        {
            gm.pendingExtraComplaints += (int)item.power; // 내일 민원인 추가
        }
        else
        {
            // 1회성인데 즉시 효과가 아닌 게 적혀있으면 데이터 실수 (돈만 나감)
            Debug.LogWarning("1회성 아이템에 안 맞는 효과가 붙어있음: " + item.id);
        }
    }
}
