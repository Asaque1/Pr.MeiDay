using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image speakerFaceImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("선택지 UI")]
    [SerializeField] private Transform choiceParent;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("대사 출력 설정")]
    [SerializeField] private float typingSpeed = 0.04f;

    [Header("선택지 설정")]
    [Tooltip("선택지 표시 시 숨길 GameObject (null 이면 dialogueText 사용)")]
    [SerializeField] private GameObject hideOnChoiceTarget;

    private DialogueData currentDialogueData;
    private int currentLineIndex;
    private bool isTyping;
    private bool isWaitingForChoice;
    private string fullText;
    private Coroutine typingCoroutine;

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
        dialoguePanel.SetActive(false);
        ClearChoiceButtons();
    }

    private void Update()
    {
        if (!dialoguePanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null ||
            dialogueData.dialogueLines == null ||
            dialogueData.dialogueLines.Length == 0)
        {
            Debug.LogWarning("대화 데이터가 없습니다.");
            return;
        }

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
        speakerNameText.text = line.speakerName;

        ClearChoiceButtons();

        // 숨김 대상 다시 표시
        SetHideTargetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeDialogue(line.text));
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
            Destroy(choiceParent.GetChild(i).gameObject);
        }
    }

    private void SelectChoice(DialogueChoiceData choice)
    {
        ClearChoiceButtons();

        // 숨김 대상 다시 표시
        SetHideTargetActive(true);

        ExecuteChoiceEvent(choice.eventId);

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

    private void ExecuteChoiceEvent(int eventId)
    {
        if (eventId == -1)
            return;

        switch (eventId)
        {
            case 0:
                Debug.Log("eventId 0: 보상 0 지급");
                break;
            case 1:
                Debug.Log("eventId 1: 보상 1 지급");
                break;
            case 2:
                Debug.Log("eventId 2: 호감도 증가");
                break;
            case 3:
                Debug.Log("eventId 3: 보상 사이클 시작");
                break;
            default:
                Debug.LogWarning("등록되지 않은 eventId: " + eventId);
                break;
        }
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