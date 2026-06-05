using System.Collections.Generic;
using UnityEngine;

// 단어 상자 한 칸 (문장에서 하나의 자리)
[System.Serializable]
public class ComplaintSlot
{
    public WordCategory category;      // 무슨 색 상자인지
    public WordDefinition correctWord; // 이 칸의 정답 단어
}

// 민원 한 건의 정보
[CreateAssetMenu(fileName = "Complaint_", menuName = "MeiDay/Complaint")]
public class ComplaintDefinition : ScriptableObject
{
    public string id;
    public CharacterDefinition complainant; // 민원 넣은 외계인

    [TextArea(2, 4)]
    public string complaintLine;            // 민원 대사

    // 1단계: 외계어 해석 (맞춰야 하는 상자들 + 밑에 나오는 카드들)
    public List<ComplaintSlot> slots = new List<ComplaintSlot>();
    public List<WordDefinition> cardPool = new List<WordDefinition>();

    // 2단계: 처리
    public ComplaintResolution correctResolution; // 올바른 처리 방법
    public DirectHandleType directHandleType;     // 직접 처리면 뭘로 할지

    public int reward = 50; // 잘 처리하면 주는 돈
}
