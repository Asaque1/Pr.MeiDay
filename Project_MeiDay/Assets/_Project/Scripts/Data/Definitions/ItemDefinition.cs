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

    public Sprite icon; // 상점에 보이는 그림

    // 영구템은 레벨 오를 때마다 그림이랑 설명이 바뀔 수 있음 (기획서: 만렙이면 달라짐)
    // 안 넣으면 위의 icon / description 을 그대로 씀. 레벨 1부터 순서대로 넣을 것
    public Sprite[] levelIcons;
    [TextArea(2, 4)]
    public string[] levelDescriptions;

    public int price = 100;     // 가격
    public bool isSpaceshipTicket = false; // 이게 우주선표인지 (사면 탈출 가능)

    public int maxLevel = 3; // 영구템 몇 레벨까지 올릴 수 있는지
    public float power = 1;  // 효과 세기 (영구템은 레벨당 이만큼씩 세짐)
    public float sidePower = 0; // 밀수템의 부작용 세기 (보수 깎는 %, 매일 오르는 의심도 등)

    // 알바 한 번 끝낼 때마다 더 받는 크레딧 (레벨당)
    // 이력서 템플릿이랑 위조 신분증이 쓰는 값
    public int arbeitPayBonus = 0;

    // 랜덤박스일 때 보상 범위
    public int boxRewardMin = 0;
    public int boxRewardMax = 0;

    // 이 영구템을 끝까지 사야 풀리는 물건 (브로커 해금용. 없으면 비워둠)
    public ItemDefinition requiresMaxed;
    // 알바를 이 횟수만큼 잘해야 팔아주는 물건 (0 이면 조건 없음)
    // '잘함' 기준은 알바 결과가 '괜찮음' 이상인 판
    public int requiredArbeitGood = 0;

    // 상점 레벨이 얼마 이상이어야 팔아주는 물건 (0 이면 조건 없음)
    // 어느 상점 레벨을 보는지 따로 적어둠 - 브로커 물건이 "서점 5레벨" 같은 조건을 걸 수 있어야 해서
    public ShopType requiredShopType;
    public int requiredShopLevel = 0;

    // 지금 레벨에 맞는 그림 (레벨별로 안 넣었으면 기본 그림)
    public Sprite IconFor(int level)
    {
        int index = level - 1;
        if (levelIcons != null && index >= 0 && index < levelIcons.Length
            && levelIcons[index] != null)
        {
            return levelIcons[index];
        }

        return icon;
    }

    // 지금 레벨에 맞는 설명 (레벨별로 안 넣었으면 기본 설명)
    public string DescriptionFor(int level)
    {
        int index = level - 1;
        if (levelDescriptions != null && index >= 0 && index < levelDescriptions.Length
            && levelDescriptions[index].Length > 0)
        {
            return levelDescriptions[index];
        }

        return description;
    }
}
