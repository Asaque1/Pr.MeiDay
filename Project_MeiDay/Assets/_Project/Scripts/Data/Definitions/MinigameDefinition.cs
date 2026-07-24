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
    public int expReward = 8;   // 수행률 100% 기준 상점 경험치 (아이템 구매는 10)

    public MinigameKind kind;   // 어떤 규칙으로 도는 알바인지

    // 책 정리(BookSort)용 설정
    public int bookCount = 6;   // 한 판에 나오는 책 수
    public int genreCount = 3;  // 장르(칸) 개수

    // 시간제 알바(조명 등)용 설정
    public float playSeconds = 20f; // 제한 시간
}
