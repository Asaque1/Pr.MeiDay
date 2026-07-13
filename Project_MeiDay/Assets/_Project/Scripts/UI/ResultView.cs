using TMPro;
using UnityEngine;

// 하루 결산 화면 (성공/실패/수입 보여줌)
public class ResultView : MonoBehaviour
{
    [SerializeField] TMP_Text summaryText; // 결산 내용

    // 패널 켜질 때 오늘 결과 채워줌
    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        ComplaintManager cm = GameManager.instance.complaint;
        EconomyManager economy = GameManager.instance.economy;

        string text = "성공: " + cm.successCount + "   실패: " + cm.failCount;
        text += "\n오늘 수입: +" + cm.earnedToday + " cr";

        if (cm.mistakeCount == 0 && cm.successCount > 0)
        {
            text += "\n무실수 보너스!";
        }

        text += "\n식비: -" + economy.foodPaidToday + " cr";

        if (economy.rentWasDueToday == true)
        {
            if (economy.rentMissedToday == true)
            {
                text += "\n월세: 미납 (" + economy.rentMissCount + " / "
                    + GameManager.instance.config.rentMissLimit + ")";
            }
            else
            {
                text += "\n월세: -" + economy.rentPaidToday + " cr";
            }
        }

        text += "\n남은 크레딧: " + economy.credits + " cr";

        summaryText.text = text;
    }
}
