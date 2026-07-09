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

        // 다음 날 되면 밥값 먼저 나감
        gm.economy.PayFood();

        // 월세 청구일이면 월세 처리 (독촉 포함)
        if (day >= gm.economy.nextRentDay)
        {
            gm.economy.PayRent();
        }

        // 월세 못 내서 게임오버 됐으면 여기서 끝
        if (gm.phase == GamePhase.GameOver)
        {
            return;
        }

        // 하드모드면 하드모드 하루 진행 (탈출 엔딩 체크 포함)
        if (gm.hardMode == true)
        {
            gm.AdvanceHardMode();
            return;
        }

        // 우주선 없이 마지막 날까지 가면 무한플레이 엔딩(임시 종료)
        if (day >= gm.config.totalDays)
        {
            gm.Ending(EndingType.NoShip);
        }
    }
}
