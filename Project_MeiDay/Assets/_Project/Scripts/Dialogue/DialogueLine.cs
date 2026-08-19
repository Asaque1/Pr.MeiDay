using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public Sprite speakerFace;
    public string speakerName;

    [TextArea(2, 5)]
    public string text;

    // 기존 ChoiceData[]에서 변경
    public DialogueChoiceData[] choices;

    public int nextLineIndex = -1;
}

[System.Serializable]
public class DialogueChoiceData
{
    public string choiceText;
    public int nextLineIndex = -1;
    public int eventId = -1;
}