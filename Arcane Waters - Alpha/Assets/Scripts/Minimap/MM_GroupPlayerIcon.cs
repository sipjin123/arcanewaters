using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_GroupPlayerIcon : MM_Icon {
   #region Public Variables

   // Associated player entity
   public NetEntity player;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (Global.player == null || player == null || Global.player.voyageGroupId == -1 || player.voyageGroupId == -1 
         || Global.player.voyageGroupId != player.voyageGroupId || Global.player.areaKey != player.areaKey) {
         gameObject.SetActive(false);
         Destroy(this.gameObject);
         return;
      }

      // Keep the icon in the right position
      if (Global.player != null) {
         Area currentArea = AreaManager.self.getArea(Global.player.areaKey);
         if (currentArea != null) {
            // Keep the icon in the right position
            Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(player.transform, currentArea));
         }
      }
   }

   public void onHoverBegin () {
      Minimap.self.displayIconInfo(player.entityName);
   }

   public void onHoverEnd () {
      Minimap.self.disableIconInfo();
   }

   #region Private Variables

   #endregion
}
