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

    // 이 선택지를 고르면 벌어지는 일 (-1 이면 아무 일도 안 남)
    //   0 크레딧    eventValue 만큼 주고 뺌 (음수면 뺌)
    //   1 의심도    eventValue 만큼 올리고 내림 (음수면 내림)
    //   2 상점 해금  eventValue 안 씀
    //   3 구매 잠금  eventValue 번 상점에서 물건을 못 사게 함
    //   4 구매 해금  eventValue 번 상점을 다시 열어줌
    //     (상점 번호는 0 서점 / 1 연극단 / 2 사무용품점 / 3 브로커)
    public int eventId = -1;
    public int eventValue = 0;
}