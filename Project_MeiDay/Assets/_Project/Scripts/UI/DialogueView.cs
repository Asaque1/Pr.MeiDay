using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아침 이벤트 대사창 - 타이핑 효과로 대사를 한 글자씩 보여줌
public class DialogueView : MonoBehaviour
{
    [SerializeField] GameObject dialogueBox;   // 대사창 전체 (이벤트 중일 때만 켜짐. 이 스크립트가 붙은 오브젝트 말고 자식을 넣을 것)
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

        // 화자가 없는 줄(혼잣말/지문)이면 초상화 칸을 아예 숨김
        // 안 그러면 그림 없는 흰 사각형이 뜨거나 직전 인물 얼굴이 그대로 남았음
        Sprite face = (line.speaker != null) ? line.speaker.portrait : null;
        if (line.speaker != null)
        {
            speakerText.text = line.speaker.displayName;
        }
        if (portraitImage != null)
        {
            portraitImage.sprite = face;
            portraitImage.enabled = (face != null);
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

            // 타이핑 소리 (시끄러우면 몇 글자마다 한 번으로 조절)
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx(SoundManager.instance.typeTick);
            }

            yield return new WaitForSeconds(charDelay);
        }

        _typing = false;
        _typeRoutine = null;
        nextIndicator.SetActive(true);
    }

    // 대사창 클릭 (버튼 OnClick 에 연결)
    // 대사창의 건너뛰기 버튼 (버튼 OnClick 에 연결)
    // 한 줄씩 넘기는 게 아니라 이번 이벤트 대사를 통째로 닫음
    public void OnClickSkip()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        StopTyping();
        GameManager.instance.events.SkipLines();
    }

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
