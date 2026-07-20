using UnityEngine;

// 미니게임 한 개 정보 (밤에 하는 아르바이트용)
[CreateAssetMenu(fileName = "Minigame_", menuName = "MeiDay/Minigame")]
public class MinigameDefinition : ScriptableObject
{
    public string id;
    public string minigameName; // 미니게임 이름

    [TextArea(2, 4)]
    public string description;  // 설명

    public string sceneName;    // 불러올 씬 이름

    public ShopType shopType;   // 어느 상점 알바인지.
    public int basePay = 40;    // 수행률 100% 기준 보수
    public float bonusChance = 0.1f;    // 보너스 터질 확률
    public float bonusMultiplier = 2f;  // 보너스 배수
}
