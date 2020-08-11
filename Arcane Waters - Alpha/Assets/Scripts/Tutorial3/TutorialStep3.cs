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

   // The text spoken by the NPC during this step
   public string npcSpeech;

   #endregion

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech) {
      this.completionTrigger = completionTrigger;
      this.npcSpeech = npcSpeech;
   }

   #region Private Variables

   #endregion
}
