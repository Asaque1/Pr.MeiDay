using System.Collections.Generic;
using UnityEngine;

// 게임에 들어가는 모든 데이터를 모아놓는 곳
// (여기에 만든 민원, 단어, 아이템들을 다 넣어두고 GameManager 에서 꺼내 씀)
[CreateAssetMenu(fileName = "ContentDatabase", menuName = "MeiDay/Content Database")]
public class ContentDatabase : ScriptableObject
{
    public List<CharacterDefinition> characters = new List<CharacterDefinition>();
    public List<WordDefinition> words = new List<WordDefinition>();
    public List<ComplaintDefinition> complaints = new List<ComplaintDefinition>();
    public List<ItemDefinition> items = new List<ItemDefinition>();
    public List<MinigameDefinition> minigames = new List<MinigameDefinition>();
    public List<EventDefinition> events = new List<EventDefinition>();

    // 일차 고정 이벤트는 이 표에서 관리함 (며칠에 뭐가 나오는지 한눈에)
    public EventSchedule eventSchedule;

    // 브로커 대화 3개 (우주선 사면 바로 -> 선택 -> 고른 대로 하나)
    // 아침 이벤트가 아니라 그 자리에서 바로 트는 거라 여기 따로 둠
    public DialogueData brokerOfferDialogue;    // 우주선 산 직후
    public DialogueData brokerDatabaseDialogue; // DB 접근을 골랐을 때
    public DialogueData brokerReportDialogue;   // 신고를 골랐을 때
    public DialogueData brokerNextDayDialogue; // 우주선 산 다음날 아침

    // 엔딩 대사 (7일 다 버틴 아침, 고른 루트대로 하나)
    public DialogueData databaseEndingDialogue;
    public DialogueData reportEndingDialogue;
    public List<AchievementDefinition> achievements = new List<AchievementDefinition>();
}
