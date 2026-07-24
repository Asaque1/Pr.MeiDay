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

    // 상점별 알바 누적 횟수 (서점, 연극단, 파티, 브로커 순. 상점 레벨 계산에 씀)
    public List<int> arbeitDoneCounts = new List<int>() { 0, 0, 0, 0 };

    // 아침마다 횟수 충전 (날짜가 지날수록 늘어나고 상한 있음)
    public void ResetDaily()
    {
        GameConfig config = GameManager.instance.config;
        int count = config.arbeitStart;
        if (config.arbeitGrowEvery > 0)
        {
            count += (GameManager.instance.calendar.day - 1) / config.arbeitGrowEvery;
        }
        if (count > config.arbeitMax)
        {
            count = config.arbeitMax;
        }

        // 위조 신분증: 레벨만큼 알바 횟수 추가 (상한이랑 별개)
        count += (int)GameManager.instance.inventory.GetEffectPower(ItemEffect.ArbeitCountUp);

        arbeitLeftToday = count;
        currentArbeit = null;
        bookSort = null;
        spotlight = null;
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

        return true;
    }

    // 알바 끝 - 수행률(0~1)만큼 보수 받음 (가끔 보너스)
    public void FinishArbeit(float rate)
    {
        if (currentArbeit == null)
        {
            return;
        }

        int pay = Mathf.RoundToInt(currentArbeit.basePay * rate);

        // 낮은 확률로 보너스 터짐
        if (Random.value < currentArbeit.bonusChance)
        {
            pay = Mathf.RoundToInt(pay * currentArbeit.bonusMultiplier);
        }

        arbeitDoneCounts[(int)currentArbeit.shopType]++; // 이 상점에서 일한 횟수 +1
        GameManager.instance.economy.AddCredits(pay);
        currentArbeit = null;
        bookSort = null;
        spotlight = null;
    }
}
