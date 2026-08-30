using TMPro;
using UnityEngine;

// 결산 화면에 한 줄씩 찍히는 프리팹 (TextPref 에 붙여서 씀)
public class ResultLineUI : MonoBehaviour
{
    [SerializeField] TMP_Text lineText;  // 줄 내용
    [SerializeField] GameObject checkMark; // 라디오 안쪽 채움 (좋은 줄에만 켜짐, 테두리는 늘 보임)

    // 지금 이 줄에 찍힌 글 (크레딧 줄을 번갈아 바꿀 때 씀)
    public TMP_Text Label { get { return lineText; } }

    // 결산 화면이 만들면서 채워줌
    public void Set(string text, bool good)
    {
        Set(text, good, 0f);
    }

    // size 가 0 보다 크면 글자 크기를 그걸로 바꿈 (중요한 줄을 크게 보여주려고)
    public void Set(string text, bool good, float size)
    {
        if (lineText != null)
        {
            lineText.text = text;

            if (size > 0f)
            {
                lineText.fontSize = size;
            }
        }

        // 보너스처럼 좋은 줄에만 체크가 켜짐 (안 꽂아뒀으면 그냥 넘어감)
        if (checkMark != null)
        {
            checkMark.SetActive(good);
        }
    }
}
