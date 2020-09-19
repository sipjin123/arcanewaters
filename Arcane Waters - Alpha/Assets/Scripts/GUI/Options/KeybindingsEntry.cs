using UnityEngine;
using UnityEngine.UI;

public class KeybindingsEntry : ClientMonoBehaviour
{
   #region Public Variables

   // Name of the action
   public Text actionLabel;

   // Buttons for setting keys
   public Button primaryButton;
   public Button secondaryButton;

   // The type of action this entry controls
   public KeyAction action;

   #endregion

   public KeybindingsEntry initialize (KeyBindingsPanel owner, KeyAction action, string title) {
      _owner = owner;
      this.action = action;
      actionLabel.text = title;

      _primaryText = primaryButton.GetComponentInChildren<Text>();
      _secondaryText = secondaryButton.GetComponentInChildren<Text>();

      primaryButton.onClick.AddListener(() => owner.requestUserForKey(this, true));
      secondaryButton.onClick.AddListener(() => owner.requestUserForKey(this, false));

      return this;
   }

   public void setPrimary (KeyCode key) {
      _primaryText.text = key.ToString();
   }

   public void setSecondary (KeyCode key) {
      _secondaryText.text = key.ToString();
   }

   #region Private Variables

   // Panel this entry belongs to
   private KeyBindingsPanel _owner;

   // Labels in buttons for key bindings
   private Text _primaryText;
   private Text _secondaryText;

   #endregion
}
