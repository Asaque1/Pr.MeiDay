using UnityEngine;

// 임시 디버그용 - 진짜 UI 만들기 전에 하루 루프 도는지 버튼으로 확인
// TODO: 진짜 UI 생기면 이거 지우기
public class DebugLoopDriver : MonoBehaviour
{
    void OnGUI()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(20, 20, 320, 540), GUI.skin.box);

        // 지금 상태 보여주기
        GUILayout.Label("페이즈: " + gm.phase);
        GUILayout.Label("날짜: " + gm.calendar.day + "일차");
        GUILayout.Label("크레딧: " + gm.economy.credits);
        GUILayout.Label("의심도: " + gm.suspicion.current + " / " + gm.config.maxSuspicion);
        GUILayout.Space(10);

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
            ComplaintManager cm = gm.complaint;
            if (cm.currentIndex < cm.todayComplaints.Count)
            {
                ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];
                GUILayout.Label("민원 " + (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count);

                if (GUILayout.Button("민원 정답 처리"))
                {
                    cm.SelectResolution(c.correctResolution);
                }
                if (GUILayout.Button("민원 오답 처리"))
                {
                    cm.SelectResolution(WrongOf(c.correctResolution));
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
            if (GUILayout.Button("자기 -> 다음날"))
            {
                gm.SleepAndAdvance();
            }
        }
        else if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
            GUILayout.Label(">>> " + gm.phase + " <<<");
            if (GUILayout.Button("타이틀로"))
            {
                gm.RestartToTitle();
            }
        }

        GUILayout.EndArea();
    }

    // 정답 아닌 처리 하나 골라줌 (오답 흉내내기)
    ComplaintResolution WrongOf(ComplaintResolution correct)
    {
        if (correct == ComplaintResolution.CallSecurity)
        {
            return ComplaintResolution.DirectHandle;
        }

        return ComplaintResolution.CallSecurity;
    }
}
