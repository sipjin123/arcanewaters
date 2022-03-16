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

      // Keep the icon in the right position
      Area currentArea = AreaManager.self.getArea(Global.player.areaKey);

      // Physical map size is in range [-5, 5], we need to transform it to minimap space which is [-64, 64]
      const float worldToMapSpaceTransform = 64f / 5f;
      if (currentArea != null) {
         float minimapSpriteWidth = Minimap.self.backgroundImage.sprite.textureRect.width;
         float mapActiveAreaWidth = (Minimap.self.realAreaSize != Vector2Int.zero) ? Minimap.self.realAreaSize.x : minimapSpriteWidth;

         // Some pvp arenas have a playable area that is smaller than the total area size, so we have to calculate it with the 'arena size'
         if (VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
            PvpArenaSize arenaSize = AreaManager.self.getAreaPvpArenaSize(Global.player.areaKey);
            mapActiveAreaWidth = AreaManager.getWidthForPvpArenaSize(arenaSize);
         }

         // Scale transformation based on current map size
         float relativePositionScale = (mapActiveAreaWidth / 64.0f);
         Vector3 relativePosition = waypoint.transform.localPosition * worldToMapSpaceTransform / relativePositionScale;

         // For 64x64 map, there is no minimap translation
         float minimapTranslationScale = (mapActiveAreaWidth - 64.0f) / 64.0f;

         // For pvp arenas, we never want to have minimap translation
         if (VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
            minimapTranslationScale = 0.0f;
         }

         // It is more suited for Minimap class but to avoid race condition and ensure correct calling sequence, it is used here
         if (minimapTranslationScale < 0.0f) {
            Minimap.self.backgroundImage.rectTransform.localPosition = Vector2.zero;
         } else {
            Minimap.self.backgroundImage.rectTransform.localPosition = new Vector2(
               Mathf.Clamp(-relativePosition.x * minimapTranslationScale, -64f * minimapTranslationScale, 64f * minimapTranslationScale),
               Mathf.Clamp(-relativePosition.y * minimapTranslationScale, -64f * minimapTranslationScale, 64f * minimapTranslationScale));
         }
         Util.setLocalXY(this.transform, relativePosition);
      }
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
