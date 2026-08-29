using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 창 위에 붙는 탭 줄 (업적 / 설명서)
//
// 두 창은 크기랑 자리가 같아서, 한쪽을 켜면 다른 쪽이 닫히는 것만으로
// 탭을 갈아탄 것처럼 보임 (닫는 건 CloseOthersOnOpen 이 알아서 함)
// 그래서 여기서는 어느 탭이 켜져 있는지 색만 칠하고, 누르면 다른 창을 열어줌
public class WindowTabUI : MonoBehaviour
{
    [Header("창")]
    [SerializeField] GameObject otherWindow; // 이 탭을 누르면 열릴 창

    [Header("탭 두 개")]
    [SerializeField] Image myTab;        // 지금 보고 있는 창의 탭
    [SerializeField] Image otherTab;     // 넘어갈 창의 탭
    [SerializeField] TMP_Text myLabel;
    [SerializeField] TMP_Text otherLabel;

    [Header("색")]
    // 지금 보고 있는 탭이 진한 남색, 안 보는 쪽이 밝게
    [SerializeField] Color onColor = new Color(0.231f, 0.290f, 0.400f);  // 켜진 탭 (진하게)
    [SerializeField] Color offColor = new Color(0.78f, 0.81f, 0.88f);    // 안 켜진 탭 (밝게)
    [SerializeField] Color onTextColor = new Color(1f, 1f, 1f);
    [SerializeField] Color offTextColor = new Color(0.20f, 0.24f, 0.33f);

    void OnEnable()
    {
        Paint();
    }

    // 지금 창의 탭만 밝게
    void Paint()
    {
        if (myTab != null)
        {
            myTab.color = onColor;
        }
        if (otherTab != null)
        {
            otherTab.color = offColor;
        }
        if (myLabel != null)
        {
            myLabel.color = onTextColor;
        }
        if (otherLabel != null)
        {
            otherLabel.color = offTextColor;
        }
    }

    // 다른 탭 버튼 OnClick 에 연결
    // 저쪽 창을 켜면 CloseOthersOnOpen 이 이 창을 닫아줌
    public void OnClickOtherTab()
    {
        if (otherWindow == null)
        {
            return;
        }

        if (otherWindow.activeSelf == true)
        {
            return; // 이미 그 창임
        }

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.uiClick);
        }

        otherWindow.SetActive(true);
    }
}
