using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ContextMenuButton : MonoBehaviour
{
   #region Public Variables

   // A delegate used to determine whether a ContextMenuButton should be enabled
   public delegate bool ContextMenuCondition ();

   // Our button
   public Button button;

   // The button text
   public Text buttonText;

   // The condition to enable this button
   public ContextMenuCondition isInteractableCondition;

   // The color to use for the text when the button is enabled
   public Color buttonEnabledTextColor = new Color(1, 1, 1, 1);

   // The color to use for the text when the button is disabled
   public Color buttonDisabledTextColor = new Color(0.6f, 0.6f, 0.6f, 1);

   #endregion

   public void initForAction(string text, UnityAction action, ContextMenuCondition isEnabledCondition = null) {
      // Hide the panel after the button is pressed
      action += PanelManager.self.contextMenuPanel.hide;

      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(action);
      buttonText.text = text;

      this.isInteractableCondition = isEnabledCondition;
   }

   private void Update () {
      if (isInteractableCondition != null) {
         bool isInteractable = isInteractableCondition();
         button.interactable = isInteractable;
         buttonText.color = isInteractable ? buttonEnabledTextColor : buttonDisabledTextColor;
      }
   }

   #region Private Variables

   #endregion
}