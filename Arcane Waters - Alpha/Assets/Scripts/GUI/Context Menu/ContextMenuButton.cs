using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ContextMenuButton : MonoBehaviour
{
   #region Public Variables

   // Our button
   public Button button;

   // The button text
   public Text buttonText;

   #endregion

   public void initForAction(string text, UnityAction action) {
      // Hide the panel after the button is pressed
      action += PanelManager.self.contextMenuPanel.hide;

      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(action);
      buttonText.text = text;
   }

   #region Private Variables

   #endregion
}