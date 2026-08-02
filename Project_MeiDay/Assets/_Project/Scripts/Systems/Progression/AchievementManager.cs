using System;
using System.Collections.Generic;
using UnityEngine;

// 업적을 관리하는 매니저
// 게임 여기저기서 Check() 를 불러주면 조건 맞는 업적을 찾아서 달성 처리함
// 한 번 딴 업적은 저장돼서 새 게임을 해도 남아있음 (수집 요소니까)
public class AchievementManager : MonoBehaviour
{
    public List<string> unlockedIds = new List<string>(); // 이미 딴 업적 id 들 (저장됨)

    // 누적 기록 (새 게임을 해도 안 지워짐)
    public int totalComplaints = 0; // 처리한 민원 수
    public int totalArbeit = 0;     // 한 알바 수
    public int totalPerfectDays = 0; // 무실수로 끝낸 날 수

    // 자동 플레이(밸런스 시뮬) 도는 동안엔 켬
    // 안 그러면 20판 돌리는 버튼 한 번에 업적이 다 뚫리고 누적도 수백 배로 부풀어버림
    public bool skipRecord = false;

    // 업적 땄을 때 알려줌 (토스트 UI 가 듣고 띄움)
    public event Action<AchievementDefinition> OnUnlocked;

    // 아래 세 개는 기록을 늘리는 통로. 시뮬 중엔 여기서 다 막힘
    // (호출하는 쪽마다 시뮬인지 따지게 하면 새 호출부가 생길 때 또 새니까 여기서 한 번에 막음)
    public void AddComplaints(int amount)
    {
        if (skipRecord == true)
        {
            return;
        }

        totalComplaints += amount;
    }

    public void AddArbeit()
    {
        if (skipRecord == true)
        {
            return;
        }

        totalArbeit++;
    }

    public void AddPerfectDay()
    {
        if (skipRecord == true)
        {
            return;
        }

        totalPerfectDays++;
    }

    // 조건 맞는 게 있는지 전부 훑어봄 (하루 끝, 구매, 알바 끝 같은 데서 부름)
    public void Check()
    {
        if (skipRecord == true)
        {
            return; // 시뮬 중엔 업적 안 줌
        }
        if (GameManager.instance == null || GameManager.instance.content == null)
        {
            return;
        }

        List<AchievementDefinition> all = GameManager.instance.content.achievements;
        for (int i = 0; i < all.Count; i++)
        {
            AchievementDefinition ach = all[i];
            if (ach == null || HasUnlocked(ach.id) == true)
            {
                continue;
            }

            if (IsDone(ach) == true)
            {
                Unlock(ach);
            }
        }
    }

    // 이 업적 조건이 채워졌는지
    bool IsDone(AchievementDefinition ach)
    {
        GameManager gm = GameManager.instance;

        if (ach.condition == AchievementCondition.DayReached)
        {
            return gm.calendar.day >= ach.goal;
        }
        if (ach.condition == AchievementCondition.CreditsReached)
        {
            return gm.economy.credits >= ach.goal;
        }
        if (ach.condition == AchievementCondition.ComplaintsDone)
        {
            return totalComplaints >= ach.goal;
        }
        if (ach.condition == AchievementCondition.ArbeitDone)
        {
            return totalArbeit >= ach.goal;
        }
        if (ach.condition == AchievementCondition.ShopLevelSum)
        {
            int sum = gm.shop.GetShopLevel(ShopType.Bookstore)
                + gm.shop.GetShopLevel(ShopType.Theater)
                + gm.shop.GetShopLevel(ShopType.PartyShop);
            return sum >= ach.goal;
        }
        if (ach.condition == AchievementCondition.PerfectDays)
        {
            return totalPerfectDays >= ach.goal;
        }
        if (ach.condition == AchievementCondition.BoughtSpaceship)
        {
            return gm.hasSpaceship == true;
        }

        // 엔딩·게임오버는 phase 말고 기록값을 봄
        // phase 로 보면 나중에 컷신 끝나고 타이틀로 돌리는 코드가 생기는 순간 조용히 못 받게 됨
        if (ach.condition == AchievementCondition.EndingDatabase)
        {
            return gm.lastEnding == EndingType.DatabaseAccess;
        }
        if (ach.condition == AchievementCondition.EndingReport)
        {
            return gm.lastEnding == EndingType.ReportBroker;
        }
        if (ach.condition == AchievementCondition.GameOverAny)
        {
            return gm.phase == GamePhase.GameOver;
        }
        if (ach.condition == AchievementCondition.GameOverSuspicion)
        {
            return gm.phase == GamePhase.GameOver
                && gm.lastGameOverReason == GameOverReason.SuspicionExceeded;
        }
        if (ach.condition == AchievementCondition.GameOverRent)
        {
            return gm.phase == GamePhase.GameOver
                && gm.lastGameOverReason == GameOverReason.RentDefault;
        }
        if (ach.condition == AchievementCondition.ItemOwned)
        {
            if (ach.requiredItem == null)
            {
                return false; // 어떤 물건인지 안 적어둔 데이터 실수
            }
            return GameManager.instance.inventory.GetItemLevel(ach.requiredItem.id) > 0
                || GameManager.instance.inventory.HasItem(ach.requiredItem.id) == true;
        }

        return false; // 모르는 조건이면 안 줌
    }

    // 업적 주기
    void Unlock(AchievementDefinition ach)
    {
        unlockedIds.Add(ach.id);
        Debug.Log("업적 달성! " + ach.title);

        if (OnUnlocked != null)
        {
            OnUnlocked(ach);
        }

        Save();
    }

    // 이미 딴 업적인지
    public bool HasUnlocked(string id)
    {
        for (int i = 0; i < unlockedIds.Count; i++)
        {
            if (unlockedIds[i] == id)
            {
                return true;
            }
        }

        return false;
    }

    // 업적을 처음부터 다시 (디버그용 - 시뮬 돌려서 오염됐을 때 씀)
    public void ResetAll()
    {
        unlockedIds.Clear();
        totalComplaints = 0;
        totalArbeit = 0;
        totalPerfectDays = 0;
        Save();
        Debug.Log("업적 기록 초기화함");
    }

    // 업적은 회차랑 상관없이 남아야 해서 게임 저장이랑 따로 관리함
    public void Save()
    {
        if (skipRecord == true)
        {
            return; // 시뮬 기록을 진짜 저장에 쓰면 안 됨
        }

        PlayerPrefs.SetInt("achCount", unlockedIds.Count);
        for (int i = 0; i < unlockedIds.Count; i++)
        {
            PlayerPrefs.SetString("ach" + i, unlockedIds[i]);
        }

        PlayerPrefs.SetInt("achComplaints", totalComplaints);
        PlayerPrefs.SetInt("achArbeit", totalArbeit);
        PlayerPrefs.SetInt("achPerfect", totalPerfectDays);
        PlayerPrefs.Save();
    }

    // 게임 켜질 때 불러옴
    void Awake()
    {
        unlockedIds.Clear();
        int count = PlayerPrefs.GetInt("achCount", 0);
        for (int i = 0; i < count; i++)
        {
            string id = PlayerPrefs.GetString("ach" + i, "");

            // 빈 값이 들어오면 HasSeen("") 이 true 가 돼서
            // id 를 안 적어둔 새 업적이 조용히 달성 처리됨 - 그래서 걸러냄
            if (id.Length == 0 || HasUnlocked(id) == true)
            {
                continue;
            }

            unlockedIds.Add(id);
        }

        totalComplaints = PlayerPrefs.GetInt("achComplaints", 0);
        totalArbeit = PlayerPrefs.GetInt("achArbeit", 0);
        totalPerfectDays = PlayerPrefs.GetInt("achPerfect", 0);
    }
}
