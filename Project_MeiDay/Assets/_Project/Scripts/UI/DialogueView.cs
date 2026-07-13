using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아침 이벤트 대사창 - 타이핑 효과로 대사를 한 글자씩 보여줌
public class DialogueView : MonoBehaviour
{
    [SerializeField] GameObject dialogueBox;   // 대사창 전체 (이벤트 중일 때만 켜짐)
    [SerializeField] TMP_Text speakerText;     // 말하는 사람 이름
    [SerializeField] TMP_Text lineText;        // 대사 내용
    [SerializeField] GameObject nextIndicator; // 출력 다 되면 나오는 넘김 표시
    [SerializeField] Image portraitImage;      // 말하는 사람 얼굴

    [SerializeField] float charDelay = 0.04f;  // 글자 하나 나오는 간격
    [SerializeField] float openDelay = 1.2f;   // 등장하고 대사 시작까지 텀

    bool _typing = false;  // 지금 타이핑 연출 중인지
    string _fullLine = ""; // 지금 대사 전체 내용
    Coroutine _typeRoutine;

    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return; // 아직 게임 준비 전이면 패스
        }

        GameManager.instance.events.OnDialogueUpdated += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.events.OnDialogueUpdated -= Refresh;
        }
    }

    // 대사 상태 바뀔 때마다 다시 그림
    void Refresh()
    {
        EventManager em = GameManager.instance.events;
        EventLine line = em.CurrentLine();

        // 이벤트 중 아니면 대사창 끔
        if (line == null)
        {
            StopTyping();
            dialogueBox.SetActive(false);
            return;
        }

        // 방금 열리는 거면 등장 텀을 줌
        bool firstOpen = (dialogueBox.activeSelf == false);
        dialogueBox.SetActive(true);

        speakerText.text = "";
        if (line.speaker != null)
        {
            speakerText.text = line.speaker.displayName;
            if (portraitImage != null)
            {
                portraitImage.sprite = line.speaker.portrait;
            }
        }

        _fullLine = line.text;

        StopTyping();
        _typeRoutine = StartCoroutine(TypeLine(firstOpen));
    }

    // 돌던 타이핑 연출 멈추기
    void StopTyping()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }
        _typing = false;
    }

    // 글자 하나씩 나오는 연출
    IEnumerator TypeLine(bool withOpenDelay)
    {
        _typing = true;
        nextIndicator.SetActive(false);
        lineText.text = "";

        if (withOpenDelay == true)
        {
            yield return new WaitForSeconds(openDelay);
        }

        for (int i = 0; i < _fullLine.Length; i++)
        {
            lineText.text += _fullLine[i];
            yield return new WaitForSeconds(charDelay);
        }

        _typing = false;
        _typeRoutine = null;
        nextIndicator.SetActive(true);
    }

    // 대사창 클릭 (버튼 OnClick 에 연결)
    public void OnClickDialogue()
    {
        // 타이핑 중이면 일단 전부 보여주기
        if (_typing == true)
        {
            StopTyping();
            lineText.text = _fullLine;
            nextIndicator.SetActive(true);
            return;
        }

        // 다 나온 상태면 다음 대사로
        GameManager.instance.events.AdvanceLine();
    }
}
