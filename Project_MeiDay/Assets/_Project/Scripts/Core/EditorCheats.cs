#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

// 에디터에서만 도는 치트
// 파일 전체가 UNITY_EDITOR 로 묶여있어서 빌드에는 아예 안 들어감
//
// F1 = 다음 날
//
// 씬에 붙일 필요 없음 - 플레이 누르면 알아서 하나 생김
public class EditorCheats : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        GameObject go = new GameObject("EditorCheats (F1: 다음 날)");
        DontDestroyOnLoad(go); // 씬 갈아끼워도 살아있게
        go.AddComponent<EditorCheats>();
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame == true)
        {
            NextDay();
        }
    }

    // 다음 날로 (밥값 / 월세 / 엔딩 체크는 SleepAndAdvance 가 알아서 함)
    void NextDay()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return; // 로비나 프롤로그엔 없음
        }

        if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
            Debug.Log("치트: 이미 끝난 판이라 안 넘김");
            return;
        }

        // 대사가 떠 있으면 먼저 닫고 넘김
        // 대사 하나 끝나면 바로 다음 대사가 뜨는 경우가 있어서 몇 번 돌려봄
        for (int i = 0; i < 8; i++)
        {
            if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsPlaying == false)
            {
                break;
            }

            DialogueSystem.Instance.OnClickSkip();
        }

        gm.SleepAndAdvance();
        Debug.Log("치트: 다음 날 -> " + gm.calendar.day + "일차");
    }
}
#endif
