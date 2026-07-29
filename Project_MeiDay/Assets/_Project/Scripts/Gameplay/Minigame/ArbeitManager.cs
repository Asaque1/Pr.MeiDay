using System.Collections.Generic;
using UnityEngine;

// 밤 아르바이트를 관리하는 매니저
// 횟수 차감 + 시작/종료 + 수행률에 따라 보상 정산
public class ArbeitManager : MonoBehaviour
{
    public int arbeitLeftToday = 0;          // 오늘 남은 알바 횟수
    public MinigameDefinition currentArbeit; // 지금 하는 알바 (없으면 null)
    public BookSortGame bookSort;            // 책 정리 하는 중이면 여기 들어있음 (아니면 null)
    public SpotlightGame spotlight;          // 조명 연출 하는 중이면 여기 들어있음 (아니면 null)
    public PromoGame promo;                  // 판촉 하는 중이면 여기 들어있음 (아니면 null)

    // 상점별 알바 누적 횟수 (서점, 연극단, 파티, 브로커 순. 통계용)
    public List<int> arbeitDoneCounts = new List<int>() { 0, 0, 0, 0 };

    // 아침마다 횟수 충전 (상점 해금돼야 생기고, 그 뒤론 아이템으로만 늘어남)
    public void ResetDaily()
    {
        GameConfig config = GameManager.instance.config;
        int count = config.arbeitStart;
        if (GameManager.instance.shopUnlocked == true)
        {
            count += 1; // 상점 해금 보너스
        }
        if (count > config.arbeitMax)
        {
            count = config.arbeitMax;
        }

        // 위조 신분증: 레벨만큼 알바 횟수 추가 (상한이랑 별개)
        int idBonus = (int)GameManager.instance.inventory.GetEffectPower(ItemEffect.ArbeitCountUp);
        count += idBonus;

        // 어제 안 쓴 횟수는 조금만 이월됨 (위조 신분증이 이월량도 늘려줌)
        int carryMax = config.arbeitCarryMax + idBonus;
        int carried = arbeitLeftToday;
        if (carried > carryMax)
        {
            carried = carryMax;
        }

        arbeitLeftToday = count + carried;
        currentArbeit = null;
        bookSort = null;
        spotlight = null;
        promo = null;
    }

    // 오늘 알바 더 할 수 있는지
    public bool CanWork()
    {
        return arbeitLeftToday > 0 && currentArbeit == null;
    }

    // 알바 시작 (횟수 차감)
    public bool StartArbeit(MinigameDefinition arbeit)
    {
        if (CanWork() == false)
        {
            return false;
        }

        arbeitLeftToday--;
        currentArbeit = arbeit;

        // 책 정리 알바면 게임 규칙 준비
        if (arbeit.kind == MinigameKind.BookSort)
        {
            bookSort = new BookSortGame();
            bookSort.StartGame(arbeit.bookCount, arbeit.genreCount);
        }

        // 조명 연출 알바면 게임 규칙 준비
        if (arbeit.kind == MinigameKind.Spotlight)
        {
            spotlight = new SpotlightGame();
            spotlight.StartGame(arbeit.playSeconds);
        }

        // 판촉 알바면 게임 규칙 준비
        if (arbeit.kind == MinigameKind.Promo)
        {
            promo = new PromoGame();
            promo.StartGame(arbeit.playSeconds);
        }

        return true;
    }

    // 알바 끝 - 수행률 구간에 따라 보수랑 상점 경험치를 받음
    public void FinishArbeit(float rate)
    {
        if (currentArbeit == null)
        {
            return;
        }

        // 기획서 계단: 80% 이상 전부, 60% 이상 3/4, 40% 이상 절반, 그 밑은 실패
        float tier = 0f;
        if (rate >= 0.8f)
        {
            tier = 1f;
        }
        else if (rate >= 0.6f)
        {
            tier = 0.75f;
        }
        else if (rate >= 0.4f)
        {
            tier = 0.5f;
        }

        int pay = Mathf.RoundToInt(currentArbeit.basePay * tier);
        int exp = Mathf.RoundToInt(currentArbeit.expReward * tier);

        arbeitDoneCounts[(int)currentArbeit.shopType]++; // 이 상점에서 일한 횟수 +1
        GameManager.instance.economy.AddCredits(pay);
        GameManager.instance.shop.AddShopExp(currentArbeit.shopType, exp);

        currentArbeit = null;
        bookSort = null;
        spotlight = null;
        promo = null;
    }
}
