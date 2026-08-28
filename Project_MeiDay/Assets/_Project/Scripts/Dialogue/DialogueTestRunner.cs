using UnityEngine;
using UnityEngine.InputSystem;

// 대화 테스트 씬(DialogueTest) 전용 실행기
// 인스펙터에 대화 에셋 꽂고 플레이 누르면 바로 재생됨
// 본편 씬엔 안 들어감 - 대화 내용만 따로 확인하는 용도
public class DialogueTestRunner : MonoBehaviour
{
    [Header("테스트할 대화")]
    public DialogueData dialogue; // 여기에 DialogueData 에셋을 끌어다 놓기

    [Header("동작")]
    public bool playOnStart = true; // 플레이 누르면 바로 시작

    void Start()
    {
        if (playOnStart == true)
        {
            // 바로 틀면 DialogueSystem 의 Start(판 꺼두기)가 뒤에 돌면서 도로 꺼버림
            // 한 프레임 기다렸다가 시작
            StartCoroutine(PlayNextFrame());
        }
    }

    System.Collections.IEnumerator PlayNextFrame()
    {
        yield return null;
        Play();
    }

    void Update()
    {
        // R 키로 처음부터 다시 (대화 끝난 뒤에도 됨)
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Play();
        }
    }

    // 지금 꽂힌 대화 재생
    [ContextMenu("재시작")]
    public void Play()
    {
        if (dialogue == null)
        {
            Debug.LogWarning("대화 에셋이 비어있음 - Tester 오브젝트의 Dialogue 칸에 끌어다 놓기");
            return;
        }
        if (DialogueSystem.Instance == null)
        {
            Debug.LogWarning("씬에 DialogueSystem(DialogueRoot) 이 없음");
            return;
        }

        DialogueSystem.Instance.StartDialogue(dialogue);
    }
}
