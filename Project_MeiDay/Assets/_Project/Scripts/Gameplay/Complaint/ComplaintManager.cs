using System.Collections.Generic;
using UnityEngine;

// 하루 민원 처리를 진행하는 매니저
// 오늘 민원들을 순서대로 하나씩 처리함
public class ComplaintManager : MonoBehaviour
{
    public TranslationController translation; // 해석 담당 (인스펙터에서 연결)

    public List<ComplaintDefinition> todayComplaints = new List<ComplaintDefinition>(); // 오늘 민원들
    public int currentIndex = 0; // 지금 몇 번째 민원인지

    // 오늘 결산용 집계 (하루 시작할 때 초기화)
    public int successCount = 0; // 잘 처리한 민원 수
    public int failCount = 0;    // 틀린 민원 수
    public int earnedToday = 0;  // 오늘 민원으로 번 크레딧

    // 하루 시작 - 오늘 처리할 민원 뽑기
    public void StartDay()
    {
        currentIndex = 0;
        todayComplaints.Clear();
        successCount = 0;
        failCount = 0;
        earnedToday = 0;

        // config 에 정한 개수만큼 앞에서부터 가져옴
        int count = GameManager.instance.config.complaintsPerDay;
        List<ComplaintDefinition> all = GameManager.instance.content.complaints;

        for (int i = 0; i < count && i < all.Count; i++)
        {
            todayComplaints.Add(all[i]);
        }

        StartCurrent();
    }

    // 지금 차례 민원 시작
    void StartCurrent()
    {
        // 민원 다 끝났으면 결산 화면으로
        if (currentIndex >= todayComplaints.Count)
        {
            GameManager.instance.OnDayWorkDone();
            return;
        }

        translation.StartComplaint(todayComplaints[currentIndex]);
    }

    // 플레이어가 처리 방법 골랐을 때
    public void SelectResolution(ComplaintResolution choice)
    {
        ComplaintDefinition c = todayComplaints[currentIndex];

        if (choice == c.correctResolution)
        {
            // 맞게 처리 -> 돈 받음
            GameManager.instance.economy.AddCredits(c.reward);
            successCount++;
            earnedToday += c.reward;
        }
        else
        {
            // 틀리게 처리 -> 의심받음
            GameManager.instance.suspicion.AddSuspicion(GameManager.instance.config.suspicionWrongResolution);
            failCount++;
        }

        NextComplaint();
    }

    // 다음 민원으로
    public void NextComplaint()
    {
        // 이미 게임오버면 더 안 넘김 (결산이 게임오버 덮어쓰는 거 방지)
        if (GameManager.instance.phase == GamePhase.GameOver)
        {
            return;
        }

        currentIndex++;
        StartCurrent();
    }
}
