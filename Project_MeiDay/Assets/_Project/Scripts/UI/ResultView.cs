using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 하루 결산 화면 (성공/실패/수입 보여줌)
public class ResultView : MonoBehaviour
{
    [SerializeField] TMP_Text summaryText; // 통째로 보여줄 때 (아래 줄 프리팹을 안 쓰면 이거)

    // 줄 프리팹으로 한 줄씩 찍어서 보여줄 때 (TextPref 같은 거)
    [SerializeField] Transform lineParent;
    [SerializeField] ResultLineUI linePrefab;

    [Header("글자 크기 - 중요한 줄일수록 크게")]
    [SerializeField] float hugeSize = 28f;   // 크레딧
    [SerializeField] float bigSize = 24f;    // 성공/실패
    [SerializeField] float normalSize = 20f; // 보너스/실수
    [SerializeField] float smallSize = 16f;  // 식비/월세

    [Header("크레딧 줄 번갈아 보여주기")]
    [SerializeField] float creditSwapSeconds = 2.5f;

    List<ResultLineUI> _lines = new List<ResultLineUI>();
    int _creditLineIndex = -1;

    // 패널 켜질 때 오늘 결과 채워줌
    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        List<string> lines = new List<string>();
        List<bool> goods = new List<bool>();
        List<float> sizes = new List<float>();
        _creditLineIndex = -1;
        BuildLines(lines, goods, sizes);

        // 줄 프리팹을 꽂아뒀으면 한 줄씩, 아니면 예전처럼 한 덩어리로
        if (linePrefab != null && lineParent != null)
        {
            ShowAsLines(lines, goods, sizes);
            StartCoroutine(SwapCredits());
            return;
        }

        ShowAsOneText(lines);
    }

    void OnDisable()
    {
        StopAllCoroutines(); // 화면 닫히면 크레딧 번갈아 바꾸기도 멈춤
    }

    // 오늘 결산 내용을 줄 단위로 만들어줌 (good 은 좋은 소식인지, size 는 글자 크기)
    void BuildLines(List<string> lines, List<bool> goods, List<float> sizes)
    {
        ComplaintManager cm = GameManager.instance.complaint;
        EconomyManager economy = GameManager.instance.economy;
        GameConfig config = GameManager.instance.config;

        lines.Add("성공: " + cm.successCount + "   실패: " + cm.failCount);
        goods.Add(cm.failCount == 0);
        sizes.Add(bigSize);

        if (cm.mistakeCount == 0 && cm.successCount > 0)
        {
            // 얼마 받는지 안 적혀 있어서 뭘 받았는지 알 수가 없었음
            lines.Add("무실수 보너스: +" + config.perfectDayBonus + " cr");
            goods.Add(true);
            sizes.Add(normalSize);
        }
        else if (cm.mistakeCount > 0)
        {
            // 왜 무실수 보너스를 못 받았는지 보이게
            lines.Add("실수: " + cm.mistakeCount + "회");
            goods.Add(false);
            sizes.Add(normalSize);
        }

        lines.Add("식비: -" + economy.foodPaidToday + " cr");
        goods.Add(false);
        sizes.Add(smallSize);

        if (economy.rentWasDueToday == true)
        {
            if (economy.rentMissedToday == true)
            {
                lines.Add("월세: 미납 (" + economy.rentMissCount + " / " + config.rentMissLimit + ")");
                goods.Add(false);
            }
            else
            {
                lines.Add("월세: -" + economy.rentPaidToday + " cr");
                goods.Add(true);
            }
            sizes.Add(smallSize);
        }

        // 오늘 번 것과 남은 것을 한 줄에서 번갈아 보여줌 (자리를 아끼고 눈이 가게)
        _creditLineIndex = lines.Count;
        lines.Add(EarnedText());
        goods.Add(cm.earnedToday > 0);
        sizes.Add(hugeSize);
    }

    string EarnedText()
    {
        return "오늘 수입: +" + GameManager.instance.complaint.earnedToday + " cr";
    }

    string LeftText()
    {
        return "남은 크레딧: " + GameManager.instance.economy.credits + " cr";
    }

    // 크레딧 줄을 정해둔 간격으로 번갈아 바꿔줌
    System.Collections.IEnumerator SwapCredits()
    {
        bool showEarned = true;

        while (true)
        {
            // 결산 화면은 시간이 멈춘 데서도 열리니 unscaled 로 셈
            yield return new WaitForSecondsRealtime(creditSwapSeconds);

            if (_creditLineIndex < 0 || _creditLineIndex >= _lines.Count)
            {
                yield break;
            }

            ResultLineUI line = _lines[_creditLineIndex];
            if (line == null || line.Label == null)
            {
                yield break;
            }

            showEarned = (showEarned == false);
            line.Label.text = showEarned ? EarnedText() : LeftText();
        }
    }

    // 줄 프리팹을 하나씩 찍어서 보여주기
    void ShowAsLines(List<string> lines, List<bool> goods, List<float> sizes)
    {
        // 어제 찍어둔 줄 치우기
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
            {
                Destroy(_lines[i].gameObject);
            }
        }
        _lines.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            ResultLineUI line = Instantiate(linePrefab, lineParent);
            _lines.Add(line);
            float size = (i < sizes.Count) ? sizes[i] : 0f;
            line.Set(lines[i], goods[i], size);
        }

        // 줄로 보여주는 중이면 통짜 글자는 꺼둠
        // 글자만 비우면 같은 자리에 빈 줄로 남아서 아예 끔
        if (summaryText != null)
        {
            summaryText.gameObject.SetActive(false);
        }
    }

    // 예전처럼 한 덩어리로 보여주기
    void ShowAsOneText(List<string> lines)
    {
        if (summaryText == null)
        {
            return;
        }

        summaryText.gameObject.SetActive(true); // 줄 프리팹 쓰다가 꺼졌을 수도 있어서
        string text = "";
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                text += "\n";
            }
            text += lines[i];
        }

        summaryText.text = text;
    }
}
