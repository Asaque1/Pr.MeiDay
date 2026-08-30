using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설명서 한 쪽 (제목 + 내용 + 그림)
[System.Serializable]
public class ManualPage
{
    public string title; // 쪽 제목 (예: 민원 해석하기)

    [TextArea(4, 8)]
    public string body;  // 설명 글

    public Sprite image; // 곁들일 그림 (없으면 글만 나옴)
}

// 게임 설명서 창 - 업적창처럼 버튼 눌러서 보고 싶을 때 봄
// 창 열고 닫는 건 QuickButton 이 하고, 여기는 쪽 넘기기만 함
// 내용은 인스펙터의 Pages 에서 채움 (쪽 추가/수정에 코드 안 건드려도 됨)
public class ManualView : MonoBehaviour
{
    [SerializeField] List<ManualPage> pages = new List<ManualPage>();

    [Header("창 안 연결")]
    [SerializeField] TMP_Text titleText;   // 쪽 제목
    [SerializeField] TMP_Text bodyText;    // 설명 글
    [SerializeField] Image pageImage;      // 곁들일 그림 (쪽에 그림 없으면 숨김)
    [SerializeField] TMP_Text pageLabel;   // "1 / 4" 표시
    [SerializeField] GameObject prevButton; // 첫 쪽에선 숨김
    [SerializeField] GameObject nextButton; // 마지막 쪽에선 숨김

    int _page = 0;

    void OnEnable()
    {
        // 창을 열 때마다 첫 쪽부터 (읽다 만 데서 이어지면 오히려 헷갈림)
        _page = 0;
        ShowPage();
    }

    // 이전 쪽 버튼 OnClick 에 연결
    public void OnClickPrev()
    {
        if (_page <= 0)
        {
            return;
        }
        _page--;
        ShowPage();

        // 뒤로 넘길 때도 앞으로 넘길 때랑 같은 소리를 냄 (한쪽만 조용하면 고장난 것처럼 들림)
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.pageTurn);
        }
    }

    // 다음 쪽 버튼 OnClick 에 연결
    public void OnClickNext()
    {
        if (_page >= pages.Count - 1)
        {
            return;
        }
        _page++;
        ShowPage();

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.pageTurn);
        }
    }

    // 지금 쪽 그리기
    void ShowPage()
    {
        if (pages.Count == 0)
        {
            if (titleText != null) { titleText.text = "설명서"; }
            if (bodyText != null) { bodyText.text = "아직 내용이 없어요."; }
            SetOn(prevButton, false);
            SetOn(nextButton, false);
            if (pageLabel != null) { pageLabel.text = ""; }
            return;
        }

        if (_page < 0) { _page = 0; }
        if (_page > pages.Count - 1) { _page = pages.Count - 1; }

        ManualPage page = pages[_page];

        if (titleText != null) { titleText.text = page.title; }
        if (bodyText != null) { bodyText.text = page.body; }
        if (pageLabel != null) { pageLabel.text = (_page + 1) + " / " + pages.Count; }

        // 그림 있는 쪽만 그림 칸을 보여줌
        if (pageImage != null)
        {
            pageImage.gameObject.SetActive(page.image != null);
            pageImage.sprite = page.image;
            pageImage.preserveAspect = true;
        }

        // 끝 쪽에선 그쪽 화살표를 숨김 (눌러도 안 되는 버튼이 보이면 답답함)
        SetOn(prevButton, _page > 0);
        SetOn(nextButton, _page < pages.Count - 1);
    }

    void SetOn(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on)
        {
            go.SetActive(on);
        }
    }
}
