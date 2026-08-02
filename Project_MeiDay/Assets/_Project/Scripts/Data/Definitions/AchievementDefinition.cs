using UnityEngine;

// 업적 한 개 정보
// (에디터에서 우클릭 -> Create -> MeiDay -> Achievement 로 만듦)
[CreateAssetMenu(fileName = "Achievement_", menuName = "MeiDay/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    public string id;           // 저장에 쓰는 구분용 이름 (예: ach_first_day)
    public string title;        // 수첩에 보이는 이름

    [TextArea(2, 4)]
    public string description;  // 어떻게 얻는지 설명

    public AchievementCondition condition; // 무슨 조건인지
    public int goal = 1;                   // 조건에 쓰는 숫자 (며칠, 몇 크레딧 등)
    public ItemDefinition requiredItem;    // ItemOwned 조건일 때 어떤 물건인지

    public bool hidden = false; // 켜면 얻기 전까지 수첩에 물음표로 나옴
    public Sprite icon;         // 수첩에 보일 그림 (없어도 됨)
}
