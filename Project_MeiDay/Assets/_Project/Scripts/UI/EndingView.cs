using TMPro;
using UnityEngine;

// 엔딩 / 게임오버 화면
public class EndingView : MonoBehaviour
{
    [SerializeField] TMP_Text titleText; // 큰 제목
    [SerializeField] TMP_Text descText;  // 설명

    // 패널 켜질 때 어떻게 끝났는지 보여줌
    void OnEnable()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        if (gm.phase == GamePhase.GameOver)
        {
            titleText.text = "게임 오버";

            if (gm.lastGameOverReason == GameOverReason.SuspicionExceeded)
            {
                descText.text = "지구인인 게 들켰다...";
            }
            else
            {
                descText.text = "월세를 못 내서 쫓겨났다...";
            }
        }
        else
        {
            titleText.text = "엔딩";

            if (gm.lastEnding == EndingType.DatabaseAccess)
            {
                descText.text = "브로커에게 DB를 열어줬다. 찜찜하지만... 지구로 돌아간다!";
            }
            else
            {
                descText.text = "브로커를 신고하고도 끝까지 버텼다. 지구로 돌아간다!";
            }
        }
    }
}
