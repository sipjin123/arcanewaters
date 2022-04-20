using UnityEngine;
using UnityEngine.UI;

public class MM_WaypointIcon : ClientMonoBehaviour
{
   #region Public Variables

   // The waypoint represented by this icon
   public WorldMapWaypoint waypoint;

   // The tooltip we want for this icon
   public Tooltipped tooltip;

   #endregion

   private void Start () {
      _image = GetComponent<Image>();
      if (_image == null) {
         gameObject.SetActive(false);
      }
   }

   private void Update () {
      if (Global.player == null || waypoint == null) {
         return;
      }

      // Update the tooltip
      tooltip.text = waypoint.displayName;

      // Reposition waypoint icon
      Area currentArea = AreaManager.self.getArea(Global.player.areaKey);
      Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(waypoint.transform, currentArea));
   }

   public string getTooltip () {
      return tooltip.text;
   }

   public Image getImage () {
      if (_image) {
         return _image;
      }
      return _image = GetComponent<Image>();
   }

   public void onHoverEnter () {
      if (tooltip != null && tooltip.text.Length > 0) {
         Minimap.self.tooltipText.text = tooltip.text;
         Minimap.self.toolTipContainer.SetActive(true);
      }
   }

   public void onHoverExit () {
      Minimap.self.toolTipContainer.SetActive(false);
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
