using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipUISkillTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the skill
   public Text skillName;

   // Icon of the skill
   public Image skillIcon;

   // The ship ability data reference
   public ShipAbilityData shipAbilityData;

   #endregion

   public void pointerEnter () {
      ShipyardScreen.self.shipAbilityTooltip.triggerAbilityTooltip(transform.position, shipAbilityData);
   }

   public void pointerExit () {
      ShipyardScreen.self.shipAbilityTooltip.abilityToolTipHolder.SetActive(false);
   }

   #region Private Variables

   #endregion
}
