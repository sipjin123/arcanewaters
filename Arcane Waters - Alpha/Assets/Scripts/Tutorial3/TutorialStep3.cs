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

   // The target that must be pointed to with an arrow, if any
   public TutorialArrow.Target arrowTarget = TutorialArrow.Target.None;

   #endregion

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech)
      : this(completionTrigger, npcSpeech, 1, TutorialArrow.Target.None) { }

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech, int countRequirement)
      : this(completionTrigger, npcSpeech, countRequirement, TutorialArrow.Target.None) { }

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech, TutorialArrow.Target arrowTarget)
      : this(completionTrigger, npcSpeech, 1, arrowTarget) { }

   public TutorialStep3 (TutorialTrigger completionTrigger, string npcSpeech, int countRequirement, TutorialArrow.Target arrowTarget) {
      this.completionTrigger = completionTrigger;
      this.npcSpeech = npcSpeech;
      this.countRequirement = countRequirement;
      this.arrowTarget = arrowTarget;
   }

   #region Private Variables

   #endregion
}
