using UnityEngine;

// 상점에서 파는 아이템 한 개
[CreateAssetMenu(fileName = "Item_", menuName = "MeiDay/Item")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string itemName;     // 아이템 이름
    public ShopType shopType;   // 어느 상점 물건인지
    public ItemKind kind;       // 영구 / 1회성 / 랜덤박스
    public ItemEffect effect;   // 뭘 해주는지

    [TextArea(2, 4)]
    public string description;  // 설명

    public int price = 100;     // 가격
    public bool isSpaceshipTicket = false; // 이게 우주선표인지 (사면 탈출 가능)

    public int maxLevel = 3; // 영구템 몇 레벨까지 올릴 수 있는지
    public float power = 1;  // 효과 세기 (영구템은 레벨당 이만큼씩 세짐)

    // 랜덤박스일 때 보상 범위
    public int boxRewardMin = 0;
    public int boxRewardMax = 0;

    // 이 영구템을 끝까지 사야 풀리는 물건 (브로커 해금용. 없으면 비워둠)
    public ItemDefinition requiresMaxed;
    public int requiredShopLevel = 0;
}
