using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FlagShipAbilityRow : MonoBehaviour {
   #region Public Variables

   // Image component
   public Image iconImage;

   // String data
   public string abilityName;
   public string abilityInfo;

   // The ship ability data reference
   public ShipAbilityData shipAbilityData;

   #endregion

   public void pointerEnter () {
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);
      panel.shipAbilityTooltip.triggerAbilityTooltip(transform.position, shipAbilityData);
   }

   public void pointerExit () {
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);
      panel.shipAbilityTooltip.abilityToolTipHolder.SetActive(false);
   }

   #region Private Variables

   #endregion
}
