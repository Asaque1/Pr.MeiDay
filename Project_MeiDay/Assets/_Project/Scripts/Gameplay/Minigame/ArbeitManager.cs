using System.Collections.Generic;
using UnityEngine;

// 밤 아르바이트를 관리하는 매니저
// 횟수 차감 + 시작/종료 + 수행률에 따라 보상 정산
public class ArbeitManager : MonoBehaviour
{
    public int arbeitLeftToday = 0;          // 오늘 남은 알바 횟수
    public int arbeitMaxToday = 0;           // 오늘 받은 총 횟수 (화면에 "2/5" 로 보여줄 때 씀)
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
        arbeitMaxToday = arbeitLeftToday; // 오늘 몫은 여기까지
        currentArbeit = null;
        bookSort = null;
        spotlight = null;
        promo = null;
    }

    // 화면에 보여줄 오늘 총 횟수
    // 이월이나 아이템으로 상한 위로 붙는 경우가 있어서 config.arbeitMax 를 쓰면 "6/3" 같은 게 나옴
    // 저장에서 불러온 판은 총량이 안 남아 있어서 남은 횟수를 총량으로 봄
    public int MaxToday()
    {
        if (arbeitMaxToday < arbeitLeftToday)
        {
            return arbeitLeftToday;
        }
        return arbeitMaxToday;
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
            bookSort.StartGame(arbeit.bookCount, arbeit.genreCount, arbeit.playSeconds);
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

        // 알바마다 깔리는 곡이 따로 있음
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayArbeitBgm(arbeit.kind);
        }

        return true;
    }

    // 하다 만 알바 물리기 (화면을 닫거나 그만두기를 눌렀을 때)
    // 횟수를 돌려주지 않으면 하루치 알바가 통째로 날아가서 되돌려줌
    public void AbortArbeit()
    {
        if (currentArbeit == null)
        {
            return;
        }

        arbeitLeftToday++;
        currentArbeit = null;
        bookSort = null;
        spotlight = null;
        promo = null;

        BackToPhaseBgm();
    }

    // 수행률이 어느 구간인지 (기획서 계단: 맨 위 전부, 60% 이상 3/4, 40% 이상 절반, 그 밑은 실패)
    //
    // 맨 위 칸만 기획서의 80% 에서 88% 로 올림
    // 80 이면 보통 실력(70~85)도 그냥 만점이 나와서 "잘했다" 가 아니라 참가상이 됐음
    // 아래 두 칸(60/40)은 그대로 - 20초 꼬박 하고 0원이 되는 구간을 넓히면 의욕만 꺾임
    public const float TopRate = 0.88f;

    // 결과 화면에서도 같은 기준을 써야 해서 밖에서 부를 수 있게 빼둠
    public static float TierOf(float rate)
    {
        if (rate >= TopRate)
        {
            return 1f;
        }
        if (rate >= 0.6f)
        {
            return 0.75f;
        }
        if (rate >= 0.4f)
        {
            return 0.5f;
        }
        return 0f;
    }

    // 그 구간을 사람 말로 (결과 화면에 띄움)
    public static string TierName(float rate)
    {
        if (rate >= TopRate)
        {
            return "아주 잘함";
        }
        if (rate >= 0.6f)
        {
            return "괜찮음";
        }
        if (rate >= 0.4f)
        {
            return "그럭저럭";
        }
        return "실패";
    }

    // 알바 끝 - 수행률 구간에 따라 보수랑 상점 경험치를 받음
    public void FinishArbeit(float rate)
    {
        if (currentArbeit == null)
        {
            return;
        }

        float tier = TierOf(rate);

        int pay = Mathf.RoundToInt(currentArbeit.basePay * tier);
        int exp = Mathf.RoundToInt(currentArbeit.expReward * tier);

        arbeitDoneCounts[(int)currentArbeit.shopType]++; // 이 상점에서 일한 횟수 +1
        GameManager.instance.economy.AddCredits(pay);
        GameManager.instance.shop.AddShopExp(currentArbeit.shopType, exp);

        // 업적용 누적 기록
        // 업적은 게임 저장과 따로 관리하니까 여기서 같이 적어둠
        // (안 그러면 껐다 켤 때 밤에 한 알바 횟수가 되돌아감)
        if (GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.AddArbeit();
            GameManager.instance.achievements.Check();
            GameManager.instance.achievements.Save();
        }

        // 여기서 게임을 저장하면 안 됨
        // 저장은 '다음 날 아침' 상태만 담게 돼 있는데(취침 때 한 번),
        // 밤에 저장해버리면 오늘 아침으로 되돌아가는 파일이 만들어져서
        // 이어하기 하면 오늘 낮을 통째로 다시 하게 됨 (수입도 다시 들어옴)

        currentArbeit = null;
        bookSort = null;
        spotlight = null;
        promo = null;

        BackToPhaseBgm();
    }

    // 알바 판을 닫았으니 원래 밤 음악으로
    void BackToPhaseBgm()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.RefreshBgm();
        }
    }
}
