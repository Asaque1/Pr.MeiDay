using UnityEngine;

// 날짜를 관리하는 매니저 (며칠째인지, 다음 날로 넘어가기)
public class CalendarManager : MonoBehaviour
{
    public int day = 1; // 며칠째인지

    // 다음 날로 넘어가기
    public void NextDay()
    {
        GameManager gm = GameManager.instance;
        day++;

        // 하드모드면 하드모드 하루 진행 (탈출 엔딩 체크 포함)
        // 우주선 안 샀으면 정해진 끝 없이 계속 진행 (무한 모드)
        if (gm.hardMode == true)
        {
            gm.AdvanceHardMode();
        }
    }
}
