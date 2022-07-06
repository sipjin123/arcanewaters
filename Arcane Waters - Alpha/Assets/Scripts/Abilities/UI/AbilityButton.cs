using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using DG.Tweening;

public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Index from the AbilityInventory, this value is set directly from the inspector.
   // (If this is 0, then this button will execute the first ability in the ability inventory)
   public int abilityIndex = -1;

   // The index by type such as Attack index 0
   public int abilityTypeIndex = -1;

   // Where is this button coming from? (from the player battler or selected enemy battler?) 
   public AbilityOrigin abilityOrigin;

   // The icon of the ability
   public Image abilityIcon;

   // The button of this ability
   public Button abilityButton;

   // Holds the grayscale holder
   public GameObject grayScaleObj;

   // If this Button is enabled
   public bool isEnabled;

   // Cool down parameters
   public float cooldownValue, cooldownTarget = .1f;

   // The cooldown image
   public Image cooldownImage;

   // The cancel button
   public Button cancelButton;

   // The ability type 
   public AbilityType abilityType;

   // The animator reference
   public Animator buttonAnimator;

   // The animation clip names
   public static string INVALID_ANIM = "Invalid";
   public static string SELECT_ANIM = "Select";
   public static string IDLE_ANIM = "Idle";

   // Type of sprites to activate depending on the ability type
   public GameObject[] buffTypeSprites, attackTypeSprites;

   // If is an invalid ability based on the weapon equipped
   public bool isInvalidAbility;

   // The transform that holds all the visuals for the button
   public Transform buttonVisuals;

   // The transform that holds the visuals for the button border
   public Transform borderVisuals;
   
   // Reference to ability button pending indicator
   public GameObject pendingIndicator;
   
   // Ability tooltip
   public ToolTipComponent tooltip;

   // The last disable trigger for tracking
   public string lastDisableTrigger;
   
   // Flag if ability is currently on cooldown
   public bool onCooldown => cooldownValue < cooldownTarget - .1f;

   // Flag if ability is a pending ability
   public bool isAbilityPending;

   #endregion

   public void setAbility (AbilityType abilityType) {
      this.abilityType = abilityType;
      
      if (abilityType == AbilityType.Standard) {
         foreach (GameObject obj in attackTypeSprites) {
            obj.SetActive(true);
         }
         foreach (GameObject obj in buffTypeSprites) {
            obj.SetActive(false);
         }
      } else if (abilityType == AbilityType.BuffDebuff) {
         foreach (GameObject obj in attackTypeSprites) {
            obj.SetActive(false);
         }
         foreach (GameObject obj in buffTypeSprites) {
            obj.SetActive(true);
         }
      } else if (abilityType == AbilityType.Stance) {
         // TODO: Add stance logic here
      }
   }

   public void startCooldown (float coolDownTarget) {
      CancelInvoke();
      cooldownValue = 0;
      cooldownTarget = coolDownTarget;
      cooldownImage.enabled = true;
      cooldownImage.fillAmount = 1;
      InvokeRepeating("processCooldownTime", 0, .1f);
   }

   public void invalidButtonClick () {
      if (buttonAnimator.isActiveAndEnabled) {
         buttonAnimator.Play(INVALID_ANIM);
      }
   }

   public void playSelectAnim () {
      if (buttonAnimator.isActiveAndEnabled) {
         buttonAnimator.Play(SELECT_ANIM);
      }

      borderVisuals.DORewind();
      borderVisuals.DOScale(0.9f, 0.2f);
      buttonVisuals.DORewind();
      buttonVisuals.DOScale(1.3f, 0.15f).SetEase(Ease.InElastic);
   }

   public void playIdleAnim () {
      if (buttonAnimator.isActiveAndEnabled) {
         buttonAnimator.Play(IDLE_ANIM);
      }

      borderVisuals.DORewind();
      borderVisuals.DOScale(1.0f, 0.2f);
      buttonVisuals.DORewind();
      buttonVisuals.DOScale(1.0f, 0.15f).SetEase(Ease.OutElastic);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (!isEnabled) {
         return;
      }

      double currentCooldown = BattleManager.self.getPlayerBattler().stanceCurrentCooldown;

      switch (abilityOrigin) {
         case AbilityOrigin.Enemy:
            if (BattleManager.self.getPlayerBattler().basicAbilityIDList.Count < abilityIndex) {
               D.debug("Ability Index mismatch!");
            } else {
               try {
                  BasicAbilityData fetchedAbilityData = AbilityManager.getAbility(BattleManager.self.getPlayerBattler().basicAbilityIDList[abilityIndex], AbilityType.Undefined);
                  BattleUIManager.self.onAbilityHover.Invoke(fetchedAbilityData);
               } catch {
                  D.adminLog("Ability mismatch!: " + abilityIndex + " / " + BattleManager.self.getPlayerBattler().basicAbilityIDList.Count, D.ADMIN_LOG_TYPE.Ability);
               }
            }
            break;

         case AbilityOrigin.Player:
            // TODO - ZERONEV: Remove this debug tooltip event later
            BattleUIManager.self.setDebugTooltipState(true);

            RectTransform debugTooltipRect = BattleUIManager.self.debugWIPFrame.GetComponent<RectTransform>();
            debugTooltipRect.position = transform.position + new Vector3(debugTooltipRect.sizeDelta.x * 1.75f, debugTooltipRect.sizeDelta.y * 1.75f);
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
            BattleUIManager.self.setDescriptionActiveState(false);
            break;

         case AbilityOrigin.Player:
            // TODO - ZERONEV: Remove this debug tooltip event later
            BattleUIManager.self.setDebugTooltipState(false);
            break;
      }
   }

   private void processCooldownTime () {
      if (cooldownValue < cooldownTarget) {
         cooldownValue+= .1f;
         cooldownImage.fillAmount = cooldownValue / cooldownTarget;
      } else {
         cooldownImage.enabled = false;
         BattleUIManager.self.updateButtons();
         playIdleAnim();
         CancelInvoke();
      }
   }

   public void enableButton () {
      if (isInvalidAbility) {
         D.debug("Skip enable button! Invalid ability: " + abilityIndex);
         return;
      }

      Image buttonImage = GetComponent<Image>();
      if (buttonImage != null) {
         buttonImage.raycastTarget = true;
      }

      foreach (Image image in GetComponentsInChildren<Image>()) {
         image.raycastTarget = true;
      }

      tooltip.GetComponent<Image>().raycastTarget = false;
      
      abilityButton.interactable = true;

      abilityIcon.color = Color.white;
      isEnabled = true;
   }

   public void disableButton (string triggerState) {
      lastDisableTrigger = triggerState;
      Image buttonImage = GetComponent<Image>();
      if (buttonImage != null) {
         buttonImage.raycastTarget = false;
      }

      foreach (Image image in GetComponentsInChildren<Image>()) {
         image.raycastTarget = false;
      }

      tooltip.GetComponent<Image>().raycastTarget = true;
      tooltip.message = "select an enemy";
      
      abilityButton.interactable = false;

      abilityIcon.color = Color.gray;
      isEnabled = false;
   }
   
   public void togglePendingIndicator (bool isPending) {
      // Show/Hide ability button pending indicator
      isAbilityPending = isPending;
      pendingIndicator.SetActive(isPending);
   }

   public void clearButton () {
      abilityIndex = -1;
      abilityTypeIndex = -1;
      abilityType = AbilityType.Undefined;
      abilityIcon.sprite = ImageManager.self.blankSprite;
      disableButton("ClearButton");
      enabled = false;
      gameObject.SetActive(false);
      togglePendingIndicator(false);
   }

   public Button getButton () {
      return abilityButton;
   }

   //private void OnDisable () {
   //   BattleUIManager.self.setDescriptionActiveState(false);

   //   // TODO - ZERONEV: Remove this debug tooltip event later
   //   BattleUIManager.self.setDebugTooltipState(false);
   //}

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
