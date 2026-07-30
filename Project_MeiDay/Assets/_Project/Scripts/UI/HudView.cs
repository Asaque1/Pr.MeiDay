using TMPro;
using UnityEngine;

// 화면 위에 항상 떠 있는 정보 (날짜 / 크레딧 / 의심도)
public class HudView : MonoBehaviour
{
    [SerializeField] TMP_Text dayText;       // 날짜
    [SerializeField] TMP_Text creditsText;   // 크레딧
    [SerializeField] TMP_Text suspicionText; // 의심도

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
    }
}
