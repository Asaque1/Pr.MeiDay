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
    public List<AchievementDefinition> achievements = new List<AchievementDefinition>();
}
