using System.Collections.Generic;
using UnityEngine;

// 단어 상자 한 칸 (문장에서 하나의 자리)
[System.Serializable]
public class ComplaintSlot
{
    public WordCategory category;      // 무슨 색 상자인지
    public WordDefinition correctWord; // 이 칸의 정답 단어 (후보를 안 적었을 때 씀)

    // 이 칸에 들어갈 수 있는 후보들
    // 하나 이상 적어두면 민원이 시작될 때마다 그 중에서 하나를 뽑아 정답으로 씀
    // 그래서 같은 민원이라도 매번 요구하는 서류가 달라짐 (외우는 게 아니라 익히게 하려고)
    // 안 뽑힌 후보들은 그대로 오답 카드로 쓰임 - 같은 종류라 그럴싸하게 헷갈림
    // 후보를 하나만 적으면 그게 고정 정답이 됨
    public List<WordDefinition> candidateWords = new List<WordDefinition>();

    // 이번 판에 쓸 정답 하나 고르기
    public WordDefinition PickAnswer()
    {
        return PickAnswer(null);
    }

    // taken: 같은 민원의 다른 칸이 이미 가져간 단어들
    // 한 문장에 같은 단어가 두 칸에 들어가면 방언 힌트까지 똑같이 떠서
    // 플레이어 눈에는 "왜 같은 게 두 개지?" 로 보임 - 되도록 겹치지 않게 뽑음
    public WordDefinition PickAnswer(List<WordDefinition> taken)
    {
        List<WordDefinition> pool = new List<WordDefinition>();
        for (int i = 0; i < candidateWords.Count; i++)
        {
            if (candidateWords[i] != null && pool.Contains(candidateWords[i]) == false)
            {
                pool.Add(candidateWords[i]);
            }
        }

        if (pool.Count == 0)
        {
            return correctWord; // 후보를 안 적었으면 예전처럼 고정 정답
        }

        // 아직 안 쓴 후보가 있으면 그 중에서
        if (taken != null)
        {
            List<WordDefinition> fresh = new List<WordDefinition>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (taken.Contains(pool[i]) == false)
                {
                    fresh.Add(pool[i]);
                }
            }

            // 후보가 모자라면 어쩔 수 없이 겹치게 둠 (칸 수보다 후보가 적은 민원)
            if (fresh.Count > 0)
            {
                return fresh[Random.Range(0, fresh.Count)];
            }
        }

        return pool[Random.Range(0, pool.Count)];
    }
}

// 민원 한 건의 정보
[CreateAssetMenu(fileName = "Complaint_", menuName = "MeiDay/Complaint")]
public class ComplaintDefinition : ScriptableObject
{
    public string id;
    public CharacterDefinition complainant; // 민원 넣은 외계인
    public bool eventOnly = false;          // 이벤트로만 나오는 민원 (아침 랜덤 뽑기에서 제외)

    [TextArea(2, 4)]
    public string complaintLine;            // 민원 대사

    // 1단계: 외계어 해석
    // 정답(또는 후보)은 slots 에 들어있음. 카드 깔기는 로직이 랜덤으로 함
    // wrongWords 는 더 넣고 싶은 오답이 있을 때만 적으면 됨
    // (안 적어도 slots 의 안 뽑힌 후보들이 오답으로 쓰임)
    public List<ComplaintSlot> slots = new List<ComplaintSlot>();
    public List<WordDefinition> wrongWords = new List<WordDefinition>();

    // 2단계: 처리
    public ComplaintResolution correctResolution; // 올바른 처리 방법

    public int reward = 50; // 잘 처리하면 주는 돈
}
