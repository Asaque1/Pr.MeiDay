using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 서점 알바 화면 - 카트에서 나온 책을 장르 칸에 넣음
// 장르 버튼은 인스펙터에 장르 수만큼 넣어두면 됨 (누르는 건 코드가 연결해줌)
public class BookSortView : MonoBehaviour
{
    [SerializeField] TMP_Text bookText;     // 지금 책 (장르 / 황금 표시)
    [SerializeField] TMP_Text countText;    // 정리 수 / 황금 / 실수
    [SerializeField] Button[] genreButtons; // 장르 칸 버튼들

    ArbeitView _owner;
    bool _wired = false; // 버튼 연결을 한 번만 하려고

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        gameObject.SetActive(true);

        // 버튼이 아예 없으면 책을 꽂을 방법이 없어서 영영 안 끝남 (책 정리엔 제한 시간이 없음)
        BookSortGame game = GetGame();
        if (genreButtons == null || genreButtons.Length == 0)
        {
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
        Refresh();
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

        if (bookText != null)
        {
            string mark = "";
            if (game.currentGolden == true)
            {
                mark = "   [황금]";
            }
            bookText.text = "이번 책: " + (game.currentGenre + 1) + "번 장르" + mark;
        }

        if (countText != null)
        {
            countText.text = "정리 " + game.sortedCount + " / " + game.totalBooks
                + "    황금 " + game.goldenCount
                + "    실수 " + game.missCount;
        }
    }

    // 다 끝났으면 수행률 넘기고 화면 닫기
    void Finish(BookSortGame game)
    {
        float rate = 0f;
        if (game != null)
        {
            rate = game.GetRate();
        }

        gameObject.SetActive(false);

        if (_owner != null)
        {
            _owner.OnGameFinished(rate);
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
