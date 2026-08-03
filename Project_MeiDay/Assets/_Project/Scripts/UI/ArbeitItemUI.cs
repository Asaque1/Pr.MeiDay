using TMPro;
using UnityEngine;

// 밤 알바 목록의 버튼 한 개
public class ArbeitItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameText; // 알바 이름
    [SerializeField] TMP_Text payText;  // 다 해내면 받는 보수랑 경험치

    ArbeitView _view;
    MinigameDefinition _arbeit;

    // 만들어질 때 view 가 채워줌
    public void Set(ArbeitView view, MinigameDefinition arbeit)
    {
        _view = view;
        _arbeit = arbeit;

        if (nameText != null)
        {
            nameText.text = arbeit.minigameName;
        }
        if (payText != null)
        {
            payText.text = arbeit.basePay + " cr / 경험치 " + arbeit.expReward;
        }
    }

    // 버튼 OnClick 에 연결
    public void OnClick()
    {
        _view.OnClickArbeit(_arbeit);
    }
}
