using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 서점 알바 화면 - 위에 나온 책을 끌어내려 아래 카트의 맞는 칸에 담음
// 카트가 다 차면(위 책을 다 치우면) 다음 책들이 나옴
public class BookSortView : MonoBehaviour
{
    [Header("카트 칸")]
    [SerializeField] RectTransform[] shelves;  // 장르 순서대로 (0번 칸 = 장르 0)
    [SerializeField] TMP_Text[] shelfLabels;   // 칸 이름 (없어도 됨)

    [Header("카트")]
    [SerializeField] RectTransform cartArea;   // 책이 놓이는 자리
    [SerializeField] BookCardUI bookPrefab;    // 책 카드 하나 (꺼둔 채로 두면 됨)
    [SerializeField] RectTransform dragLayer;  // 끄는 동안 책이 올라앉을 곳 (없으면 이 화면)

    [Header("위/아래 줄")]
    [SerializeField] Image timerFill;
    [SerializeField] TMP_Text limitText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] TMP_Text countText;
    [SerializeField] TMP_Text guideText;

    [Header("모양")]
    [SerializeField] float cardGap = 12f; // 카트에 놓인 책 사이 간격
    [SerializeField] Color[] genreColors = new Color[] {
        new Color(0.85f, 0.35f, 0.35f),
        new Color(0.35f, 0.55f, 0.85f),
        new Color(0.40f, 0.75f, 0.45f),
        new Color(0.80f, 0.65f, 0.30f),
    };
    [SerializeField] Color goldenColor = new Color(0.98f, 0.80f, 0.28f);
    [SerializeField] float guideShowTime = 3f;

    ArbeitView _owner;
    List<BookCardUI> _cards = new List<BookCardUI>();
    float _elapsed = 0f;

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        _elapsed = 0f;
        gameObject.SetActive(true);

        if (cartArea == null || bookPrefab == null || shelves == null || shelves.Length == 0)
        {
            Debug.LogWarning("책 정리 화면에 책 놓는 자리나 카트 칸이 연결 안 됨");
        }

        BookSortGame game = GetGame();
        if (game != null && limitText != null)
        {
            limitText.text = "제한 시간: " + Mathf.RoundToInt(game.duration) + "초";
        }

        ShowShelfLabels(game);
        RebuildCards();

        if (guideText != null)
        {
            guideText.text = "위의 책을 아래 같은 색 카트 칸에 끌어다 놓으세요";
            guideText.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        ClearCards();
    }

    void Update()
    {
        BookSortGame game = GetGame();
        if (game == null || _owner == null)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        game.Tick(Time.deltaTime);
        RefreshTexts(game);

        if (guideText != null && _elapsed >= guideShowTime && guideText.gameObject.activeSelf == true)
        {
            guideText.gameObject.SetActive(false);
        }

        if (game.IsDone() == true)
        {
            Finish(game);
        }
    }

    // 끄는 동안 책이 올라앉을 곳
    public Transform DragLayer()
    {
        if (dragLayer != null)
        {
            return dragLayer;
        }
        return transform;
    }

    // 책을 놓았을 때 - 어느 카트 칸에 놓였는지 보고 처리 (놓은 데가 칸이면 true)
    public bool OnDropBook(BookCardUI card, UnityEngine.EventSystems.PointerEventData eventData)
    {
        BookSortGame game = GetGame();
        if (game == null || shelves == null)
        {
            return false;
        }

        int shelf = ShelfUnder(eventData);
        if (shelf < 0)
        {
            return false; // 엉뚱한 데 놓음 - 카드가 제자리로 돌아감
        }

        bool right = (card.cartIndex >= 0 && card.cartIndex < game.cartGenre.Count
            && game.cartGenre[card.cartIndex] == shelf);

        game.PlaceBook(card.cartIndex, shelf);

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(right ? SoundManager.instance.cardPlace : SoundManager.instance.wrongAnswer);
        }

        RebuildCards(); // 번호가 밀리니까 통째로 다시 그림
        return true;
    }

    // 지금 마우스가 어느 카트 칸 위인지 (-1이면 칸 밖)
    int ShelfUnder(UnityEngine.EventSystems.PointerEventData eventData)
    {
        for (int i = 0; i < shelves.Length; i++)
        {
            if (shelves[i] == null)
            {
                continue;
            }

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                shelves[i], eventData.position, eventData.pressEventCamera);
            if (inside == true)
            {
                return i;
            }
        }
        return -1;
    }

    // 카트에 실린 책들을 다시 그림
    void RebuildCards()
    {
        ClearCards();

        BookSortGame game = GetGame();
        if (game == null || cartArea == null || bookPrefab == null)
        {
            return;
        }

        int count = game.cartGenre.Count;
        if (count == 0)
        {
            return;
        }

        RectTransform pref = bookPrefab.transform as RectTransform;
        float cardWidth = pref.sizeDelta.x;
        float total = count * cardWidth + (count - 1) * cardGap;
        float startX = -total / 2f + cardWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            BookCardUI card = Instantiate(bookPrefab, cartArea);
            card.gameObject.SetActive(true);

            RectTransform rt = card.transform as RectTransform;
            rt.anchoredPosition = new Vector2(startX + i * (cardWidth + cardGap), 0f);

            int genre = game.cartGenre[i];
            bool golden = game.cartGolden[i];

            // 황금 책도 장르 색은 그대로 둠 (노랗게 칠해버리면 어느 칸에 꽂을지 알 수가 없음)
            // 대신 별을 붙이고 조금 크게 해서 티를 냄
            string text = golden ? ("★ " + GenreName(genre)) : GenreName(genre);
            card.Set(this, i, text, ColorOfGenre(genre), golden);

            _cards.Add(card);
        }
    }

    void ClearCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i] == null)
            {
                continue;
            }

            // Destroy 는 이 프레임 끝에야 실제로 지워짐
            // 그 사이에 새 카드가 만들어지면 한 프레임 겹쳐 보이니까 먼저 꺼둠
            _cards[i].gameObject.SetActive(false);
            Destroy(_cards[i].gameObject);
        }
        _cards.Clear();
    }

    // 카트 칸에 장르 이름/색 넣기
    void ShowShelfLabels(BookSortGame game)
    {
        if (shelves == null)
        {
            return;
        }

        int genres = (game != null) ? game.genreCount : shelves.Length;

        for (int i = 0; i < shelves.Length; i++)
        {
            if (shelves[i] == null)
            {
                continue;
            }

            // 이번 판에 안 쓰는 칸은 숨김 (장르 수가 판마다 다를 수 있음)
            bool use = (i < genres);
            if (shelves[i].gameObject.activeSelf != use)
            {
                shelves[i].gameObject.SetActive(use);
            }
            if (use == false)
            {
                continue;
            }

            Image img = shelves[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = ColorOfGenre(i);
            }
            if (shelfLabels != null && i < shelfLabels.Length && shelfLabels[i] != null)
            {
                shelfLabels[i].text = GenreName(i);
            }
        }
    }

    // 칸 라벨이랑 책 표지에 같이 쓰는 이름 (칸 색 순서랑 맞춰둠)
    static readonly string[] GenreNames = { "규정집", "항성도", "지구학" };

    string GenreName(int genre)
    {
        if (genre < 0 || genre >= GenreNames.Length)
        {
            return "장르 " + (genre + 1); // 칸 수를 늘리면 이름이 모자라니까
        }
        return GenreNames[genre];
    }

    Color ColorOfGenre(int genre)
    {
        if (genreColors == null || genreColors.Length == 0)
        {
            return Color.white;
        }
        return genreColors[genre % genreColors.Length];
    }

    void RefreshTexts(BookSortGame game)
    {
        if (timerFill != null && game.duration > 0f)
        {
            timerFill.fillAmount = Mathf.Clamp01(game.timeLeft / game.duration);
        }
        if (progressText != null)
        {
            // 황금 책을 틀린 칸에 넣으면 세는 수가 최대치를 넘어서 "23/22" 처럼 떴음
            int done = Mathf.Min(game.DoneCount(), game.totalBooks);
            progressText.text = done + "/" + game.totalBooks;
        }
        if (countText != null)
        {
            countText.text = "황금 " + game.goldenCount + "    실수 " + game.missCount;
        }
    }

    // 다 꽂았거나 시간이 다 됐으면 수행률 넘기고 화면 닫기
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

    // 그만두기 버튼 OnClick 에 연결 (알바 횟수는 돌려받음)
    public void OnClickQuit()
    {
        ArbeitView owner = _owner;
        _owner = null;

        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.ShowList();
        }
    }
}
