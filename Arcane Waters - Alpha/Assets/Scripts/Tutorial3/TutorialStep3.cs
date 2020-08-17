using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialStep3
{
   #region Public Variables

   // The key that will trigger the completion of the step
   public TutorialTrigger completionTrigger;

   // The number of times the trigger must be set off for the step to be completed
   public int countRequirement = 1;

   // The text spoken by the NPC during this step
   public string npcSpeech;

   #endregion

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech, int countRequirement = 1) {
      this.completionTrigger = completionTrigger;
      this.npcSpeech = npcSpeech;
      this.countRequirement = countRequirement;
   }

   #region Private Variables

   #endregion
}
