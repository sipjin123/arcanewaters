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

   public KeybindingsEntry initialize (KeyBindingsPanel owner, KeyBindingsPanel.RebindAction action, bool isKeyboard) {
      _owner = owner;
      _isKeyboard = isKeyboard;
      this.action = action;

      _primaryText = primaryButton.GetComponentInChildren<Text>();
      _secondaryText = secondaryButton.GetComponentInChildren<Text>();

      actionLabel.text = action.name;
      refreshTexts();
      
      primaryButton.onClick.AddListener(() => rebind(
         _isKeyboard ? InputManager.BindingType.Keyboard : InputManager.BindingType.Gamepad, 
         _isKeyboard ? InputManager.BindingId.KeyboardPrimary : InputManager.BindingId.GamepadPrimary
      ));
      secondaryButton.onClick.AddListener(() => rebind(
         _isKeyboard ? InputManager.BindingType.Keyboard : InputManager.BindingType.Gamepad, 
         _isKeyboard ? InputManager.BindingId.KeyboardSecondary : InputManager.BindingId.GamepadSecondary
      ));

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
      _primaryText.text = action.inputAction.bindings[(int)(_isKeyboard ? InputManager.BindingId.KeyboardPrimary : InputManager.BindingId.GamepadPrimary)].effectivePath;
      _primaryText.text = _primaryText.text.Replace("<Keyboard>/", "").Replace("<Gamepad>/", "");
      _secondaryText.text = action.inputAction.bindings[(int)(_isKeyboard ? InputManager.BindingId.KeyboardSecondary : InputManager.BindingId.GamepadSecondary)].effectivePath;
      _secondaryText.text = _secondaryText.text.Replace("<Keyboard>/", "").Replace("<Gamepad>/", "");
   }

   #region Private Variables

   // Panel this entry belongs to
   private KeyBindingsPanel _owner;
   private bool _isKeyboard;

   // Labels in buttons for key bindings
   private Text _primaryText;
   private Text _secondaryText;

   #endregion
}
