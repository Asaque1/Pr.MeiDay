using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image speakerFaceImage;
    [SerializeField] private GameObject portraitFrameRoot; // 얼굴 뒤에 깔리는 배경틀 (얼굴이랑 같이 켜지고 꺼짐)
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("선택지 UI")]
    [SerializeField] private Transform choiceParent;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("대사 출력 설정")]
    [SerializeField] private float typingSpeed = 0.04f;

    [Header("초상화 표시 설정")]
    [Tooltip("인물 그림을 위에서부터 세로로 얼마나 보여줄지 (0.75 = 머리부터 몸까지, 1 = 전신)")]
    [SerializeField, Range(0.3f, 1f)] private float portraitShowRatio = 0.75f;
    [Tooltip("초상화 틀 안쪽 여백")]
    [SerializeField] private float portraitPadding = 6f;

    [Header("선택지 설정")]
    [Tooltip("선택지 표시 시 숨길 GameObject (null 이면 dialogueText 사용)")]
    [SerializeField] private GameObject hideOnChoiceTarget;

    private DialogueData currentDialogueData;

    // 지금 대화가 떠 있는지 (선택창 같은 게 겹쳐 뜨지 않게 물어봄)
    public bool IsPlaying { get { return currentDialogueData != null; } }
    private int currentLineIndex;
    private bool isTyping;
    private bool isWaitingForChoice;
    private string fullText;
    private Coroutine typingCoroutine;
    private System.Action onDialogueEnd; // 대화 끝나면 부를 곳 (이벤트가 이어서 굴러가야 함)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 시작 시 대화창과 선택지 모두 숨김
        //
        // 단, 이미 대화가 돌고 있으면 건드리지 않음
        // 컷신을 보고 넘어오면 UiRoot 의 Start 안에서 게임이 시작되는데,
        // 그때 이 Start 가 아직 안 돌았으면 방금 켠 대화창을 여기서 도로 꺼버림
        // (대사가 안 끝나니 민원도 시작 안 돼서 민원인이 영영 안 나왔음)
        if (currentDialogueData != null)
        {
            return;
        }

        dialoguePanel.SetActive(false);
        ClearChoiceButtons();
    }

    private void Update()
    {
        if (!dialoguePanel.activeSelf)
            return;

        // 선택지가 떠 있는 동안은 버튼만 받음 (아무 데나 눌러서 넘어가면 선택이 씹힘)
        if (choiceParent != null && choiceParent.childCount > 0)
            return;

        if (WasAdvancePressed())
        {
            AdvanceDialogue();
        }
    }

    // 이 프로젝트는 새 Input System 이라 Input.GetKeyDown 은 못 씀
    private static bool WasAdvancePressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        StartDialogue(dialogueData, null);
    }

    // onEnd: 대화가 끝나면 불러줌 (이벤트가 끝나야 게임이 다시 굴러가서 필요함)
    public void StartDialogue(DialogueData dialogueData, System.Action onEnd)
    {
        if (dialogueData == null ||
            dialogueData.dialogueLines == null ||
            dialogueData.dialogueLines.Length == 0)
        {
            Debug.LogWarning("대화 데이터가 없습니다.");

            // 여기서 그냥 돌아가면 부른 쪽이 계속 기다리다가 게임이 멈춤
            if (onEnd != null)
                onEnd();

            return;
        }

        onDialogueEnd = onEnd;
        currentDialogueData = dialogueData;
        currentLineIndex = 0;

        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentDialogueData == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentDialogueData.dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogueData.dialogueLines[currentLineIndex];

        speakerFaceImage.sprite = line.speakerFace;
        // 얼굴 없는 대사는 스프라이트 빈 이미지가 흰 네모로 떠서, 있을 때만 켬
        // 배경틀도 얼굴이랑 같이 - 얼굴 없는데 틀만 떠 있으면 빈 남색 상자가 됨
        speakerFaceImage.enabled = (line.speakerFace != null);
        if (portraitFrameRoot != null)
        {
            portraitFrameRoot.SetActive(line.speakerFace != null);
        }
        FitPortrait(line.speakerFace);
        speakerNameText.text = line.speakerName;

        ClearChoiceButtons();

        // 숨김 대상 다시 표시
        SetHideTargetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeDialogue(line.text));
    }

    // 인물 그림을 초상화 틀에 앉히기
    // 전신 그림을 틀에 통째로 넣으면 사람이 콩알만 해짐 - 얼굴이 안 보임
    // 그래서 머리를 틀 위에 붙이고 위에서 portraitShowRatio 만큼만 보여줌
    // 아래로 넘치는 만큼은 틀(RectMask2D)이 잘라내서 "머리부터 몸까지"로 끊김
    // 단, 가로로 넘칠 것 같으면 가로에 맞춰 줄임 - 팔이 잘려나가면 안 되니까
    private void FitPortrait(Sprite face)
    {
        if (face == null || speakerFaceImage == null)
            return;

        RectTransform portrait = speakerFaceImage.rectTransform;
        RectTransform frame = portrait.parent as RectTransform;
        if (frame == null)
            return;

        float drawWidth = frame.rect.width - portraitPadding * 2f; // 좌우는 여백을 둠
        float drawHeight = frame.rect.height - portraitPadding;    // 위만 여백, 아래는 잘리는 쪽
        float faceWidth = face.rect.width;
        float faceHeight = face.rect.height;

        if (drawWidth <= 0f || drawHeight <= 0f || faceWidth <= 0f || faceHeight <= 0f)
            return; // 틀이나 그림이 이상하면 건드리지 않음

        // 그림의 위쪽 portraitShowRatio 만큼이 틀 높이를 채우도록 키움
        float scale = drawHeight / (faceHeight * portraitShowRatio);

        if (faceWidth * scale > drawWidth)
        {
            scale = drawWidth / faceWidth; // 가로가 넘치면 팔 안 잘리게 줄임
        }

        // 키가 작아서 다 들어가는 그림은 바닥에 세움 (위에 붙이면 공중에 뜬 것처럼 보임)
        float drawn = faceHeight * scale;
        float gap = drawHeight - drawn;
        if (gap < 0f)
        {
            gap = 0f;
        }

        portrait.anchorMin = new Vector2(0.5f, 1f);
        portrait.anchorMax = new Vector2(0.5f, 1f);
        portrait.pivot = new Vector2(0.5f, 1f); // 머리 꼭대기가 기준점
        portrait.sizeDelta = new Vector2(faceWidth * scale, drawn);
        portrait.anchoredPosition = new Vector2(0f, -(portraitPadding + gap));
    }

    private IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        isWaitingForChoice = false;
        fullText = text;
        dialogueText.text = "";

        foreach (char character in text)
        {
            dialogueText.text += character;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        if (currentDialogueData != null &&
            currentLineIndex < currentDialogueData.dialogueLines.Length)
        {
            DialogueLine currentLine = currentDialogueData.dialogueLines[currentLineIndex];
            if (currentLine.choices != null && currentLine.choices.Length > 0)
            {
                isWaitingForChoice = true;
            }
        }
    }

    // 대사 통째로 건너뛰기 (대사창 오른쪽 위 버튼)
    // 한 줄씩 넘기는 게 아니라 이 대화를 그냥 닫음
    //
    // 선택지가 떠 있어도 닫음 - 지금 대화의 선택지는 전부 효과가 없어서 놓치는 게 없음
    // (상점 해금 같은 건 선택지가 아니라 이벤트 쪽에 붙어있음)
    // 나중에 효과 있는 선택지를 넣으면 여기서 막아줘야 함
    public void OnClickSkip()
    {
        if (currentDialogueData == null)
            return;

        EndDialogue();
    }

    private void AdvanceDialogue()
    {
        if (currentDialogueData == null)
            return;

        if (isTyping)
        {
            FinishTyping();
            return;
        }

        if (isWaitingForChoice)
        {
            ShowChoices();
            return;
        }

        DialogueLine currentLine = currentDialogueData.dialogueLines[currentLineIndex];

        if (currentLine.nextLineIndex == -1)
        {
            EndDialogue();
        }
        else
        {
            currentLineIndex = currentLine.nextLineIndex;
            ShowCurrentLine();
        }
    }

    private void FinishTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = fullText;
        isTyping = false;

        if (currentDialogueData != null &&
            currentLineIndex < currentDialogueData.dialogueLines.Length)
        {
            DialogueLine currentLine = currentDialogueData.dialogueLines[currentLineIndex];
            if (currentLine.choices != null && currentLine.choices.Length > 0)
            {
                isWaitingForChoice = true;
            }
        }
    }

    private void ShowChoices()
    {
        isWaitingForChoice = false;

        if (currentDialogueData == null ||
            currentLineIndex >= currentDialogueData.dialogueLines.Length)
        {
            return;
        }

        DialogueLine currentLine = currentDialogueData.dialogueLines[currentLineIndex];

        if (currentLine.choices != null && currentLine.choices.Length > 0)
        {
            CreateChoiceButtons(currentLine.choices);
        }
    }

    private void CreateChoiceButtons(DialogueChoiceData[] choices)
    {
        ClearChoiceButtons();

        // 지정한 오브젝트 숨김 (null 이면 dialogueText)
        SetHideTargetActive(false);

        for (int i = 0; i < choices.Length; i++)
        {
            DialogueChoiceData choice = choices[i];

            GameObject buttonObject =
                Instantiate(choiceButtonPrefab, choiceParent, false);

            Button button = buttonObject.GetComponent<Button>();
            TMP_Text buttonText =
                buttonObject.GetComponentInChildren<TMP_Text>();

            if (button == null)
            {
                Debug.LogError("선택지 프리팹에 Button 컴포넌트가 없습니다.");
                Destroy(buttonObject);
                continue;
            }

            if (buttonText == null)
            {
                Debug.LogError("선택지 프리팹 안에 TMP_Text 가 없습니다.");
                Destroy(buttonObject);
                continue;
            }

            buttonText.text = choice.choiceText;
            button.onClick.AddListener(() => SelectChoice(choice));
        }
    }

    private void ClearChoiceButtons()
    {
        if (choiceParent == null)
            return;

        for (int i = choiceParent.childCount - 1; i >= 0; i--)
        {
            GameObject old = choiceParent.GetChild(i).gameObject;

            // Destroy 는 이 프레임 끝에야 실제로 지워짐
            // 그동안 버튼이 그대로 눌려서, 빠르게 두 번 누르면 선택이 두 번 먹었음
            // (다음 선택지가 같은 자리에 새로 깔리기도 함)
            Button oldButton = old.GetComponent<Button>();
            if (oldButton != null)
            {
                oldButton.onClick.RemoveAllListeners();
                oldButton.interactable = false;
            }
            old.SetActive(false);

            Destroy(old);
        }
    }

    private void SelectChoice(DialogueChoiceData choice)
    {
        ClearChoiceButtons();

        // 숨김 대상 다시 표시
        SetHideTargetActive(true);

        ExecuteChoiceEvent(choice.eventId, choice.eventValue);

        if (choice.nextLineIndex == -1)
        {
            EndDialogue();
        }
        else
        {
            currentLineIndex = choice.nextLineIndex;
            ShowCurrentLine();
        }
    }

    // 선택지 효과를 실제 게임에 먹임 (번호 뜻은 DialogueChoiceData 주석 참고)
    private void ExecuteChoiceEvent(int eventId, int eventValue)
    {
        if (eventId == -1)
            return;

        if (GameManager.instance == null)
        {
            Debug.LogWarning("게임이 아직 준비 전이라 선택지 효과를 못 먹임: " + eventId);
            return;
        }

        switch (eventId)
        {
            case 0:
                GameManager.instance.economy.AddCredits(eventValue);
                break;
            case 1:
                if (eventValue > 0)
                {
                    GameManager.instance.suspicion.AddSuspicion(eventValue);
                }
                else if (eventValue < 0)
                {
                    GameManager.instance.suspicion.ReduceSuspicion(-eventValue);
                }
                break;
            case 2:
                GameManager.instance.UnlockShop();
                break;
            case 3:
            case 4:
                // 상점 구매 탭을 잠그고 여는 건 같은 처리라 한 자리에서 봄
                SetShopBuyLocked(eventValue, eventId == 3);
                break;
            default:
                Debug.LogWarning("등록되지 않은 eventId: " + eventId);
                break;
        }
    }

    // eventValue 로 받은 번호가 진짜 상점인지 보고 구매 탭을 잠그거나 열어줌
    private void SetShopBuyLocked(int shopNumber, bool locked)
    {
        if (shopNumber < 0 || shopNumber > (int)ShopType.Broker)
        {
            Debug.LogWarning("그런 상점 번호가 없음: " + shopNumber);
            return;
        }

        GameManager.instance.shop.SetBuyLocked((ShopType)shopNumber, locked);
    }

    private void EndDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        ClearChoiceButtons();
        dialoguePanel.SetActive(false);
        isTyping = false;
        isWaitingForChoice = false;
        currentDialogueData = null;

        // 부를 곳을 먼저 비워둠 - 이어서 다음 대화가 시작돼도 안 꼬이게
        System.Action callback = onDialogueEnd;
        onDialogueEnd = null;

        if (callback != null)
            callback();
    }

    // hideOnChoiceTarget 가 있으면 그걸, 없으면 dialogueText 를 제어
    private void SetHideTargetActive(bool active)
    {
        if (hideOnChoiceTarget != null)
        {
            hideOnChoiceTarget.SetActive(active);
        }
        else
        {
            dialogueText.gameObject.SetActive(active);
        }
    }
}