using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OwnedMapsPanel : Panel
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

   public void displayFor (OwnedMapManager manager, bool warpAfterSelecting = false) {
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
         cell.setData(manager.getBaseMapName(baseMapKey), () => selectBaseMap(baseMapKey));
      }

      // TODO: highlight the entry, which is owned by user
   }

   public void selectBaseMap (string baseMapKey) {
      // TODO: save in db which base map is selected by the user

      // For testing, set base map as selected
      if (_poManager is POFarmManager) {
         (_poManager as POFarmManager).testSelected = true;
      }

      if (_warpAfterSelecting) {
         PanelManager.self.popPanel();
         Global.player.Cmd_SpawnInNewMap(_poManager.mapTypeAreaKey, string.Empty, Direction.South);
      }
   }

   #region Private Variables

   // Player owned map manager that we are currently showing for
   private OwnedMapManager _poManager;

   // Should we warp into the base map after selecting it
   private bool _warpAfterSelecting;

   #endregion
}
