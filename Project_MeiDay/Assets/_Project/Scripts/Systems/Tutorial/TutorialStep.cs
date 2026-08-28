using UnityEngine;

// 이 단계를 넘어가는 조건 (튜토리얼이 매 프레임 이것만 확인함)
public enum TutorialCheck
{
    CardPlaced,      // 빈칸에 카드를 하나라도 꽂음
    TranslationDone, // 해석을 맞춰서 처리 단계로 넘어감
    PdaOpened,       // PDA 를 엶
    PrintOpened,     // 서류 발급 화면을 엶
    ComplaintDone,   // 민원 한 건을 끝냄
    NextComplaint,   // 다음 민원인이 옴
    DayResult,       // 결산 화면까지 감
    AchieveOpened,   // 업적 창을 엶
}

// 튜토리얼 한 단계
[System.Serializable]
public class TutorialStep
{
    public string id; // 구분용 (화면엔 안 나옴)

    [TextArea(2, 3)]
    public string hint; // 화면에 뜨는 안내 한 줄

    public RectTransform target;   // 테두리로 가리킬 곳 (비우면 테두리 없이 안내만)
    public GameObject[] unlocks;   // 이 단계가 시작될 때 풀리는 것들
    public TutorialCheck doneWhen; // 뭘 하면 다음 단계로 넘어가는지

    // 켜두면 결산 화면에서만 테두리가 보임
    // (업적 단계처럼 결산에서 할 일인데, 민원 중에 미리 가리키면 헷갈림)
    public bool resultOnly = false;
}
