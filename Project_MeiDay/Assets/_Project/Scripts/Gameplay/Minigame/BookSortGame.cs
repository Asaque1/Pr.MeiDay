using System.Collections.Generic;
using UnityEngine;

// 서점 알바 - 책 정리 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 카트에서 나오는 책을 장르 맞는 칸에 꽂으면 됨
public class BookSortGame
{
    public int genreCount = 3;    // 장르(칸) 개수
    public int totalBooks = 6;    // 이번에 정리할 책 수 (황금 책은 여기 안 셈)
    public int sortedCount = 0;   // 맞게 꽂은 수
    public int goldenCount = 0;   // 맞게 꽂은 황금 책 수 (보너스)
    public int missCount = 0;     // 잘못 꽂은 수
    public int currentGenre = -1; // 지금 카트에 나온 책 장르 (-1이면 끝)
    public bool currentGolden = false; // 지금 책이 황금 책인지
    public float goldenChance = 0.15f; // 황금 책이 껴서 들어올 확률

    List<int> _queue = new List<int>();          // 아직 안 나온 책들
    List<bool> _queueGolden = new List<bool>();  // 그 책이 황금인지

    // 게임 준비 (책 큐 만들어서 첫 책 꺼냄)
    public void StartGame(int books, int genres)
    {
        totalBooks = books;
        genreCount = genres;
        sortedCount = 0;
        goldenCount = 0;
        missCount = 0;

        _queue.Clear();
        _queueGolden.Clear();
        for (int i = 0; i < totalBooks; i++)
        {
            _queue.Add(Random.Range(0, genreCount));
            _queueGolden.Add(false);

            // 가끔 황금 책이 껴서 들어옴 (최대치엔 안 세는 보너스)
            if (Random.value < goldenChance)
            {
                _queue.Add(Random.Range(0, genreCount));
                _queueGolden.Add(true);
            }
        }

        NextBook();
    }

    // 다음 책 꺼내기 (없으면 -1로 끝 표시)
    void NextBook()
    {
        if (_queue.Count == 0)
        {
            currentGenre = -1;
            currentGolden = false;
            return;
        }

        currentGenre = _queue[0];
        currentGolden = _queueGolden[0];
        _queue.RemoveAt(0);
        _queueGolden.RemoveAt(0);
    }

    // 지금 책을 이 장르 칸에 꽂기
    public void PlaceBook(int shelfGenre)
    {
        if (currentGenre < 0)
        {
            return; // 이미 끝남
        }

        if (shelfGenre == currentGenre)
        {
            if (currentGolden == true)
            {
                goldenCount++; // 황금 책은 보너스로 침
            }
            else
            {
                sortedCount++;
            }
        }
        else
        {
            missCount++;
        }

        NextBook();
    }

    // 책 다 나왔는지
    public bool IsDone()
    {
        return currentGenre < 0;
    }

    // 수행률 (맞게 꽂은 비율. 황금 책은 순수 보너스라 넘칠 수 있음)
    public float GetRate()
    {
        if (totalBooks == 0)
        {
            return 0;
        }
        return Mathf.Clamp01((float)(sortedCount + goldenCount) / totalBooks);
    }
}
