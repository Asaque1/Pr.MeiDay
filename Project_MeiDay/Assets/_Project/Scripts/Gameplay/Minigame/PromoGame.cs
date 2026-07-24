using System.Collections.Generic;
using UnityEngine;

// 파티용품점 알바 - 판촉 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 길에 지나다니는 행인들한테 타이밍 맞춰 홍보물을 던져서 쥐여줌. 황금 행인은 보너스
public class PromoGame
{
    public float timeLeft = 0f;  // 남은 시간
    public float duration = 20f; // 전체 시간

    public int handedCount = 0; // 잘 쥐여준 수
    public int goldenCount = 0; // 잘 쥐여준 황금 행인 수 (보너스, 최대치엔 안 셈)
    public int missCount = 0;   // 헛던진 수
    public int totalPeople = 0; // 등장한 일반 행인 수 (수행률의 최대치)

    public List<float> peoplePos = new List<float>();  // 행인들 위치 (0~1)
    public List<bool> peopleGolden = new List<bool>(); // 행인별 황금 여부

    public float personSpeed = 0.45f;  // 행인 걷는 속도
    public float goldenChance = 0.15f; // 황금 행인 확률
    public float throwMin = 0.35f;     // 던지면 닿는 구간
    public float throwMax = 0.65f;

    float _nextPersonIn = 0f; // 다음 행인 등장까지 남은 시간

    // 게임 준비
    public void StartGame(float seconds)
    {
        duration = seconds;
        timeLeft = seconds;
        handedCount = 0;
        goldenCount = 0;
        missCount = 0;
        totalPeople = 0;
        peoplePos.Clear();
        peopleGolden.Clear();
        _nextPersonIn = 0.5f;
    }

    // 한 프레임 진행
    public void Tick(float dt)
    {
        if (IsDone() == true)
        {
            return;
        }

        timeLeft -= dt;

        // 행인이 계속 나옴 (여러 명이 같이 걸어다닐 수 있음)
        _nextPersonIn -= dt;
        if (_nextPersonIn <= 0f)
        {
            bool golden = Random.value < goldenChance;
            peoplePos.Add(0f);
            peopleGolden.Add(golden);

            if (golden == false)
            {
                totalPeople++; // 황금 행인은 최대치에 안 셈
            }

            _nextPersonIn = Random.Range(0.8f, 1.6f);
        }

        // 다들 길을 걸어감. 끝까지 가면 못 준 것
        for (int i = peoplePos.Count - 1; i >= 0; i--)
        {
            peoplePos[i] += personSpeed * dt;
            if (peoplePos[i] > 1f)
            {
                peoplePos.RemoveAt(i);
                peopleGolden.RemoveAt(i);
            }
        }
    }

    // 홍보물 던지기 - 구간 안에 있는 행인한테 감 (여러 명이면 앞에 선 사람)
    public void Throw()
    {
        for (int i = 0; i < peoplePos.Count; i++)
        {
            if (peoplePos[i] >= throwMin && peoplePos[i] <= throwMax)
            {
                if (peopleGolden[i] == true)
                {
                    goldenCount++;
                }
                else
                {
                    handedCount++;
                }

                peoplePos.RemoveAt(i);
                peopleGolden.RemoveAt(i);
                return;
            }
        }

        missCount++; // 구간에 아무도 없었음
    }

    // 시간 다 됐는지
    public bool IsDone()
    {
        return timeLeft <= 0f;
    }

    // 수행률 (일반 행인 대비 쥐여준 비율. 황금은 순수 보너스라 넘칠 수 있음)
    public float GetRate()
    {
        if (totalPeople == 0)
        {
            return 0f;
        }
        return Mathf.Clamp01((handedCount + goldenCount) / (float)totalPeople);
    }
}
