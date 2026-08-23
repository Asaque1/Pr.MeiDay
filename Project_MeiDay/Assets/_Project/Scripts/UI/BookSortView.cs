using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 서점 알바 화면 - 카트에서 나온 책을 장르 칸에 넣음
// 장르 버튼은 인스펙터에 장르 수만큼 넣어두면 됨 (누르는 건 코드가 연결해줌)
public class BookSortView : MonoBehaviour
{
    [Header("장르")]
    [SerializeField] Button[] genreButtons; // 장르 칸 버튼들
    // 장르별 색 - 버튼이랑 책이 같은 색이라야 눈으로 짝지을 수 있음
    [SerializeField] Color[] genreColors = new Color[] {
        new Color(0.96f, 0.62f, 0.19f), // 주황
        new Color(0.16f, 0.60f, 0.93f), // 파랑
        new Color(0.27f, 0.70f, 0.60f), // 청록
    };

    [Header("책 더미")]
    // 맨 앞 책이 지금 꽂아야 할 책 (뒤엣것들은 다음 책이 쌓여있는 느낌만 냄)
    [SerializeField] Image frontBook;
    [SerializeField] Image[] backBooks;
    [SerializeField] TMP_Text bookText; // 책에 적히는 글 (황금 표시 등)

    [Header("위/아래 줄")]
    [SerializeField] Image timerFill;      // 남은 시간 막대 (Filled 이미지)
    [SerializeField] TMP_Text limitText;   // "제한 시간: N초"
    [SerializeField] TMP_Text progressText; // "0/6"
    [SerializeField] TMP_Text countText;   // 황금/실수 (안 꽂아둬도 됨)

    ArbeitView _owner;
    bool _wired = false; // 버튼 연결을 한 번만 하려고

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        gameObject.SetActive(true);

        BookSortGame game = GetGame();
        if (genreButtons == null || genreButtons.Length == 0)
        {
            // 버튼이 아예 없으면 책을 꽂을 방법이 없음
            Debug.LogWarning("책 정리 화면에 장르 버튼이 하나도 연결 안 됨");
            Finish(game);
            return;
        }
        if (game != null && genreButtons.Length < game.genreCount)
        {
            Debug.LogWarning("장르 버튼이 " + genreButtons.Length + "개뿐인데 장르는 "
                + game.genreCount + "개 - 못 꽂는 책이 생겨서 수행률이 안 나옴");
        }

        WireButtons();
        PaintButtons();
        Refresh();
    }

    void Update()
    {
        BookSortGame game = GetGame();
        if (game == null || _owner == null)
        {
            return;
        }

        game.Tick(Time.deltaTime);
        RefreshTimer(game);

        // 시간이 다 되면 거기까지 한 만큼으로 끝냄
        if (game.IsDone() == true)
        {
            Finish(game);
        }
    }

    // 그만두기 버튼 OnClick 에 연결 (알바 횟수는 돌려받음)
    public void OnClickQuit()
    {
        gameObject.SetActive(false);

        if (_owner != null)
        {
            _owner.ShowList();
        }
    }

    // 장르 버튼에 누르는 동작 붙이기
    void WireButtons()
    {
        if (_wired == true)
        {
            return;
        }
        _wired = true;

        for (int i = 0; i < genreButtons.Length; i++)
        {
            if (genreButtons[i] == null)
            {
                continue;
            }

            int index = i; // 그냥 i 를 쓰면 마지막 값이 들어가서 복사해둠
            genreButtons[i].onClick.AddListener(delegate { OnClickGenre(index); });
        }
    }

    // 버튼을 장르 색으로 칠하기 (한 번만 해두면 됨)
    void PaintButtons()
    {
        for (int i = 0; i < genreButtons.Length; i++)
        {
            if (genreButtons[i] == null)
            {
                continue;
            }

            Image im = genreButtons[i].GetComponent<Image>();
            if (im != null)
            {
                im.color = ColorOfGenre(i);
            }

            TMP_Text label = genreButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "장르 " + (i + 1);
            }
        }
    }

    // 장르 색 (넣어둔 것보다 장르가 많으면 회색)
    Color ColorOfGenre(int genre)
    {
        if (genreColors != null && genre >= 0 && genre < genreColors.Length)
        {
            return genreColors[genre];
        }

        return new Color(0.55f, 0.58f, 0.65f);
    }

    // 장르 칸을 눌렀을 때
    void OnClickGenre(int genre)
    {
        BookSortGame game = GetGame();
        if (game == null || game.IsDone() == true)
        {
            return;
        }

        game.PlaceBook(genre);

        if (game.IsDone() == true)
        {
            Finish(game);
            return;
        }

        Refresh();
    }

    // 남은 시간 막대만 갱신 (매 프레임)
    void RefreshTimer(BookSortGame game)
    {
        if (timerFill == null || game.duration <= 0f)
        {
            return;
        }

        timerFill.fillAmount = Mathf.Clamp01(game.timeLeft / game.duration);
    }

    // 지금 상태대로 화면 갱신
    void Refresh()
    {
        BookSortGame game = GetGame();
        if (game == null)
        {
            return;
        }

        // 장르 수보다 많은 버튼은 숨김
        for (int i = 0; i < genreButtons.Length; i++)
        {
            if (genreButtons[i] == null)
            {
                continue;
            }
            genreButtons[i].gameObject.SetActive(i < game.genreCount);
        }

        // 맨 앞 책을 지금 꽂아야 할 장르 색으로
        if (frontBook != null)
        {
            frontBook.color = ColorOfGenre(game.currentGenre);
        }

        // 뒤에 쌓인 책들은 다음 책이 있다는 느낌만 (색은 조금씩 어둡게)
        if (backBooks != null)
        {
            for (int i = 0; i < backBooks.Length; i++)
            {
                if (backBooks[i] == null)
                {
                    continue;
                }
                float dim = 0.75f - i * 0.12f;
                Color c = ColorOfGenre((game.currentGenre + i + 1) % Mathf.Max(1, game.genreCount));
                backBooks[i].color = new Color(c.r * dim, c.g * dim, c.b * dim, 1f);
            }
        }

        if (bookText != null)
        {
            if (game.currentGolden == true)
            {
                bookText.text = "황금 책";
            }
            else
            {
                bookText.text = "";
            }
        }

        if (limitText != null)
        {
            limitText.text = "제한 시간: " + Mathf.RoundToInt(game.duration) + "초";
        }
        if (progressText != null)
        {
            progressText.text = game.sortedCount + "/" + game.totalBooks;
        }
        if (countText != null)
        {
            countText.text = "황금 " + game.goldenCount + "    실수 " + game.missCount;
        }

        RefreshTimer(game);
    }

    // 다 끝났으면 수행률 넘기고 화면 닫기
    void Finish(BookSortGame game)
    {
        float rate = 0f;
        if (game != null)
        {
            rate = game.GetRate();
        }

        ArbeitView owner = _owner;
        _owner = null; // Update 가 한 번 더 부르지 않게 먼저 끊음

        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.OnGameFinished(rate);
        }
    }

    // 지금 도는 책 정리 규칙 (없으면 null)
    BookSortGame GetGame()
    {
        if (GameManager.instance == null)
        {
            return null;
        }

        return GameManager.instance.arbeit.bookSort;
    }
}
