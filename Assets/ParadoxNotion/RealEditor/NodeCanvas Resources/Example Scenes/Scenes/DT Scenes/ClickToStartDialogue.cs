using NodeCanvas.DialogueTrees;
using System.Collections;
using UnityEngine;

public class ClickToStartDialogue : MonoBehaviour
{

    public DialogueTreeController dialogueController;

    private void OnMouseDown()
    {
        gameObject.SetActive(false);
        dialogueController.StartDialogue(OnDialogueEnd);
    }

    private void OnDialogueEnd(bool success)
    {
        gameObject.SetActive(true);
    }
}