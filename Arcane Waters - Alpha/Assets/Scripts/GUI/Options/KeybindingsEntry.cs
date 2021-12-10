using UnityEngine.UI;

public class KeybindingsEntry : ClientMonoBehaviour
{
   #region Public Variables

   // Name of the action
   public Text actionLabel;

   // Buttons for setting keys
   public Button primaryButton;
   public Button secondaryButton;

   // The action this entry controls
   public KeyBindingsPanel.RebindAction action;

   #endregion

   public KeybindingsEntry initialize (KeyBindingsPanel owner, KeyBindingsPanel.RebindAction action) {
      _owner = owner;
      this.action = action;

      _primaryText = primaryButton.GetComponentInChildren<Text>();
      _secondaryText = secondaryButton.GetComponentInChildren<Text>();

      actionLabel.text = action.name;
      refreshTexts();
      
      primaryButton.onClick.AddListener(() => rebind(InputManager.BindingType.Keyboard, InputManager.BindingId.KeyboardPrimary));
      secondaryButton.onClick.AddListener(() => rebind(InputManager.BindingType.Keyboard, InputManager.BindingId.KeyboardSecondary));

      return this;
   }

   private void rebind (InputManager.BindingType bindingType, InputManager.BindingId bindingId) {
      _owner.inputBlocker.SetActive(true);
      InputManager.self.rebindAction(
         action.inputAction, 
         bindingType,
         bindingId,
         () => {
            refreshTexts();
            _owner.inputBlocker.SetActive(false);
         });
   }

   public void refreshTexts () {
      _primaryText.text = action.inputAction.bindings[(int)InputManager.BindingId.KeyboardPrimary].effectivePath.Replace("<Keyboard>/", "");
      _secondaryText.text = action.inputAction.bindings[(int)InputManager.BindingId.KeyboardSecondary].effectivePath.Replace("<Keyboard>/", "");
   }

   #region Private Variables

   // Panel this entry belongs to
   private KeyBindingsPanel _owner;

   // Labels in buttons for key bindings
   private Text _primaryText;
   private Text _secondaryText;

   #endregion
}
