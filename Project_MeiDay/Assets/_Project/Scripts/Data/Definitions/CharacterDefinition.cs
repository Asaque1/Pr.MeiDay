using UnityEngine;

// 등장인물 한 명의 정보를 담는 데이터
// (에디터에서 우클릭 -> Create -> MeiDay -> Character 로 만듦)
[CreateAssetMenu(fileName = "Character_", menuName = "MeiDay/Character")]
public class CharacterDefinition : ScriptableObject
{
    public string id;            // 구분용 이름 (예: char_helen)
    public string displayName;   // 화면에 보이는 이름 (예: 헬렌 머핀 2세)
    public CharacterRole role;   // 무슨 역할인지

    public string gender;        // 성별
    public string job;           // 직업
    public string species;       // 종족

    [TextArea(3, 6)]
    public string bio;           // 설명, 배경 이야기

    public Sprite portrait;      // 얼굴 그림
}
