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

    // Where the tutorial arrow must point to - also used as trigger under certain conditions
    public TutorialData3.Location targetLocation = TutorialData3.Location.None;

    // The action performed by the weapon to equip
    public Weapon.ActionType weaponAction = Weapon.ActionType.None;

    // The number of times the trigger must be set off for the step to be completed
    public int countRequirement = 1;

    // The text spoken by the NPC during this step
    public string npcSpeech;

    #endregion

    public TutorialStep3() { }

    public TutorialStep3(TutorialTrigger completionTrigger, string npcSpeech)
       : this(completionTrigger, npcSpeech, 1) { }

    public TutorialStep3(TutorialTrigger completionTrigger, string npcSpeech, int countRequirement)
       : this(completionTrigger, TutorialData3.Location.None, npcSpeech, countRequirement) { }

    public TutorialStep3(TutorialTrigger completionTrigger, TutorialData3.Location targetLocation, string npcSpeech)
       : this(completionTrigger, targetLocation, npcSpeech, 1) { }

    public TutorialStep3(TutorialTrigger completionTrigger, TutorialData3.Location targetLocation, string npcSpeech, int countRequirement)
    {
        this.weaponAction = Weapon.ActionType.None;
        this.completionTrigger = completionTrigger;
        this.targetLocation = targetLocation;
        this.npcSpeech = npcSpeech;
        this.countRequirement = countRequirement;
        this.targetLocation = targetLocation;
    }

    public TutorialStep3(TutorialData3.Location targetLocation, string npcSpeech)
    {
        this.completionTrigger = TutorialTrigger.None;
        this.weaponAction = Weapon.ActionType.None;
        this.countRequirement = 1;
        this.targetLocation = targetLocation;
        this.npcSpeech = npcSpeech;
    }

    public TutorialStep3(Weapon.ActionType weaponAction, string npcSpeech)
    {
        this.targetLocation = TutorialData3.Location.None;
        this.completionTrigger = TutorialTrigger.EquipWeapon;
        this.weaponAction = weaponAction;
        this.countRequirement = 1;
        this.npcSpeech = npcSpeech;
    }

    #region Private Variables

    #endregion
}
