using UnityEngine;

// 상점에서 파는 아이템 한 개
[CreateAssetMenu(fileName = "Item_", menuName = "MeiDay/Item")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string itemName;     // 아이템 이름
    public ItemType itemType;   // 종류

    [TextArea(2, 4)]
    public string description;  // 설명

    public int price = 100;     // 가격
    public bool isSpaceshipTicket = false; // 이게 우주선표인지 (사면 탈출 가능)

    public float suspicionReduce = 0; // 사면 바로 내려가는 의심도 (변장 용품 같은 1회성 효과)
}
