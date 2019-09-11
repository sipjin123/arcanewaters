using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Index from the AbilityInventory, this value is set directly from the inspector.
   // (If this is 0, then this button will execute the first ability in the ability inventory.)
   public int abilityIndex = -1;

   // Where is this button coming from? (from the player battler or selected enemy battler?) 
   public AbilityOrigin abilityOrigin;

   #endregion

   public void executeAbility () {
      //Debug.Log("executed ability: " + AbilityInventory.self.equippedAbilities[abilityIndex].getName);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      BattleUIManager.self.onAbilityHover.Invoke(BattleManager.self.getPlayerBattler().getAbilities[abilityIndex]);

      BattleUIManager.self.setTooltipFrame((int) abilityOrigin);

      RectTransform tooltipRect = BattleUIManager.self.tooltipWindow;

      tooltipRect.position = transform.position + new Vector3(tooltipRect.sizeDelta.x * 1.75f, tooltipRect.sizeDelta.y * 1.75f);
   }

   // Whenever we have exit the button with the hover, we hide the tooltip again.
   public void OnPointerExit (PointerEventData eventData) {
      BattleUIManager.self.setTooltipActiveState(false);
   }

   private void OnDisable () {
      BattleUIManager.self.setTooltipActiveState(false);
   }

   public enum AbilityOrigin
   {
      Enemy = 1,
      Player = 2
   }

   #region Private Variables

   #endregion
}
