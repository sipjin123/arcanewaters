using UnityEngine;
using System.Collections.Generic;

public class WorldMapPinsMenu : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create menuitems
   public GameObject menuItemPrefab;

   // The control that will hold the menuitems
   public Transform menuItemsContainer;

   // Reference to the canvas group
   public CanvasGroup canvasGroup;

   #endregion

   public void clear () {
      foreach (WorldMapPinsMenuItem menuItem in _menuItems) {
         Destroy(menuItem.gameObject);
      }

      _menuItems.Clear();
   }

   public void add (List<WorldMapPanelPinInfo> pins) {
      foreach (WorldMapPanelPinInfo pin in pins) {
         GameObject menuItemGO = Instantiate(menuItemPrefab);
         WorldMapPinsMenuItem menuItem = menuItemGO.GetComponent<WorldMapPinsMenuItem>();
         menuItem.transform.SetParent(menuItemsContainer);
         menuItem.setMenu(this);
         menuItem.setPin(pin);
         _menuItems.Add(menuItem);
      }
   }

   public void toggle (bool show) {
      if (canvasGroup == null) {
         return;
      }

      canvasGroup.alpha = show ? 1.0f : 0.0f;
      canvasGroup.interactable = show;
      canvasGroup.blocksRaycasts = show;
   }

   public void onMenuItemSelected (WorldMapPinsMenuItem menuItem) {
      WorldMapPanel.self.onMenuItemSelected(menuItem);
   }

   #region Private Variables

   // The current set of menu items
   private List<WorldMapPinsMenuItem> _menuItems = new List<WorldMapPinsMenuItem>();

   #endregion
}
