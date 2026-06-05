using UnityEngine;

// 돈(크레딧)이랑 지출을 관리하는 매니저
public class EconomyManager : MonoBehaviour
{
    public int credits = 100;     // 지금 가진 돈
    public int rentMissCount = 0; // 월세 몇 번 밀렸는지

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

    // 매일 밥값 내기
    public void PayFood()
    {
        credits -= GameManager.instance.config.foodCost;
    }

    // 월세 내기
    public void PayRent()
    {
        int rent = GameManager.instance.config.rentCost;

        if (credits >= rent)
        {
            credits -= rent;
            rentMissCount = 0; // 잘 냈으니 초기화
        }
        else
        {
            rentMissCount++; // 월세 밀림
            if (rentMissCount >= GameManager.instance.config.rentMissLimit)
            {
                // 3번 밀리면 쫓겨남 (게임오버)
                GameManager.instance.GameOver(GameOverReason.RentDefault);
            }
        }
    }
}
