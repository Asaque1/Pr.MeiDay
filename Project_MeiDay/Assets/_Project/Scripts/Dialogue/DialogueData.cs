using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}
