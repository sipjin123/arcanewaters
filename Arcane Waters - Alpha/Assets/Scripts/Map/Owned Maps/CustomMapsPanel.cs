using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class CustomMapsPanel : Panel
{
   #region Public Variables

   // Container, that holds base map entries
   public GridLayoutGroup mapBaseContainer;

   // Prefab of the base map entry
   public BaseMapEntry baseMapEntryPref;

   // Title of the panel
   public Text title;

   // Text, which displays a hint for the next user action
   public Text userActionText;

   #endregion

   public void displayFor (CustomMapManager manager, bool warpAfterSelecting = false) {
      // Show panel if it is not showing already
      PanelManager.self.pushIfNotShowing(type);

      _poManager = manager;
      _warpAfterSelecting = warpAfterSelecting;

      // Clear out any old info
      mapBaseContainer.gameObject.DestroyChildren();

      title.text = manager.typeDisplayName;
      userActionText.text = $"Choose a space for your { manager.typeDisplayName }:";

      // Create base map entries
      foreach (string baseMapKey in manager.getAllBaseMapKeys()) {
         BaseMapEntry cell = Instantiate(baseMapEntryPref, mapBaseContainer.transform, false);
         cell.setData(manager.getBaseMapDisplayName(baseMapKey), () => selectBaseMap(AreaManager.self.getAreaId(baseMapKey)));
      }

      // TODO: highlight the entry, which is owned by user
   }

   public void selectBaseMap (int baseMapId) {
      Global.player.rpc.Cmd_SetCustomMapBaseMap(_poManager.mapTypeAreaKey, baseMapId, _warpAfterSelecting);
   }

   public void baseMapUpdated (string customMapKey, int baseMapId) {
      // Ensure we are showing the panel and we are targeting a custom map type that was updated
      if (!isShowing() || !customMapKey.Equals(_poManager?.mapTypeAreaKey)) {
         return;
      }

      if (_warpAfterSelecting) {
         PanelManager.self.popPanel();
      } else {
         // TODO: update UI to show selected map
      }
   }

   #region Private Variables

   // Player owned map manager that we are currently showing for
   private CustomMapManager _poManager;

   // Should we warp into the base map after selecting it
   private bool _warpAfterSelecting;

   #endregion
}
