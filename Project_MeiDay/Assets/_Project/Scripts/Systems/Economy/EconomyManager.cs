using UnityEngine;

// 돈(크레딧)이랑 지출을 관리하는 매니저
public class EconomyManager : MonoBehaviour
{
    public int credits = 100;     // 지금 가진 돈
    public int rentMissCount = 0; // 월세 몇 번 밀렸는지
    public int nextRentDay = 3;   // 다음 월세 내는 날 (게임 시작 때 맞춰줌)

    // 오늘 결산에 보여줄 지출 내역
    public int foodPaidToday = 0;
    public int rentPaidToday = 0;
    public bool rentWasDueToday = false;
    public bool rentMissedToday = false;

    // 돈 더하기
    public void AddCredits(int amount)
    {
        credits += amount;
    }

    // 돈 쓰기. 돈 충분하면 쓰고 true, 부족하면 false
    public bool SpendCredits(int amount)
    {
        if (credits < amount)
        {
            return false; // 돈 부족
        }
        credits -= amount;
        return true;
    }

    // 하루 업무가 끝났을 때 식비부터 내고, 월세 날이면 월세도 냄
    public void PayDailyBills(int day)
    {
        foodPaidToday = 0;
        rentPaidToday = 0;
        rentWasDueToday = false;
        rentMissedToday = false;

        PayFood();

        if (day >= nextRentDay)
        {
            rentWasDueToday = true;
            PayRent();
        }
    }

    // 매일 밥값 내기
    public void PayFood()
    {
        foodPaidToday = GameManager.instance.config.foodCost;
        credits -= foodPaidToday;
    }

    // 월세 내기 (독촉 방식)
    public void PayRent()
    {
        GameConfig config = GameManager.instance.config;
        int rent = config.rentCost;

        if (credits >= rent)
        {
            credits -= rent;
            rentPaidToday = rent;
            rentMissCount = 0;                   // 잘 냈으니 초기화
            nextRentDay += config.rentEveryDays; // 다음 정기 청구일로
        }
        else
        {
            rentMissedToday = true;
            rentMissCount++; // 월세 밀림
            if (rentMissCount >= config.rentMissLimit)
            {
                // 3번 밀리면 쫓겨남 (게임오버)
                GameManager.instance.GameOver(GameOverReason.RentDefault);
                return;
            }
            nextRentDay += config.rentDunEveryDays; // 며칠 뒤 다시 독촉
        }
    }
}
