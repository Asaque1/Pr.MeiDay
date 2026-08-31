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
    public EventTiming timing;        // 어떻게 뽑히는지 (고정 / 랜덤 / 조건)
    public EventSlot slot;            // 하루 중 어느 자리에 끼는지 (기본은 아침)
    public int fixedDay = 1;          // FixedDay 면 며칠째인지

    [Range(0f, 1f)]
    public float randomChance = 0.3f; // Random 이면 아침에 나올 확률

    public EventCondition condition;  // Trigger 면 이 조건이 맞는 날 나옴
    public float conditionValue = 0;  // 조건에 쓰는 숫자 (며칠차, 크레딧 등)

    public List<EventLine> lines = new List<EventLine>(); // 대사들 (순서대로)

    // 선택지가 있는 대화는 여기에 꽂음 (꽂으면 위 lines 대신 이걸 씀)
    // 선택지 없는 그냥 대사는 lines 만 채우면 됨
    public DialogueData dialogue;

    public ComplaintDefinition linkedComplaint; // 대사 끝나고 이어지는 민원 (없으면 대사만 하고 끝)
    public float suspicionChange = 0;           // 이벤트 보고 나면 오르내리는 의심도 (음수면 내려감)
    public bool unlocksShop = false;            // 켜면 이 이벤트 보고 나서 밤 상점이 열림 (해금 이벤트용)

    // 이 이벤트가 끝나면 그 상점의 구매 탭을 열거나 잠금
    // 상점 화면 전체는 위 unlocksShop 이 열고, 이건 상점 하나의 물건 판매만 건드림
    // shopBuyAction 이 None 이면 shopToToggle 은 안 봄
    public ShopType shopToToggle = ShopType.Bookstore;
    public ShopBuyAction shopBuyAction = ShopBuyAction.None;
}
