using System.Collections.Generic;
using UnityEngine;

// 이벤트 대사가 뜨는 동안 민원 화면 것들을 잠깐 감춤
// 안 감추면 말풍선이 두 개(민원인 민원 + 지금 대사)가 돼서 누가 말하는지 헷갈림
//
// 오브젝트를 껐다 켜는 대신 CanvasGroup 투명도를 씀
// 민원인은 TranslationView 가 Image.enabled 랑 NpcParts 로 직접 켜고 꺼서,
// 여기서 오브젝트까지 껐다 켜면 그쪽 판단을 덮어써버림
public class HideWhileTalking : MonoBehaviour
{
    [SerializeField] List<CanvasGroup> targets = new List<CanvasGroup>();

    void Update()
    {
        bool talking = DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying;
        float want = talking == true ? 0f : 1f;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            if (targets[i].alpha != want)
            {
                targets[i].alpha = want;
            }

            // 안 보이는 카드나 상자를 실수로 누르는 것도 같이 막음
            targets[i].blocksRaycasts = talking == false;
        }
    }
}
