using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class WorldMapPanelMenu : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create menuitems
   public GameObject menuItemPrefab;

   // Reference to the control that will hold the menuitems
   public Transform menuItemsContainer;

   // Reference to the canvas group
   public CanvasGroup canvasGroup;

   // Reference to the Tabs
   public AdminPanelTabs tabs;

   // Reference to the Panel container
   public RectTransform panelContainer;

   // Reference to the Title text control
   public Text txtTitle;

   // Warps Title
   public string warpsTitle;

   // Waypoints Title
   public string waypointsTitle;

   #endregion

   private void Start () {
      initializeTabs();
   }

   #region Menu Items

   public void clearMenuItems () {
      foreach (WorldMapPanelMenuItem menuItem in _menuItems) {
         Destroy(menuItem.gameObject);
      }

      _menuItems.Clear();
   }

   public void addMenuItems (IEnumerable<WorldMapSpot> spots) {
      foreach (WorldMapSpot spot in spots) {
         GameObject menuItemGO = Instantiate(menuItemPrefab);
         WorldMapPanelMenuItem menuItem = menuItemGO.GetComponent<WorldMapPanelMenuItem>();
         menuItem.transform.SetParent(menuItemsContainer);
         menuItem.menu = this;
         menuItem.spot = spot;

         // Temporarily hide the action buttons
         menuItem.warpActionButton.gameObject.SetActive(false);
         menuItem.waypointDeleteButton.gameObject.SetActive(false);

         // Set title of the menu item
         if (menuItem.isDestination()) {
            menuItem.setTitle(spot.displayName);
            menuItem.warpActionButton.gameObject.SetActive(spot.type == WorldMapSpot.SpotType.Warp);
         } else if (menuItem.isWaypoint()) {
            menuItem.setTitle(WorldMapManager.self.getDisplayStringFromGeoCoords(WorldMapManager.self.getGeoCoordsFromSpot(spot)));
            menuItem.waypointDeleteButton.gameObject.SetActive(true);
         }

         // Ensure the title is valid
         if (Util.isEmpty(menuItem.getTitle())) {
            menuItem.setTitle(menuItem.spot.type.ToString());
         }

         // Register menu item
         _menuItems.Add(menuItem);
      }
   }

   #endregion

   #region Tabs

   private void initializeTabs () {
      if (tabs == null) {
         return;
      }

      tabs.onTabPressed.RemoveAllListeners();
      tabs.onTabPressed.AddListener(onTabPressed);

      // Switch to the first tab
      tabs.performTabPressed(0);
   }

   private void onTabPressed (int tabIndex) {
      // Store the new index
      _currentTabIndex = tabIndex;

      // Set the title
      if (_currentTabIndex == 0) {
         txtTitle.text = warpsTitle;
      } else {
         txtTitle.text = waypointsTitle;
      }

      // Filter the menu items
      filterMenuItems();
   }

   private void filterMenuItems () {
      foreach (WorldMapPanelMenuItem menuItem in _menuItems) {
         if (_currentTabIndex == 0) {
            menuItem.gameObject.SetActive(menuItem.isDestination());
         } else {
            menuItem.gameObject.SetActive(menuItem.isWaypoint());
         }
      }
   }

   public int getCurrentTab () {
      return _currentTabIndex;
   }

   #endregion

   public void shift (bool toLeftSide = true) {
      panelContainer.transform.localPosition = new Vector3(toLeftSide ? -255 : 255, panelContainer.transform.localPosition.y);
   }

   public void show (int tabIndex = 0) {
      if (canvasGroup == null) {
         return;
      }

      canvasGroup.alpha = 1.0f;
      canvasGroup.interactable = true;
      canvasGroup.blocksRaycasts = true;

      tabs.performTabPressed(tabIndex);
   }

   public void hide () {
      if (canvasGroup == null) {
         return;
      }

      canvasGroup.alpha = 0.0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;

      WorldMapPanel.self.onMenuDismissed();
   }

   public bool isShowing () {
      return canvasGroup.alpha > 0.1f;
   }

   #region Events

   public void onMenuItemClicked (WorldMapPanelMenuItem menuItem) {
      WorldMapSpot spot = menuItem.spot;
      WorldMapPanel.self.onMenuItemPressed(menuItem);
   }

   public void onMenuItemPointerEnter (WorldMapPanelMenuItem menuItem) {
      WorldMapPanel.self.onMenuItemPointerEnter(menuItem);
   }

   public void onMenuItemPointerExit (WorldMapPanelMenuItem menuItem) {
      WorldMapPanel.self.onMenuItemPointerExit(menuItem);
   }

   #endregion

   #region Private Variables

   // The current set of menu items
   private List<WorldMapPanelMenuItem> _menuItems = new List<WorldMapPanelMenuItem>();

   // The current tab index
   private int _currentTabIndex = 0;

   #endregion
}
