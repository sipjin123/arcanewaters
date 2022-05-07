using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class InputDebugger : MonoBehaviour {
   #region Public Variables

   // When set to true, this script will track various input variables for debugging purposes
   public static bool debuggingEnabled = false;

   // The canvas group responsible for displaying debug information
   public CanvasGroup debugCanvasGroup;

   // References to text elements for each variable
   public TextMeshProUGUI isActionInputEnabled;
   public List<TextMeshProUGUI> globalPlayerNotNull;
   public List<TextMeshProUGUI> globalPlayerNotDead;
   public TextMeshProUGUI notChatManagerIsTyping;
   public TextMeshProUGUI inputManagerInputEnabled;
   public TextMeshProUGUI chatManagerIsTyping;
   public TextMeshProUGUI notUtilIsBatch;
   public TextMeshProUGUI utilHasInputField;
   public TextMeshProUGUI inputFieldFocused;
   public TextMeshProUGUI nameInputFieldFocused;
   public TextMeshProUGUI notInputFieldFocused;
   public TextMeshProUGUI notNameInputFieldFocused;
   public List<TextMeshProUGUI> utilIsGeneralInputAllowed;
   public TextMeshProUGUI notHasPanelInLinkedList;
   public TextMeshProUGUI notPanelManagerIsLoading;
   public TextMeshProUGUI notIsWritingMail;
   public TextMeshProUGUI areaManagerHasArea;
   public TextMeshProUGUI notPvpInstructionsPanelIsShowing;
   public TextMeshProUGUI canReceiveInput;
   public List<TextMeshProUGUI> isLocalPlayer;
   public TextMeshProUGUI notIsFalling;
   public TextMeshProUGUI notIsAboutToWarpOnClient;
   public TextMeshProUGUI movementInput;
   public TextMeshProUGUI generalActionMapEnabled;
   public TextMeshProUGUI seaActionMapEnabled;
   public TextMeshProUGUI playerFacing;
   public TextMeshProUGUI notShipIsDisabled;
   public TextMeshProUGUI notShipIsDead;
   public TextMeshProUGUI notShipIsGhost;
   public TextMeshProUGUI notShipIsPerformingAttack;
   public TextMeshProUGUI notShipIsChargingCannon;
   public TextMeshProUGUI shipHasReloaded;
   
   #endregion

   private void Update () {
      float canvasTargetAlpha = (debuggingEnabled) ? 0.8f : 0.0f;
      debugCanvasGroup.alpha = Mathf.Lerp(debugCanvasGroup.alpha, canvasTargetAlpha, Time.deltaTime * 5.0f);

      if (debuggingEnabled) {
         updateValues();
      }
   }

   private void updateValues () {
      isActionInputEnabled.text = getBoolString(InputManager.isActionInputEnabled());

      foreach (TextMeshProUGUI text in globalPlayerNotNull) {
         text.text = getBoolString(Global.player != null);
      }

      notChatManagerIsTyping.text = getBoolString(!ChatManager.isTyping());
      inputManagerInputEnabled.text = getBoolString(InputManager.isInputEnabled());
      chatManagerIsTyping.text = getBoolString(ChatManager.isTyping());
      notUtilIsBatch.text = getBoolString(!Util.isBatch());

      GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
      utilHasInputField.text = getBoolString(currentSelection != null && Util.hasInputField(currentSelection));

      inputFieldFocused.text = getBoolString(ChatPanel.self.inputField.isFocused);
      nameInputFieldFocused.text = getBoolString(ChatPanel.self.nameInputField.isFocused);
      notInputFieldFocused.text = getBoolString(!ChatPanel.self.inputField.isFocused);
      notNameInputFieldFocused.text = getBoolString(!ChatPanel.self.nameInputField.isFocused);

      foreach (TextMeshProUGUI text in utilIsGeneralInputAllowed) {
         text.text = getBoolString(Util.isGeneralInputAllowed());
      }
      
      notHasPanelInLinkedList.text = getBoolString(!(PanelManager.self.hasPanelInLinkedList() && !PanelManager.self.get(Panel.Type.PvpScoreBoard).isShowing()));
      notPanelManagerIsLoading.text = getBoolString(!PanelManager.isLoading);
      notIsWritingMail.text = getBoolString(!((MailPanel) PanelManager.self.get(Panel.Type.Mail)).isWritingMail());
      areaManagerHasArea.text = getBoolString(Global.player != null && AreaManager.self.hasArea(Global.player.areaKey));
      notPvpInstructionsPanelIsShowing.text = getBoolString(!PvpInstructionsPanel.isShowing);
      canReceiveInput.text = getBoolString(Global.player != null && Global.player.canReceiveInput());

      foreach (TextMeshProUGUI text in isLocalPlayer) {
         text.text = getBoolString(Global.player != null && Global.player.isLocalPlayer);
      }

      notIsFalling.text = getBoolString(Global.player != null && !Global.player.isFalling());

      foreach (TextMeshProUGUI text in globalPlayerNotDead) {
         text.text = getBoolString(Global.player != null && !Global.player.isDead());
      }

      notIsAboutToWarpOnClient.text = getBoolString(Global.player != null && !Global.player.isAboutToWarpOnClient);

      if (Global.player != null) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();
         if (playerShip != null) {
            movementInput.text = playerShip.getMovementInputDirection().ToString();
         } else {
            movementInput.text = "null";
         }
      } else {
         movementInput.text = "null";
      }

      generalActionMapEnabled.text = getBoolString(InputManager.self.inputMaster.General.enabled);
      seaActionMapEnabled.text = getBoolString(InputManager.self.inputMaster.Sea.enabled);
      
      if (Global.player != null) {
         playerFacing.text = Global.player.facing.ToString();

         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();
         if (playerShip != null) {
            notShipIsDisabled.text = getBoolString(!playerShip.isDisabled);
            notShipIsDead.text = getBoolString(!playerShip.isDead());
            notShipIsGhost.text = getBoolString(!playerShip.isGhost);
            notShipIsPerformingAttack.text = getBoolString(!playerShip.isPerformingAttack());
            notShipIsChargingCannon.text = getBoolString(!playerShip.getIsChargingCannon());
            shipHasReloaded.text = getBoolString(playerShip.hasReloaded());
         }
      }
   }

   private string getBoolString (bool value) {
      return (value) ? "True" : "False";
   }

   #region Private Variables

   #endregion
}
