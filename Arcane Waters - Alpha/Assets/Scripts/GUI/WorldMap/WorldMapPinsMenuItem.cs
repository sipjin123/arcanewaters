using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapPinsMenuItem : MonoBehaviour
{
   #region Public Variables

   // Text caption for the menu item
   public TextMeshProUGUI text;

   // Button control that performs the action for the menu item
   public Button button;

   #endregion

   private void Start () {
      if (button) {
         button.onClick.AddListener(onMenuItemClicked);
      }
   }

   public void setPin(WorldMapPanelPinInfo pin) {
      _pin = pin;

      if (text) {
         text.text = pin.displayName;
      }
   }

   public WorldMapPanelPinInfo getPin () {
      return _pin;
   }

   public void setMenu (WorldMapPinsMenu menu) {
      _menu = menu;
   }

   public WorldMapPinsMenu getMenu () {
      return _menu;
   }

   public void onMenuItemClicked () {
      if (_menu == null) {
         return;
      }

      _menu.onMenuItemSelected(this);
   }

   private void OnDestroy () {
      if (button) {
         button.onClick.RemoveAllListeners();
      }
   }

   #region Private Variables

   // Local copy of the pin
   private WorldMapPanelPinInfo _pin;

   // Reference to the menu
   public WorldMapPinsMenu _menu;

   #endregion
}
