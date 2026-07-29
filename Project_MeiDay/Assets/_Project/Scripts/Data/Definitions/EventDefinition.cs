using System.Collections.Generic;
using UnityEngine;

// 대사 한 줄 (누가 + 뭐라고)
[System.Serializable]
public class EventLine
{
    public CharacterDefinition speaker; // 말하는 사람

    [TextArea(2, 4)]
    public string text;                 // 대사 내용
}

// 아침 이벤트 한 개 정보 (대사 보여주고 민원으로 이어짐)
[CreateAssetMenu(fileName = "Event_", menuName = "MeiDay/Event")]
public class EventDefinition : ScriptableObject
{
    public string id;
    public EventTiming timing;        // 언제 나오는지
    public int fixedDay = 1;          // FixedDay 면 며칠째 아침인지

    [Range(0f, 1f)]
    public float randomChance = 0.3f; // Random 이면 아침에 나올 확률

    public List<EventLine> lines = new List<EventLine>(); // 대사들 (순서대로)

    public ComplaintDefinition linkedComplaint; // 대사 끝나고 이어지는 민원 (없으면 대사만 하고 끝)
    public float suspicionChange = 0;           // 이벤트 보고 나면 오르내리는 의심도 (음수면 내려감)
    public bool unlocksShop = false;            // 켜면 이 이벤트 보고 나서 밤 상점이 열림 (해금 이벤트용)
}
