using UnityEngine;
using System.Collections.Generic;

// 상점 매니저 (아이템 사기 + 해금 확인 + 상점 레벨)
public class ShopManager : MonoBehaviour
{
    // 상점별 경험치 (서점, 연극단, 파티, 브로커 순. 저장됨)
    public List<int> shopExp = new List<int>() { 0, 0, 0, 0 };

    // 1회성/랜덤박스를 그동안 몇 번 샀는지 (값이 오르는 기준. 매일 조금씩 깎임)
    public int consumableBuyCount = 0;

    // 구매가 잠긴 상점들 (ShopType 을 숫자로 넣어둠. 이벤트가 열고 잠금. 저장됨)
    // 잠겨도 상점에 들어가는 것과 알바는 그대로 됨 - 물건만 못 삼
    public List<int> buyLockedShops = new List<int>();

    // 이 상점에서 지금 물건을 살 수 있는지
    public bool IsBuyLocked(ShopType type)
    {
        return buyLockedShops.Contains((int)type);
    }

    // 이벤트가 상점 구매를 열거나 잠글 때 부름
    public void SetBuyLocked(ShopType type, bool locked)
    {
        int key = (int)type;

        if (locked == true)
        {
            if (buyLockedShops.Contains(key) == false)
            {
                buyLockedShops.Add(key);
            }
        }
        else
        {
            buyLockedShops.Remove(key);
        }

        Debug.Log("상점 구매 " + (locked ? "잠금" : "해금") + ": " + ShopName(type));
    }

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

        // 돈 내보기 (영구템은 올릴 레벨에 따라 값이 다름)
        int price = PriceFor(item);
        bool paid = GameManager.instance.economy.SpendCredits(price);
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

            if (GameManager.instance.achievements != null)
            {
                GameManager.instance.achievements.Check(); // 우주선 구매 업적
            }

            // 브로커가 그 자리에서 바로 찾아옴 (대사 -> 선택창 -> 고른 대로 대사)
            GameManager.instance.OnSpaceshipBought();

            return true;
        }

        int boughtLevel = 1; // 1회성/랜덤박스는 레벨이 없어서 1레벨로 침

        if (item.kind == ItemKind.Permanent)
        {
            // 영구템은 살 때마다 레벨이 오름 (경험치는 방금 올린 그 레벨 기준)
            boughtLevel = GameManager.instance.inventory.GetItemLevel(item.id) + 1;
            GameManager.instance.inventory.LevelUpItem(item.id);
        }
        else
        {
            // 1회성이랑 랜덤박스는 사자마자 효과
            GameManager.instance.inventory.AddItem(item.id);
            consumableBuyCount++; // 살수록 값이 오르게 (매일 조금씩 깎임)
            ApplyInstantEffect(item, price);
        }

        // 사면 그 상점 경험치가 참 (높은 레벨을 살수록 많이 오름)
        AddShopExp(item.shopType, ShopExpForLevel(boughtLevel));

        // 상점 레벨로 주는 업적이 있어서 확인
        if (GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.Check();
        }

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

        // 이벤트로 구매가 잠긴 상점
        if (IsBuyLocked(item.shopType) == true)
        {
            return "지금은 안 팜";
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

        // 상점을 더 키워야 파는 물건 (브로커 물건도 다른 상점 레벨을 조건으로 걸 수 있음)
        if (item.requiredShopLevel > 0
            && GetShopLevel(item.requiredShopType) < item.requiredShopLevel)
        {
            return ShopName(item.requiredShopType) + " 레벨 " + item.requiredShopLevel + " 필요";
        }

        // 알바를 잘 해내야 풀리는 물건 (위조 신분증)
        if (item.requiredArbeitGood > 0)
        {
            int good = GameManager.instance.arbeit.arbeitGoodCount;
            if (good < item.requiredArbeitGood)
            {
                return "알바 잘함 " + good + " / " + item.requiredArbeitGood;
            }
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
        if (item.requiredShopLevel > 0
            && GetShopLevel(item.requiredShopType) < item.requiredShopLevel)
        {
            return false;
        }
        // 알바를 잘 해낸 횟수 조건 (위조 신분증)
        if (item.requiredArbeitGood > 0
            && GameManager.instance.arbeit.arbeitGoodCount < item.requiredArbeitGood)
        {
            return false;
        }
        // 알바를 잘 해내야 열리는 물건 (위조 신분증처럼) - 조건 전엔 목록에 아예 안 나옴
        if (item.requiredArbeitGood > 0
            && GameManager.instance.arbeit.arbeitGoodCount < item.requiredArbeitGood)
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
    // 상점 이름 (못 사는 이유 보여줄 때 씀)
    public string ShopName(ShopType type)
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
            return "사무용품점";
        }

        return "브로커";
    }

    // 이 물건을 지금 사면 내는 값
    // 영구템은 레벨을 올릴수록 비싸짐 (1회성/랜덤박스는 항상 같은 값)
    public int PriceFor(ItemDefinition item)
    {
        if (item == null)
        {
            return 0;
        }

        // 우주선표는 목표 금액이라 날짜나 구매 횟수에 안 흔들리게 값을 고정함
        if (item.isSpaceshipTicket == true)
        {
            return item.price;
        }

        if (item.kind != ItemKind.Permanent)
        {
            return ConsumablePrice(item);
        }

        int level = GameManager.instance.inventory.GetItemLevel(item.id);
        return PriceForLevel(item, level + 1); // 다음에 살 레벨 값
    }

    // 1회성 / 랜덤박스 값 (많이 살수록 % 로 비싸지고, 날이 갈수록도 조금씩 오름)
    public int ConsumablePrice(ItemDefinition item)
    {
        GameConfig config = GameManager.instance.config;

        int basePrice = item.price + config.consumablePriceStep;
        int marked = basePrice * (100 + consumableBuyCount * config.consumablePricePerBuy) / 100;

        return marked + config.consumablePricePerDay * GameManager.instance.calendar.day;
    }

    // 하루 지날 때 산 횟수가 조금 깎임 (몰아서 사든 나눠 사든 부담이 비슷하게)
    public void DecayConsumableBuys()
    {
        consumableBuyCount -= GameManager.instance.config.consumableBuyDecayPerDay;
        if (consumableBuyCount < 0)
        {
            consumableBuyCount = 0;
        }
    }

    // 이 아이템의 몇 레벨짜리 값 (1레벨은 아이템에 적힌 값 그대로)
    public int PriceForLevel(ItemDefinition item, int level)
    {
        if (level <= 1)
        {
            return item.price;
        }

        GameConfig config = GameManager.instance.config;

        // 설정값으로 만든 "기준 물건"의 값 곡선 (200 -> 275 -> 360 -> ...)
        // 이 곡선이 몇 배로 뛰는지만 가져와서 물건 제 값에 곱함
        //
        // 예전엔 곡선 값을 그대로 값으로 썼는데, 그러면 물건 값이 곡선 시작값보다 비쌀 때
        // 강화가 처음 사는 것보다 싸졌음 (360짜리 책갈피 2레벨이 307)
        float first = BaseCurve(config, 1);
        float now = BaseCurve(config, level);
        if (first <= 0f)
        {
            return item.price;
        }

        return Mathf.RoundToInt(item.price * (now / first));
    }

    // 설정값만으로 만든 기준 값 곡선 (물건 값이랑 무관)
    float BaseCurve(GameConfig config, int level)
    {
        float price = config.itemPriceBase;

        // 전 단계 값을 물고 올라가는 식이라 1레벨부터 차례로 계산함
        for (int step = 2; step <= level; step++)
        {
            float carry = 0f;
            if (config.itemPriceCarryDiv > 0)
            {
                carry = price * (step - 1) / config.itemPriceCarryDiv;
            }

            price = config.itemPriceBase
                + config.itemPriceStep
                + config.itemPricePerLevel * (step - 1)
                + carry;
        }

        return price;
    }

    // 이 레벨짜리를 샀을 때 받는 상점 경험치 (30 -> 40 -> 52 -> 67 -> 86)
    public int ShopExpForLevel(int level)
    {
        GameConfig config = GameManager.instance.config;
        int exp = config.shopExpFirstLevel;

        // 앞 레벨 값을 물고 올라가는 식이라 1레벨부터 차례로 계산함
        for (int step = 2; step <= level; step++)
        {
            exp = Mathf.FloorToInt(exp * config.shopExpGrowRate + config.shopExpGrowAdd);
        }

        return exp;
    }

    // 상점 경험치 주기 (구매나 알바로)
    // 레벨이 오르면 알려줌 - 알바로 번 경험치가 어디로 갔는지 안 보이면 판이 헛돌아 보임
    public event System.Action<ShopType, int> OnShopLevelUp;

    public void AddShopExp(ShopType type, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int before = GetShopLevel(type);
        shopExp[(int)type] += amount;
        int after = GetShopLevel(type);

        if (after > before && OnShopLevelUp != null)
        {
            OnShopLevelUp(type, after);
        }
    }

    // 지금 레벨에서 다음 레벨까지 얼마나 왔는지 (모은 것 / 필요한 것)
    // 만렙이면 둘 다 같은 값으로 돌려줘서 화면이 꽉 찬 걸로 그림
    public void GetLevelProgress(ShopType type, out int have, out int need)
    {
        GameConfig config = GameManager.instance.config;
        int left = shopExp[(int)type];
        int level = 0;

        while (level < 5)
        {
            int step = config.shopLevelExpBase + config.shopLevelExpStep * level;
            if (step <= 0 || left < step)
            {
                have = left;
                need = (step > 0) ? step : 1;
                return;
            }

            left -= step;
            level++;
        }

        // 만렙
        have = 1;
        need = 1;
    }

    // 상점 레벨 = 경험치로 계산 (0레벨 시작, 기획서상 최대 5레벨)
    // 레벨 하나 올리는 데 100 -> 150 -> 200 -> 250 -> 300 이 들어서 만렙까지 누적 1000
    public int GetShopLevel(ShopType type)
    {
        GameConfig config = GameManager.instance.config;
        int left = shopExp[(int)type];
        int level = 0;

        while (level < 5)
        {
            int need = config.shopLevelExpBase + config.shopLevelExpStep * level;
            if (need <= 0 || left < need)
            {
                break;
            }

            left -= need;
            level++;
        }

        return level;
    }

    // 1회성/랜덤박스 효과 바로 적용 (paid = 방금 낸 값)
    void ApplyInstantEffect(ItemDefinition item, int paid)
    {
        GameManager gm = GameManager.instance;

        if (item.kind == ItemKind.RandomBox)
        {
            // 낸 값의 0 ~ 1.8배 사이 (꽝일 수도 대박일 수도) + 아이템에 적힌 추가값
            // 돈 나오는 상자는 sidePower 로 위쪽 폭을 더 벌려서 도박성을 키움
            float top = paid * gm.config.randomBoxMaxRate + item.sidePower;
            int reward = Mathf.RoundToInt(Random.Range(0f, top)) + item.boxRewardMin;
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
