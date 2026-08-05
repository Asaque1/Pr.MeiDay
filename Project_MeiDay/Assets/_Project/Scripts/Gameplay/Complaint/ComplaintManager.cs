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
    public int mistakeCount = 0; // 오늘 해석이나 처리를 틀린 횟수

    // 지금 민원의 처리 방법을 틀린 횟수
    public int resolutionWrongCount = 0;

    public ComplaintStage stage = ComplaintStage.Translate; // 지금 민원이 해석 단계인지 처리 단계인지

    // 민원 상태 바뀌면 알려줌 (UI 가 듣고 다시 그림)
    public event Action OnComplaintUpdated;

    // 민원인이 화냈을 때 알려줌 (기획서의 "테이블 쿵 치며 화면 흔들림" 연출용)
    public event Action OnComplainantAngry;

    // 하루 시작 - 오늘 처리할 민원 뽑기 (이벤트에서 넘어온 민원 있으면 맨 앞에)
    public void StartDay(ComplaintDefinition eventComplaint)
    {
        currentIndex = 0;
        todayComplaints.Clear();
        successCount = 0;
        failCount = 0;
        earnedToday = 0;
        mistakeCount = 0;

        // 할당량 계산 (일차가 지날수록 늘어나고 상한이 있음)
        GameConfig config = GameManager.instance.config;
        int count = config.complaintsPerDay;
        if (config.complaintsGrowEvery > 0)
        {
            count += (GameManager.instance.calendar.day - 1) / config.complaintsGrowEvery;
        }
        if (count > config.complaintsMax)
        {
            count = config.complaintsMax;
        }

        // 파티 버프: 오늘은 민원인이 더 옴 (상한이랑 별개로 얹음)
        count += GameManager.instance.extraComplaintsToday;

        // 이벤트 민원이 오늘 첫 손님
        if (eventComplaint != null)
        {
            todayComplaints.Add(eventComplaint);
        }

        // 나머지는 민원 풀에서 랜덤으로 뽑음 (오늘 안에서는 안 겹치게)
        List<ComplaintDefinition> pool = new List<ComplaintDefinition>();
        List<ComplaintDefinition> all = GameManager.instance.content.complaints;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == eventComplaint)
            {
                continue; // 이벤트 민원이랑 겹치면 패스
            }
            if (all[i].eventOnly == true)
            {
                continue; // 이벤트 전용 민원은 랜덤으로 안 나옴
            }
            pool.Add(all[i]);
        }

        while (todayComplaints.Count < count && pool.Count > 0)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            todayComplaints.Add(pool[pick]);
            pool.RemoveAt(pick);
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
        resolutionWrongCount = 0;
        translation.StartComplaint(todayComplaints[currentIndex]);
        NotifyUpdated();
    }

    // 화면 흔들기 같은 연출한테 알려주기
    void NotifyAngry()
    {
        if (OnComplainantAngry != null)
        {
            OnComplainantAngry();
        }
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

        // 지금 받는 민원이 없으면 무시 (아침 대사 중에 눌리는 것 방지)
        if (currentIndex >= todayComplaints.Count)
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
        mistakeCount++;

        // 틀린 소리
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.wrongAnswer);
        }
        NotifyAngry(); // 화면 흔들기 연출

        // 의심도 초과로 게임오버 됐으면 여기서 끝 (집계 중복 방지)
        if (GameManager.instance.phase == GamePhase.GameOver)
        {
            return;
        }

        // 너무 많이 틀리면 민원인 나감 (실패로 치고 다음 민원. 그 민원 보수를 못 받으니 성과금이 깎이는 셈)
        if (translation.IsOverLimit() == true)
        {
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

        // 지금 받는 민원이 없으면 무시 (아침 대사 중에 눌리는 것 방지)
        if (currentIndex >= todayComplaints.Count)
        {
            return;
        }

        ComplaintDefinition c = todayComplaints[currentIndex];

        if (choice == c.correctResolution)
        {
            // 맞게 처리 -> 돈 받음 (파티 버프 있으면 % 만큼 더)
            int pay = c.reward;
            if (GameManager.instance.payoutBuffToday > 0)
            {
                pay += pay * GameManager.instance.payoutBuffToday / 100;
            }

            // 밀수템 부작용: 위조 신분증/번역서 쓰면 보수가 깎임
            int payCut = (int)GameManager.instance.inventory.GetSidePower(ItemEffect.ArbeitCountUp)
                + (int)GameManager.instance.inventory.GetSidePower(ItemEffect.FreeAnswerWord);
            if (payCut > 0)
            {
                pay -= pay * payCut / 100;
            }

            GameManager.instance.economy.AddCredits(pay);
            successCount++;
            earnedToday += pay;

            // 이 민원을 한 번도 안 틀렸으면 의심도 조금 내려감
            if (translation.wrongCount == 0 && resolutionWrongCount == 0)
            {
                GameManager.instance.suspicion.ReduceSuspicion(GameManager.instance.config.suspicionPerfectClear);
            }
        }
        else
        {
            // 틀리게 처리 -> 의심받고 같은 민원을 다시 처리함
            resolutionWrongCount++;
            mistakeCount++;

            // 틀린 소리
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx(SoundManager.instance.wrongAnswer);
            }
            NotifyAngry(); // 화면 흔들기 연출

            GameManager.instance.suspicion.AddSuspicion(GameManager.instance.config.suspicionWrongResolution);

            if (GameManager.instance.phase == GamePhase.GameOver)
            {
                return;
            }

            // 처리도 너무 많이 틀리면 민원인이 나감 (해석이랑 제한이 다름)
            if (resolutionWrongCount >= GameManager.instance.config.maxResolutionWrongTries)
            {
                failCount++;
                NextComplaint();
                return;
            }

            NotifyUpdated();
            return;
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
