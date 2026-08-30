using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 하루 민원 처리를 진행하는 매니저
// 오늘 민원들을 순서대로 하나씩 처리함
public class ComplaintManager : MonoBehaviour
{
    public TranslationController translation; // 해석 담당 (인스펙터에서 연결)

    [Header("민원인 오는 텀")]
    // 앞 손님이 끝나고 다음 손님이 앉기까지. 너무 길면 지루해지니 짧게 잡는 게 좋음
    [SerializeField] float arriveDelayMin = 0.7f;
    [SerializeField] float arriveDelayMax = 1.6f;

    [Header("앉고 말 걸기까지")]
    // 앉자마자 말하면 어색해서 살짝 뜸을 들임 (위보다 훨씬 짧게)
    [SerializeField] float speakDelayMin = 0.2f;
    [SerializeField] float speakDelayMax = 0.6f;

    // 지금 민원인이 어디까지 왔는지 (화면이 이걸 보고 카드를 잠갔다 풀었다 함)
    public ComplaintEntry entry = ComplaintEntry.Waiting;

    Coroutine _enterRoutine; // 손님 들이는 연출

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
            entry = ComplaintEntry.Waiting;
            NotifyUpdated();
            GameManager.instance.OnDayWorkDone();
            return;
        }

        stage = ComplaintStage.Translate;
        resolutionWrongCount = 0;
        translation.StartComplaint(todayComplaints[currentIndex]);

        // 문제는 미리 만들어두고, 손님은 텀을 두고 들어옴
        if (_enterRoutine != null)
        {
            StopCoroutine(_enterRoutine);
        }
        _enterRoutine = StartCoroutine(EnterComplaint());
    }

    // 손님이 창구로 오는 연출
    // 비어있음 -> (텀) 앉음 -> (짧은 텀) 말 시작
    // 말이 끝났다는 건 대사를 찍는 화면 쪽이 OnLineFinished 로 알려줌
    IEnumerator EnterComplaint()
    {
        entry = ComplaintEntry.Waiting;
        NotifyUpdated();

        yield return new WaitForSeconds(UnityEngine.Random.Range(arriveDelayMin, arriveDelayMax));

        entry = ComplaintEntry.Arrived;
        NotifyUpdated();

        yield return new WaitForSeconds(UnityEngine.Random.Range(speakDelayMin, speakDelayMax));

        entry = ComplaintEntry.Speaking;
        NotifyUpdated();

        _enterRoutine = null;
    }

    // 대사 다 나왔을 때 (대사를 찍는 화면이 불러줌)
    // 여기서부터 카드를 꽂을 수 있음
    public void OnLineFinished()
    {
        if (entry != ComplaintEntry.Speaking)
        {
            return; // 이미 지나갔거나 아직 말할 차례가 아님
        }

        entry = ComplaintEntry.Ready;
        NotifyUpdated();
    }

    // 지금 카드를 만질 수 있는 상태인지
    public bool CanUseCards()
    {
        return entry == ComplaintEntry.Ready && stage == ComplaintStage.Translate;
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

        // 아직 손님이 말하는 중이면 무시
        if (entry != ComplaintEntry.Ready)
        {
            return;
        }

        // 빈칸을 다 안 채웠으면 아예 제출이 안 됨
        // (확인 버튼도 다 채워야 나오지만, 키보드나 다른 데서 불릴 수도 있어서 여기서도 막음)
        if (translation.IsAllFilled() == false)
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

        AddTranslationMistake();
    }

    // 해석을 틀렸을 때 벌어지는 일
    // ConfirmTranslation 에서 떼어낸 것 - 밸런스 시뮬은 화면 없이 여기만 부름
    // (실제 플레이는 빈칸을 다 채워야 제출이 되지만, 시뮬은 결과 숫자만 필요해서 바로 부름)
    public void AddTranslationMistake()
    {
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
            // 나가기 전에 마지막 실수까지 화면에 반영해줌
            // 안 그러면 "해석 실수: 2 / 3" 인 채로 나가버려서 왜 나갔는지 알 수가 없음
            NotifyUpdated();

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
            // 서류 확인 도장 찍는 소리 (틀렸을 때 나는 찢는 소리랑 짝)
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx(SoundManager.instance.docAccept);
            }

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

            // 이 민원을 한 번도 안 틀렸으면 의심도가 조금 내려감
            // 단, 깎이는 양을 그날 할당량으로 나눔
            // 그냥 민원마다 같은 값을 깎아주면 할당량이 늘수록 하루 할인 총량이 같이 커져서
            // 후반이 오히려 안전해졌음 (16일차부터 한 번 틀려도 의심도가 내려감)
            // 나눠주면 하루에 깎을 수 있는 최대치는 늘 같고, 민원이 많아질수록 다 지키기만 어려워짐
            if (translation.wrongCount == 0 && resolutionWrongCount == 0)
            {
                int quota = (todayComplaints != null && todayComplaints.Count > 0) ? todayComplaints.Count : 1;
                GameManager.instance.suspicion.ReduceSuspicion(GameManager.instance.config.suspicionPerfectClear / quota);
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

        // 민원 하나 끝날 때마다 끼는 이벤트
        // 오늘 민원이 다 끝났으면 안 봄 - 그 자리는 하루 마무리 이벤트 몫
        if (currentIndex < todayComplaints.Count)
        {
            EventDefinition ev = GameManager.instance.events.PickEvent(EventSlot.AfterComplaint);
            if (ev != null)
            {
                // 이벤트 대사창은 반투명이라 뒤가 비침
                // 창구를 비워놓고 화면을 한 번 다시 그려야, 방금 나간 민원인이랑
                // 정답 채워진 빈칸이 대사 뒤에 그대로 앉아 있지 않음
                entry = ComplaintEntry.Waiting;
                stage = ComplaintStage.Translate;
                NotifyUpdated();

                GameManager.instance.events.StartEvent(ev);
                return; // 대사 끝나면 ContinueAfterEvent 가 이어감
            }
        }

        StartCurrent();
    }

    // 민원 사이 이벤트가 끝났을 때 (EventManager 가 불러줌)
    public void ContinueAfterEvent()
    {
        StartCurrent();
    }
}
