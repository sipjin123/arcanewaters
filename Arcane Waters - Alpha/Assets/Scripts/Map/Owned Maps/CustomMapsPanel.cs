using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using MapCreationTool.Serialization;

public class CustomMapsPanel : Panel
{
   #region Public Variables

   // Container, that holds base map entries
   public GridLayoutGroup mapBaseContainer;

   // Prefab of the base map entry
   public BaseMapEntry baseMapEntryPref;

   // Title of the panel
   public Text title;

   #endregion

   public void displayFor (CustomMapManager manager, bool warpAfterSelecting = false) {
      // Show panel if it is not showing already
      PanelManager.self.linkIfNotShowing(type);

      _customMapManager = manager;
      _warpAfterSelecting = warpAfterSelecting;

      // Clear out any old info
      mapBaseContainer.gameObject.DestroyChildren();

      title.text = $"Choose a { manager.typeDisplayName } layout";

      // Create base map entries
      foreach (Map baseMap in manager.getRelatedMaps().Where(m => m.sourceMapId > 0)) {
         BaseMapEntry cell = Instantiate(baseMapEntryPref, mapBaseContainer.transform, false);

         // Get the preview image for the entry
         Sprite sprite = ImageManager.getSprite($"GUI/Map Customization/Preview Images/{ manager.typeDisplayName }_map_{ baseMap.name.ToLower().Replace(" ", "_") }");

         cell.setData(baseMap.displayName, sprite, 0, 0, () => selectBaseMap(baseMap.id));
      }

      // Trigger the tutorial
      if (manager is CustomHouseManager) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenHouseLayoutSelectionPanel);
      } else if (manager is CustomFarmManager) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenFarmLayoutSelectionPanel);
      }

      // TODO: highlight the entry, which is owned by user
   }

   public void selectBaseMap (int baseMapId) {
      D.adminLog("Player has selected map {" + baseMapId + "} as custom map", D.ADMIN_LOG_TYPE.CustomMap);
      Global.player.rpc.Cmd_SetCustomMapBaseMap(_customMapManager.mapTypeAreaKey, baseMapId, _warpAfterSelecting);

      foreach (BaseMapEntry entry in mapBaseContainer.GetComponentsInChildren<BaseMapEntry>()) {
         entry.setInteractable(false);
      }
   }

   public void baseMapUpdated (string customMapKey, int baseMapId) {
      // Ensure we are showing the panel and we are targeting a custom map type that was updated
      if (!isShowing() || !customMapKey.Equals(_customMapManager?.mapTypeAreaKey)) {
         return;
      }

      if (_warpAfterSelecting) {
         PanelManager.self.unlinkPanel();
      } else {
         foreach (BaseMapEntry entry in mapBaseContainer.GetComponentsInChildren<BaseMapEntry>()) {
            entry.setInteractable(true);
         }

         // TODO: update UI to show selected map
      }
   }

   #region Private Variables

   // Custom map manager that we are currently showing for
   private CustomMapManager _customMapManager;

   // Should we warp into the base map after selecting it
   private bool _warpAfterSelecting;

   #endregion
}
