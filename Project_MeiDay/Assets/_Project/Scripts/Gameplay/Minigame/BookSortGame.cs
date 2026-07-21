using System.Collections.Generic;
using UnityEngine;

// 서점 알바 - 책 정리 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 카트에서 나오는 책을 장르 맞는 칸에 꽂으면 됨
public class BookSortGame
{
    public int genreCount = 3;    // 장르(칸) 개수
    public int totalBooks = 6;    // 이번에 정리할 책 수
    public int sortedCount = 0;   // 맞게 꽂은 수
    public int missCount = 0;     // 잘못 꽂은 수
    public int currentGenre = -1; // 지금 카트에 나온 책 장르 (-1이면 끝)

    List<int> _queue = new List<int>(); // 아직 안 나온 책들

    // 게임 준비 (책 큐 만들어서 첫 책 꺼냄)
    public void StartGame(int books, int genres)
    {
        totalBooks = books;
        genreCount = genres;
        sortedCount = 0;
        missCount = 0;

        _queue.Clear();
        for (int i = 0; i < totalBooks; i++)
        {
            _queue.Add(Random.Range(0, genreCount));
        }

        NextBook();
    }

    // 다음 책 꺼내기 (없으면 -1로 끝 표시)
    void NextBook()
    {
        if (_queue.Count == 0)
        {
            currentGenre = -1;
            return;
        }

        currentGenre = _queue[0];
        _queue.RemoveAt(0);
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
            sortedCount++;
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

    // 수행률 (맞게 꽂은 비율)
    public float GetRate()
    {
        if (totalBooks == 0)
        {
            return 0;
        }
        return (float)sortedCount / totalBooks;
    }
}
