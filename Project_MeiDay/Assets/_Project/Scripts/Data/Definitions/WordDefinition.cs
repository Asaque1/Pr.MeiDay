using UnityEngine;

// 단어 한 개 정보 (카드랑 상자에 쓰임)
[CreateAssetMenu(fileName = "Word_", menuName = "MeiDay/Word")]
public class WordDefinition : ScriptableObject
{
    public string id;             // 구분용 이름
    public WordCategory category; // 무슨 색 상자에 들어가는지

    public string meaning;        // 진짜 뜻 (카드에 보이는 글자, 예: 가족관계증명서)
    public string dialectText;    // 외계인이 말한 사투리 (상자에 보이는 글자)

    [TextArea(1, 3)]
    public string hint;           // 힌트 설명
}
