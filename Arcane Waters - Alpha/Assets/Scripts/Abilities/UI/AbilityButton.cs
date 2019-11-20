using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Index from the AbilityInventory, this value is set directly from the inspector.
   // (If this is 0, then this button will execute the first ability in the ability inventory)
   public int abilityIndex = -1;

   // Where is this button coming from? (from the player battler or selected enemy battler?) 
   public AbilityOrigin abilityOrigin;

   #endregion

   public void OnPointerEnter (PointerEventData eventData) {

      float currentCooldown = BattleManager.self.getPlayerBattler().stanceCurrentCooldown;
      RectTransform frameRectTransform = null;

      switch (abilityOrigin) {
         case AbilityOrigin.Enemy:
            BattleUIManager.self.onAbilityHover.Invoke(BattleManager.self.getPlayerBattler().getAttackAbilities()[abilityIndex]);
            BattleUIManager.self.setTooltipFrame((int) abilityOrigin);

            frameRectTransform = BattleUIManager.self.tooltipWindow;

            frameRectTransform.position = transform.position + new Vector3(frameRectTransform.sizeDelta.x * 1.75f, frameRectTransform.sizeDelta.y * 1.75f);
            break;

         case AbilityOrigin.Player:
            // TODO - ZERONEV: Remove this debug tooltip event later
            BattleUIManager.self.setDebugTooltipState(true);

            RectTransform debugTooltipRect = BattleUIManager.self.debugWIPFrame.GetComponent<RectTransform>();
            debugTooltipRect.position = transform.position + new Vector3(debugTooltipRect.sizeDelta.x * 1.75f, debugTooltipRect.sizeDelta.y * 1.75f);
            break;

         case AbilityOrigin.StanceButton:
            if (BattleUIManager.self.playerStanceFrame.activeSelf) { return; }

            // Set the correct message whenever we hover on the stance button
            string message = currentCooldown > 0 ? ("cooldown: " + currentCooldown.ToString("F0") + " s") : "change stance";
            BattleUIManager.self.setStanceFrameActiveState(true, message);

            frameRectTransform = BattleUIManager.self.stanceButtonFrame.GetComponent<RectTransform>();
            frameRectTransform.position = transform.position + new Vector3(frameRectTransform.sizeDelta.x * 1.15f, frameRectTransform.sizeDelta.y * 2.15f);
            break;

         case AbilityOrigin.StanceAction:

            currentCooldown = BattleManager.self.getPlayerBattler().stanceCurrentCooldown;
            Sprite stanceSprite = null;
            string stanceDescription = string.Empty;
            int abilityCooldown = 0;

            switch (abilityIndex) {
               case 0:
                  stanceSprite = ImageManager.getSprite(AbilityInventory.self.balancedStance.itemIconPath);
                  abilityCooldown = (int)AbilityInventory.self.balancedStance.abilityCooldown;
                  stanceDescription = AbilityInventory.self.balancedStance.itemDescription;
                  break;

               case 1:
                  stanceSprite = ImageManager.getSprite(AbilityInventory.self.offenseStance.itemIconPath);
                  abilityCooldown = (int) AbilityInventory.self.offenseStance.abilityCooldown;
                  stanceDescription = AbilityInventory.self.offenseStance.itemDescription;
                  break;

               case 2:
                  stanceSprite = ImageManager.getSprite(AbilityInventory.self.defenseStance.itemIconPath);
                  abilityCooldown = (int) AbilityInventory.self.defenseStance.abilityCooldown;
                  stanceDescription = AbilityInventory.self.defenseStance.itemDescription;
                  break;
            }

            BattleUIManager.self.showActionStanceFrame(abilityCooldown, stanceSprite, stanceDescription);

            frameRectTransform = BattleUIManager.self.stanceActionFrame.GetComponent<RectTransform>();
            frameRectTransform.position = transform.position + new Vector3(frameRectTransform.sizeDelta.x * -2.25f, frameRectTransform.sizeDelta.y);
            break;

         default:
            D.debug("Ability button not defined");
            break;
      }
   }

   // Whenever we have exit the button with the hover, we hide the tooltip again
   public void OnPointerExit (PointerEventData eventData) {

      switch (abilityOrigin) {
         case AbilityOrigin.Enemy:
            BattleUIManager.self.setTooltipActiveState(false);
            break;

         case AbilityOrigin.Player:
            // TODO - ZERONEV: Remove this debug tooltip event later
            BattleUIManager.self.setDebugTooltipState(false);
            break;

         case AbilityOrigin.StanceButton:
            BattleUIManager.self.setStanceFrameActiveState(false, "");
            break;

         case AbilityOrigin.StanceAction:
            BattleUIManager.self.hideActionStanceFrame();
            break;
      }
   }

   private void OnDisable () {
      BattleUIManager.self.setTooltipActiveState(false);

      // TODO - ZERONEV: Remove this debug tooltip event later
      BattleUIManager.self.setDebugTooltipState(false);
   }

   public enum AbilityOrigin
   {
      Enemy = 1,
      Player = 2,
      StanceButton = 3,
      StanceAction = 4
   }

   #region Private Variables

   #endregion
}
