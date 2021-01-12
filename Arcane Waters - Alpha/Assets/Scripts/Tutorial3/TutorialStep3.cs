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

   // Where the tutorial arrow must point to - also used as trigger under certain conditions (areaKey)
   public string targetAreaKey;

   // The action performed by the weapon to equip
   public Weapon.ActionType weaponAction = Weapon.ActionType.None;

   // The number of times the trigger must be set off for the step to be completed
   public int countRequirement = 1;

   // The text spoken by the NPC during this step
   public string npcSpeech;

   #endregion

   public TutorialStep3 () { }

   #region Private Variables

   #endregion
}
