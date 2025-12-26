using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TodaysMenuStory : MonoBehaviour
{
    public void WronSeasonings() 
    {
        DialogueManager.Instance.TriggerDialogueByEvent("WrongSeasoning");
    }
}
