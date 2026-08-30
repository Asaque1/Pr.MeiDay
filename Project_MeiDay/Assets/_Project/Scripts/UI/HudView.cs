using TMPro;
using UnityEngine;

// 화면 위에 항상 떠 있는 정보 (날짜 / 크레딧 / 의심도)
public class HudView : MonoBehaviour
{
    [SerializeField] TMP_Text dayText;       // 날짜
    [SerializeField] TMP_Text creditsText;   // 크레딧
    [SerializeField] TMP_Text suspicionText; // 의심도
    [SerializeField] TMP_Text complaintText; // 오늘 민원 처리한 수

    // 민원 칩 알약 (셀 게 없을 때 글자랑 같이 접음)
    [SerializeField] GameObject complaintChip;

    void Update()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        // 게임이 실제로 도는 동안에만 띄움
        // 타이틀은 아직 의미가 없고, 엔딩·게임오버 화면에선 위에 겹쳐 떠서 지저분했음
        bool show = (gm.phase == GamePhase.DayWork
            || gm.phase == GamePhase.DayResult
            || gm.phase == GamePhase.NightShop);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf != show)
            {
                child.gameObject.SetActive(show);
            }
        }
        GetComponent<UnityEngine.UI.Image>().enabled = false; // 옛 반투명 띠는 안 씀
        if (show == false)
        {
            return;
        }

        dayText.text = gm.calendar.day + " Day";
        creditsText.text = gm.economy.credits + " cr";
        // 기획서상 의심도는 0~100 정수라서 소수점은 안 보여줌
        suspicionText.text = "의심도 " + Mathf.RoundToInt(gm.suspicion.current)
            + " / " + Mathf.RoundToInt(gm.suspicion.MaxSuspicion());

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

            // 아침 대사 중처럼 아직 셀 게 없으면 칸째로 접음 (빈 알약만 남으면 이상함)
            bool hasCount = (total > 0);
            if (complaintChip != null && complaintChip.activeSelf != hasCount)
            {
                complaintChip.SetActive(hasCount);
            }

            if (hasCount == false)
            {
                complaintText.text = "";
            }
            else
            {
                complaintText.text = now + "/" + total;
            }
        }
    }
}
