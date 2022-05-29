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

   // Reference to the action button used for Warps
   public Button warpActionButton;

   // Reference to the action button used to delete waypoints
   public Button waypointDeleteButton;

   #endregion

   private void Start () {
      if (warpActionButton) {
         warpActionButton.onClick.AddListener(onMenuItemClicked);
      }
      
      if (waypointDeleteButton) {
         waypointDeleteButton.onClick.AddListener(onMenuItemClicked);
      }
   }

   public void setTitle(string title) {
      this.text.text = title;
   }

   public string getTitle () {
      return this.text.text;
   }

   #region Events

   public void onMenuItemClicked () {
      menu.onMenuItemClicked(this);
   }

   public void onMenuItemPointerEnter () {
      menu.onMenuItemPointerEnter(this);
   }

   public void onMenuItemPointerExit () {
      menu.onMenuItemPointerExit(this);
   }

   #endregion

   public bool isWaypoint () {
      return spot.type == WorldMapSpot.SpotType.Waypoint;
   }

   public bool isDestination () {
      return spot.type != WorldMapSpot.SpotType.None && spot.type != WorldMapSpot.SpotType.Waypoint && spot.type != WorldMapSpot.SpotType.Player;
   }

   public bool isPlayerPin () {
      return spot.type == WorldMapSpot.SpotType.Player;
   }

   private void OnDestroy () {
      if (warpActionButton) {
         warpActionButton.onClick.RemoveAllListeners();
      }
      
      if (waypointDeleteButton) {
         waypointDeleteButton.onClick.RemoveAllListeners();
      }
   }

   #region Private Variables

   #endregion
}
