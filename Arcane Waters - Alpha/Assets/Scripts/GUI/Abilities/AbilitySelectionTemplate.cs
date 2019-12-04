using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilitySelectionTemplate : MonoBehaviour {
   #region Public Variables

   // Holds the icon of the skill
   public Image skillIcon;

   // Holds the name of the skill
   public Text skillName;

   // Holds the slot index of the skill
   public Text skillSlot;

   // Button to select the skill
   public Button selectButton;

   // Highlight indicator
   public GameObject highlightObj;

   // Cached the ability sql data
   public AbilitySQLData abilitySQLData;

   // Cached the ability data
   public BasicAbilityData abilityData;

   #endregion
}
