using System;
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

    public ComplaintStage stage = ComplaintStage.Translate; // 지금 민원이 해석 단계인지 처리 단계인지

    // 민원 상태 바뀌면 알려줌 (UI 가 듣고 다시 그림)
    public event Action OnComplaintUpdated;

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

        stage = ComplaintStage.Translate;
        translation.StartComplaint(todayComplaints[currentIndex]);
        NotifyUpdated();
    }

    // 구독한 UI 한테 알려주기
    void NotifyUpdated()
    {
        if (OnComplaintUpdated != null)
        {
            OnComplaintUpdated();
        }
    }

    // 해석 "확인" 눌렀을 때
    public void ConfirmTranslation()
    {
        if (stage != ComplaintStage.Translate)
        {
            return;
        }

        // 다 맞으면 처리 단계로
        if (translation.CheckAnswer() == true)
        {
            stage = ComplaintStage.Resolve;
            NotifyUpdated();
            return;
        }

        // 틀리면 의심도 오르고 횟수 셈
        translation.AddWrong();

        // 의심도 초과로 게임오버 됐으면 여기서 끝 (벌금/집계 중복 방지)
        if (GameManager.instance.phase == GamePhase.GameOver)
        {
            return;
        }

        // 너무 많이 틀리면 민원인 나감 (벌금 + 실패로 치고 다음 민원)
        if (translation.IsOverLimit() == true)
        {
            GameManager.instance.economy.credits -= GameManager.instance.config.wrongAnswerFine;
            failCount++;
            NextComplaint();
            return;
        }

        NotifyUpdated(); // 틀린 횟수 바뀐 거 알려줌
    }

    // 플레이어가 처리 방법 골랐을 때
    public void SelectResolution(ComplaintResolution choice)
    {
        // 처리 단계일 때만
        if (stage != ComplaintStage.Resolve)
        {
            return;
        }

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
