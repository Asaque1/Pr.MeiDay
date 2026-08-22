using TMPro;
using UnityEngine;

// 화면 위에 항상 떠 있는 정보 (날짜 / 크레딧 / 의심도)
public class HudView : MonoBehaviour
{
    [SerializeField] TMP_Text dayText;       // 날짜
    [SerializeField] TMP_Text creditsText;   // 크레딧
    [SerializeField] TMP_Text suspicionText; // 의심도
    [SerializeField] TMP_Text complaintText; // 오늘 민원 처리한 수

    void Update()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        dayText.text = gm.calendar.day + "일차";
        creditsText.text = gm.economy.credits + " cr";
        // 기획서상 의심도는 0~100 정수라서 소수점은 안 보여줌
        suspicionText.text = "의심도 " + Mathf.RoundToInt(gm.suspicion.current)
            + " / " + Mathf.RoundToInt(gm.config.maxSuspicion);

        // 지금 몇 번째 민원인지 (1/3 -> 2/3 식). 아직 안 꽂아뒀으면 그냥 건너뜀
        if (complaintText != null)
        {
            int total = gm.complaint.todayComplaints.Count;

            // 다 끝났으면 마지막 번호에 멈춤 (4/3 처럼 넘어가지 않게)
            int now = gm.complaint.currentIndex + 1;
            if (now > total)
            {
                now = total;
            }

            if (total <= 0)
            {
                complaintText.text = "";
            }
            else
            {
                complaintText.text = now + " / " + total;
            }
        }
    }
}
