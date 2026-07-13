using UnityEngine;

// 임시 디버그용 - 진짜 UI 만들기 전에 하루 루프 도는지 버튼으로 확인
// TODO: 진짜 UI 생기면 이거 지우기
public class DebugLoopDriver : MonoBehaviour
{
    int _selectedCard = 0; // 지금 고른 카드 번호

    void OnGUI()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(20, 20, 340, 700), GUI.skin.box);

        // 지금 상태 보여주기
        GUILayout.Label("페이즈: " + gm.phase);
        GUILayout.Label("날짜: " + gm.calendar.day + "일차");
        GUILayout.Label("크레딧: " + gm.economy.credits);
        GUILayout.Label("의심도: " + gm.suspicion.current + " / " + gm.config.maxSuspicion);
        GUILayout.Space(10);

        // 하드모드면 진행 상황 + 아직 답 안 했으면 브로커 선택창 먼저
        if (gm.hardMode == true)
        {
            GUILayout.Label("[하드모드] " + gm.hardModeDay + " / " + gm.config.hardModeDays + "일");
            if (gm.brokerChoice == BrokerChoice.None)
            {
                GUILayout.Label("브로커: 자, 이제 어떻게 할 거야?");
                if (GUILayout.Button("DB 접근시키기 (돈 뜯김)"))
                {
                    gm.ChooseBroker(BrokerChoice.DatabaseAccess);
                }
                if (GUILayout.Button("신고하기 (매일 의심도 증가)"))
                {
                    gm.ChooseBroker(BrokerChoice.ReportBroker);
                }
            }
            GUILayout.Space(10);
        }

        // 페이즈마다 버튼 다르게 보여줌
        if (gm.phase == GamePhase.Intro)
        {
            if (GUILayout.Button("게임 시작"))
            {
                gm.GameStart();
            }
        }
        else if (gm.phase == GamePhase.DayWork)
        {
            // 아침 이벤트 대사 나오는 중이면 그것부터
            if (gm.events.currentEvent != null)
            {
                DrawDialogue(gm.events);
            }
            else
            {
                ComplaintManager cm = gm.complaint;
                if (cm.currentIndex < cm.todayComplaints.Count)
                {
                    ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];
                    GUILayout.Label("민원 " + (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count);

                    // 해석 단계면 카드 맞추기, 처리 단계면 처리 고르기
                    if (cm.stage == ComplaintStage.Translate)
                    {
                        DrawTranslate(cm.translation, cm);
                    }
                    else
                    {
                        DrawResolve(cm, c);
                    }
                }
            }
        }
        else if (gm.phase == GamePhase.DayResult)
        {
            ComplaintManager cm = gm.complaint;
            GUILayout.Label("- 오늘 결산 -");
            GUILayout.Label("성공: " + cm.successCount + "  실패: " + cm.failCount);
            GUILayout.Label("오늘 수입: " + cm.earnedToday);
            if (cm.failCount == 0 && cm.successCount > 0)
            {
                GUILayout.Label("무실수 보너스!");
            }

            if (GUILayout.Button("결산 확인 -> 밤으로"))
            {
                gm.ConfirmDayResult();
            }
        }
        else if (gm.phase == GamePhase.NightShop)
        {
            GUILayout.Label("우주선 가격: " + gm.config.spaceshipPrice);
            if (gm.hasSpaceship == false)
            {
                if (GUILayout.Button("우주선 구매"))
                {
                    gm.TryBuySpaceship();
                }
            }
            else
            {
                GUILayout.Label("우주선 보유중 (자면 하드모드 시작)");
            }

            if (GUILayout.Button("자기 -> 다음날"))
            {
                gm.SleepAndAdvance();
            }
        }
        else if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
            GUILayout.Label(">>> " + gm.phase + " <<<");
            if (gm.phase == GamePhase.Ending)
            {
                GUILayout.Label("엔딩: " + gm.lastEnding);
            }
            else
            {
                GUILayout.Label("이유: " + gm.lastGameOverReason);
            }
            if (GUILayout.Button("타이틀로"))
            {
                gm.RestartToTitle();
            }
        }

        GUILayout.EndArea();
    }

    // 아침 이벤트 대사 디버그 UI
    void DrawDialogue(EventManager em)
    {
        EventLine line = em.CurrentLine();
        if (line == null)
        {
            return;
        }

        string who = "???";
        if (line.speaker != null)
        {
            who = line.speaker.displayName;
        }

        GUILayout.Label("[이벤트] " + em.currentEvent.id + " (" + (em.lineIndex + 1) + " / " + em.currentEvent.lines.Count + ")");
        GUILayout.Label(who + ": " + line.text);

        if (GUILayout.Button("다음 대사"))
        {
            em.AdvanceLine();
        }
    }

    // 해석 단계 디버그 UI (상자 + 카드)
    void DrawTranslate(TranslationController tc, ComplaintManager cm)
    {
        GUILayout.Label("[해석 단계]  틀린 횟수: " + tc.wrongCount);

        // 상자들
        for (int i = 0; i < tc.slots.Count; i++)
        {
            WordSlot s = tc.slots[i];
            string placed = "( 빈칸 )";
            if (s.placedWord != null)
            {
                placed = s.placedWord.meaning;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(i + "번 [" + s.category + "] : " + placed);
            if (GUILayout.Button("비우기", GUILayout.Width(55)))
            {
                tc.ClearSlot(i);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(4);
        GUILayout.Label("고른 카드: " + _selectedCard);

        // 카드들 (누르면 고름)
        for (int i = 0; i < tc.cards.Count; i++)
        {
            WordCard card = tc.cards[i];
            string mark = "";
            if (card.isUsed == true)
            {
                mark = " (놓음)";
            }
            if (GUILayout.Button(i + " : " + card.word.meaning + mark))
            {
                _selectedCard = i;
            }
        }

        // 고른 카드를 몇 번 상자에 넣을지
        GUILayout.Label("아래 눌러서 상자에 넣기:");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < tc.slots.Count; i++)
        {
            if (GUILayout.Button("-> " + i, GUILayout.Width(45)))
            {
                tc.PlaceCard(_selectedCard, i);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        if (GUILayout.Button("확인 (해석 제출)"))
        {
            cm.ConfirmTranslation();
        }
    }

    // 처리 단계 디버그 UI
    void DrawResolve(ComplaintManager cm, ComplaintDefinition c)
    {
        GUILayout.Label("[처리 단계]");
        if (GUILayout.Button("정답 처리"))
        {
            cm.SelectResolution(c.correctResolution);
        }
        if (GUILayout.Button("오답 처리"))
        {
            cm.SelectResolution(WrongOf(c.correctResolution));
        }
    }

    // 정답 아닌 처리 하나 골라줌 (오답 흉내내기)
    ComplaintResolution WrongOf(ComplaintResolution correct)
    {
        if (correct == ComplaintResolution.CallSecurity)
        {
            return ComplaintResolution.DocumentPrint;
        }

        return ComplaintResolution.CallSecurity;
    }
}
