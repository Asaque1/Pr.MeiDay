using TMPro;
using UnityEngine;

// 결산 화면에 한 줄씩 찍히는 프리팹 (TextPref 에 붙여서 씀)
public class ResultLineUI : MonoBehaviour
{
    [SerializeField] TMP_Text lineText;  // 줄 내용
    [SerializeField] GameObject checkMark; // 체크 표시 (있으면 좋은 줄에만 켬)

    // 결산 화면이 만들면서 채워줌
    public void Set(string text, bool good)
    {
        if (lineText != null)
        {
            lineText.text = text;
        }

        // 보너스처럼 좋은 줄에만 체크가 켜짐 (안 꽂아뒀으면 그냥 넘어감)
        if (checkMark != null)
        {
            checkMark.SetActive(good);
        }
    }
}
