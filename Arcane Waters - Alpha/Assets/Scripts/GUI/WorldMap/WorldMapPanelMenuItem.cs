using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelMenuItem : MonoBehaviour
{
   #region Public Variables

   // Text caption for the menu item
   public TextMeshProUGUI text;

   // Button control that performs the action for the menu item
   public Button button;

   // Local copy of the spot
   public WorldMapSpot spot;

   // Reference to the menu
   public WorldMapPanelMenu menu;

   #endregion

   private void Start () {
      if (button) {
         button.onClick.AddListener(onMenuItemClicked);
      }
   }

   public void setTitle(string title) {
      this.text.text = title;
   }

   public void onMenuItemClicked () {
      menu.onMenuItemClicked(this);
   }

   public void onMenuItemPointerEnter () {
      menu.onMenuItemPointerEnter(this);
   }

   public void onMenuItemPointerExit () {
      menu.onMenuItemPointerExit(this);
   }

   private void OnDestroy () {
      if (button) {
         button.onClick.RemoveAllListeners();
      }
   }

   #region Private Variables

   #endregion
}
