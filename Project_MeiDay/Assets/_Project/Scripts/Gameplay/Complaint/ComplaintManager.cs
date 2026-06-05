using System.Collections.Generic;
using UnityEngine;

// 하루 민원 처리를 진행하는 매니저
// 오늘 민원들을 순서대로 하나씩 처리함
public class ComplaintManager : MonoBehaviour
{
    public TranslationController translation; // 해석 담당 (인스펙터에서 연결)

    public List<ComplaintDefinition> todayComplaints = new List<ComplaintDefinition>(); // 오늘 민원들
    public int currentIndex = 0; // 지금 몇 번째 민원인지

    // 하루 시작 - 오늘 처리할 민원 뽑기
    public void StartDay()
    {
        currentIndex = 0;
        todayComplaints.Clear();

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
        // 민원 다 끝났으면 밤(상점)으로
        if (currentIndex >= todayComplaints.Count)
        {
            GameManager.instance.ChangePhase(GamePhase.NightShop);
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
        }
        else
        {
            // 틀리게 처리 -> 의심받음
            GameManager.instance.suspicion.AddSuspicion(GameManager.instance.config.suspicionWrongResolution);
        }

        NextComplaint();
    }

    // 다음 민원으로
    public void NextComplaint()
    {
        currentIndex++;
        StartCurrent();
    }
}
