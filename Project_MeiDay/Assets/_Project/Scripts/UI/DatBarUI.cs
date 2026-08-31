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

    [Tooltip("\"다음 월세까지\" 글자. 남은 날짜를 여기에 같이 적어줌")]
    public TMPro.TMP_Text labelText;

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
                // 여기는 UiBar(폭 줄이기)를 못 씀
                // DayBar 오브젝트 자체 폭이 0이고 자식들이 고정 여백으로 자리를 잡고 있어서,
                // 비율 앵커로 바꾸면 폭이 0이 돼서 막대가 사라짐
                fillImage.fillAmount = Mathf.Clamp01((float)left / span);
            }
        }

        // 막대만 있으면 며칠 남았는지를 알 수가 없어서 숫자도 같이 적어줌
        if (labelText != null)
        {
            int left = economy.nextRentDay - day;
            if (left < 0)
            {
                left = 0;
            }

            // 값까지 붙이면 200 폭 라벨을 넘겨서 두 줄이 되고 막대를 가림
            // 얼마인지는 결산 종이 쪽에 한 줄로 따로 적어줌
            if (left <= 0)
            {
                labelText.text = "오늘이 월세 내는 날";
            }
            else
            {
                labelText.text = "다음 월세까지 " + left + "일";
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
