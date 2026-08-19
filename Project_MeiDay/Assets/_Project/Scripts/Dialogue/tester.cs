using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tester : MonoBehaviour
{
    public DialogueData questDialogueData;

    public void Decn()
    {
        DialogueSystem.Instance.StartDialogue(questDialogueData);
    }
}
