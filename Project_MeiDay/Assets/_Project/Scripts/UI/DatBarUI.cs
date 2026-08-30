using UnityEngine;
using UnityEngine.UI;

// 결산 화면 오른쪽 "다음 월세까지" 표시
// 막대는 월세까지 남은 날, 아래 X 세 개는 밀린 횟수
//
// 예전엔 NextDay() 를 아무도 안 불러서 막대가 꽉 찬 채로 고정돼 있었음
// 이제 화면이 켜질 때마다 실제 값을 읽어서 그림
public class DayBar : MonoBehaviour
{
    public Image fillImage;

    [Tooltip("밀린 횟수만큼 켜지는 X 표시 (왼쪽부터)")]
    public GameObject[] missMarks;

    void OnEnable()
    {
        Refresh();
    }

    // 결산 화면이 매번 새로 켜지진 않아서 갱신도 돌려둠
    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        EconomyManager economy = GameManager.instance.economy;
        GameConfig config = GameManager.instance.config;
        int day = GameManager.instance.calendar.day;

        // 막대: 월세까지 남은 날 (다 차 있으면 아직 여유, 비면 오늘이 내는 날)
        // 나누는 값은 '이번 청구까지의 주기'라야 함 - 늘 6일로 나누면 막대가 영영 안 참
        // 첫 월세는 3일째고(그래서 첫 주기는 3일), 밀려서 독촉 중이면 3일 주기로 돌아옴
        if (fillImage != null)
        {
            int left = economy.nextRentDay - day;
            if (left < 0)
            {
                left = 0;
            }

            int span = config.rentEveryDays;
            if (economy.rentMissCount > 0)
            {
                span = config.rentDunEveryDays; // 독촉 중
            }
            else if (economy.nextRentDay <= config.firstRentDay)
            {
                span = config.firstRentDay;     // 아직 첫 월세 전
            }

            if (span > 0)
            {
                fillImage.fillAmount = Mathf.Clamp01((float)left / span);
            }
        }

        // X 표시: 밀린 횟수만큼 켬
        if (missMarks == null)
        {
            return;
        }

        for (int i = 0; i < missMarks.Length; i++)
        {
            if (missMarks[i] == null)
            {
                continue;
            }

            bool on = (i < economy.rentMissCount);
            if (missMarks[i].activeSelf != on)
            {
                missMarks[i].SetActive(on);
            }
        }
    }
}
