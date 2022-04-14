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
   public TextMeshProUGUI globalPlayerNotDead;
   public TextMeshProUGUI notChatManagerIsTyping;
   public TextMeshProUGUI inputManagerInputEnabled;
   public TextMeshProUGUI chatManagerIsTyping;
   public TextMeshProUGUI notUtilIsBatch;
   public TextMeshProUGUI utilHasInputField;
   public TextMeshProUGUI inputFieldFocused;
   public TextMeshProUGUI nameInputFieldFocused;
   public TextMeshProUGUI notInputFieldFocused;
   public TextMeshProUGUI notNameInputFieldFocused;
   public TextMeshProUGUI utilIsGeneralInputAllowed;
   public TextMeshProUGUI notHasPanelInLinkedList;
   public TextMeshProUGUI notPanelManagerIsLoading;
   public TextMeshProUGUI notIsWritingMail;
   public TextMeshProUGUI areaManagerHasArea;
   public TextMeshProUGUI notPvpInstructionsPanelIsShowing;

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

      globalPlayerNotDead.text = getBoolString(Global.player != null && !Global.player.isDead());
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
      utilIsGeneralInputAllowed.text = getBoolString(Util.isGeneralInputAllowed());
      notHasPanelInLinkedList.text = getBoolString(!(PanelManager.self.hasPanelInLinkedList() && !PanelManager.self.get(Panel.Type.PvpScoreBoard).isShowing()));
      notPanelManagerIsLoading.text = getBoolString(!PanelManager.isLoading);
      notIsWritingMail.text = getBoolString(!((MailPanel) PanelManager.self.get(Panel.Type.Mail)).isWritingMail());
      areaManagerHasArea.text = getBoolString(Global.player != null && AreaManager.self.hasArea(Global.player.areaKey));
      notPvpInstructionsPanelIsShowing.text = getBoolString(!PvpInstructionsPanel.isShowing);
   }

   private string getBoolString (bool value) {
      return (value) ? "True" : "False";
   }

   #region Private Variables

   #endregion
}
