using System.Collections.Generic;
using UnityEngine;

// 서점 알바 - 책 정리 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 책이 몇 권씩 나오고, 그 중 아무거나 집어서 장르 맞는 카트 칸에 담으면 됨
// 다 담으면 다음 책들이 나옴 (기획서: 처음과 카트가 비었을 때 책 3권)
public class BookSortGame
{
    public int genreCount = 3;  // 장르(카트 칸) 개수
    public int totalBooks = 9;  // 이번 판에 들어올 일반 책 수 (황금 책은 여기 안 셈)
    public int cartSize = 3;    // 카트에 한 번에 실리는 권수

    public int sortedCount = 0; // 맞게 꽂은 수
    public int goldenCount = 0; // 맞게 꽂은 황금 책 수 (보너스)
    public int missCount = 0;   // 잘못 꽂은 수

    // 지금 카트에 실려 있는 책들 (칸 번호로 집음)
    public List<int> cartGenre = new List<int>();
    public List<bool> cartGolden = new List<bool>();

    public float goldenPerGame = 1f; // 한 판에 나올 황금 책 기대 개수

    // 제한 시간 (시간 안에 다 꽂아야 함)
    public float timeLeft = 0f;
    public float duration = 20f;

    int _leftToLoad = 0; // 아직 카트에 안 실린 일반 책 수

    // 게임 준비 (첫 카트를 채움)
    public void StartGame(int books, int genres, float seconds)
    {
        totalBooks = books;
        genreCount = genres;
        if (genreCount < 1)
        {
            genreCount = 1;
        }

        sortedCount = 0;
        goldenCount = 0;
        missCount = 0;
        duration = seconds;
        timeLeft = seconds;

        _leftToLoad = totalBooks;
        cartGenre.Clear();
        cartGolden.Clear();

        Refill();
    }

    // 카트가 비었으면 다음 책들을 실어줌
    public void Refill()
    {
        if (cartGenre.Count > 0)
        {
            return; // 아직 카트에 책이 남아있음
        }

        for (int i = 0; i < cartSize && _leftToLoad > 0; i++)
        {
            cartGenre.Add(Random.Range(0, genreCount));
            cartGolden.Add(false);
            _leftToLoad--;

            // 가끔 황금 책이 같이 실려 옴 (최대치엔 안 세는 보너스)
            if (Random.value < GoldenChance())
            {
                cartGenre.Add(Random.Range(0, genreCount));
                cartGolden.Add(true);
            }
        }
    }

    // 책마다 한 번씩 굴리니까 책 수로 나눠야 한 판에 한 권쯤 나옴
    float GoldenChance()
    {
        if (totalBooks <= 0)
        {
            return 0f;
        }
        return goldenPerGame / totalBooks;
    }

    // 몇 번째 책을 이 장르 칸에 담기
    public void PlaceBook(int cartIndex, int shelfGenre)
    {
        if (cartIndex < 0 || cartIndex >= cartGenre.Count)
        {
            return;
        }

        bool right = (cartGenre[cartIndex] == shelfGenre);
        bool golden = cartGolden[cartIndex];

        if (right == true)
        {
            if (golden == true)
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

        // 맞든 틀리든 그 책은 카트에서 빠짐 (틀리면 그냥 못 꽂은 셈)
        cartGenre.RemoveAt(cartIndex);
        cartGolden.RemoveAt(cartIndex);

        Refill();
    }

    // 시간 흘리기 (화면이 매 프레임 불러줌)
    public void Tick(float dt)
    {
        if (timeLeft <= 0f)
        {
            return;
        }

        timeLeft -= dt;
        if (timeLeft < 0f)
        {
            timeLeft = 0f;
        }
    }

    // 책을 다 꽂았거나 시간이 다 됐는지
    public bool IsDone()
    {
        return (cartGenre.Count == 0 && _leftToLoad <= 0) || timeLeft <= 0f;
    }

    // 지금까지 처리한 책 수 (화면에 "3/9" 로 보여주는 용)
    public int DoneCount()
    {
        return sortedCount + missCount;
    }

    // 수행률 (맞게 꽂은 비율. 황금 책은 순수 보너스라 넘칠 수 있음)
    public float GetRate()
    {
        if (totalBooks == 0)
        {
            return 0f;
        }
        return Mathf.Clamp01((float)(sortedCount + goldenCount) / totalBooks);
    }
}
