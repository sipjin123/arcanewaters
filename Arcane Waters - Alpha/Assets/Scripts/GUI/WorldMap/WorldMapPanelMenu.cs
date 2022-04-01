using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class WorldMapPanelMenu : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create warp menuitems
   public GameObject spotMenuItemPrefab;

   // Prefab used to create waypoint menuitems
   public GameObject waypointMenuItemPrefab;

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

   public void clearWarps () {
      _warpsSpots.Clear();
   }

   public void addWarps (List<WorldMapSpot> spots) {
      foreach (WorldMapSpot spot in spots) {
         _warpsSpots.Add(spot);
      }
   }

   public void createWarpsMenuItems () {
      foreach (WorldMapSpot spot in _warpsSpots) {
         GameObject menuItemGO = Instantiate(spotMenuItemPrefab);
         WorldMapPanelMenuItem menuItem = menuItemGO.GetComponent<WorldMapPanelMenuItem>();
         menuItem.transform.SetParent(menuItemsContainer);
         menuItem.menu = this;
         menuItem.spot = spot;
         menuItem.setTitle(spot.displayName);
         _warpsMenuItems.Add(menuItem);
      }
   }

   public void clearWaypoints () {
      _waypointsSpots.Clear();
   }

   public void addWaypoints (List<WorldMapSpot> spots) {
      foreach (WorldMapSpot spot in spots) {
         _waypointsSpots.Add(spot);
      }
   }

   public void createWaypointMenuItems () {
      foreach (WorldMapSpot spot in _waypointsSpots) {
         GameObject menuItemGO = Instantiate(waypointMenuItemPrefab);
         WorldMapPanelMenuItem menuItem = menuItemGO.GetComponent<WorldMapPanelMenuItem>();
         menuItem.transform.SetParent(menuItemsContainer);
         menuItem.menu = this;
         menuItem.spot = spot;
         menuItem.setTitle(WorldMapManager.self.getDisplayStringFromGeoCoords(WorldMapManager.self.getGeoCoordsFromSpot(spot)));
         _waypointsMenuItems.Add(menuItem);
      }
   }

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
      _currentTabIndex = tabIndex;

      foreach (WorldMapPanelMenuItem menuItem in _waypointsMenuItems) {
         Destroy(menuItem.gameObject);
      }

      foreach (WorldMapPanelMenuItem menuItem in _warpsMenuItems) {
         Destroy(menuItem.gameObject);
      }

      _waypointsMenuItems.Clear();
      _warpsMenuItems.Clear();

      if (_currentTabIndex == 0) {
         createWarpsMenuItems();
         txtTitle.text = warpsTitle;
      } else {
         createWaypointMenuItems();
         txtTitle.text = waypointsTitle;
      }
   }

   public void shift (bool toLeftSide = true) {
      panelContainer.transform.localPosition = new Vector3(toLeftSide ? -255 : 255, panelContainer.transform.localPosition.y);
   }

   public void toggle (bool show, int tabIndex = 0) {
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

   public bool isWaypointMenuItem (WorldMapPanelMenuItem menuItem) {
      return _waypointsSpots.Contains(menuItem.spot);
   }

   public bool isWarpMenuItem (WorldMapPanelMenuItem menuItem) {
      return _warpsSpots.Contains(menuItem.spot);
   }

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

   public int getCurrentTab () {
      return _currentTabIndex;
   }

   #region Private Variables

   // The current set of menu items (warps)
   private List<WorldMapPanelMenuItem> _warpsMenuItems = new List<WorldMapPanelMenuItem>();

   // The current set of menu items (waypoints)
   private List<WorldMapPanelMenuItem> _waypointsMenuItems = new List<WorldMapPanelMenuItem>();

   // The current set of spots (warps)
   private List<WorldMapSpot> _warpsSpots = new List<WorldMapSpot>();

   // The current set of spots (waypoints)
   private List<WorldMapSpot> _waypointsSpots = new List<WorldMapSpot>();

   // The current tab index
   private int _currentTabIndex = 0;

   #endregion
}
