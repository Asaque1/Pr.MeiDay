using UnityEngine;

// 날짜를 관리하는 매니저 (며칠째인지, 다음 날로 넘어가기)
public class CalendarManager : MonoBehaviour
{
    public int day = 1; // 며칠째인지

    // 다음 날로 넘어가기
    public void NextDay()
    {
        day++;

        // 다음 날 되면 밥값 나감
        GameManager.instance.economy.PayFood();

        // 정해진 날마다 월세 내는 날 (기본 6일)
        if (day % GameManager.instance.config.rentEveryDays == 0)
        {
            GameManager.instance.economy.PayRent();
        }

        // 마지막 날이면 엔딩
        if (day >= GameManager.instance.config.totalDays)
        {
            GameManager.instance.Ending(EndingType.NoShip);
        }
    }
}
