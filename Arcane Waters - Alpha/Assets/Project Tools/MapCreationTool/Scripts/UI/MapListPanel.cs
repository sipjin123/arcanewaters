using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class MapListPanel : UIPanel
   {
      [SerializeField]
      private MapListEntry entryPref = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Toggle showMyMapsToggle = null;

      private List<MapListEntry> entries = new List<MapListEntry>();

      private bool showOnlyMyMaps = true;

      private void clearEverything () {
         foreach (MapListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();
      }

      public void open () {
         clearEverything();
         show();
         loadList();

         showMyMapsToggle.SetIsOnWithoutNotify(showOnlyMyMaps);
         showMyMapsChanged();
      }

      private void loadList () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<Map> maps = null;
            string error = "";
            try {
               maps = DB_Main.getMaps();
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (maps == null) {
                  UI.errorDialog.display(error);
               } else {
                  foreach (Map map in maps) {
                     MapListEntry entry = Instantiate(entryPref, entryParent);
                     entry.set(map, () => openLatestVersion(map), () => UI.versionListPanel.open(map), () => UI.renamePanel.open(map), () => deleteMap(map));
                     entry.gameObject.SetActive(
                        !showOnlyMyMaps ||
                        entry.target.creatorID == MasterToolAccountManager.self.currentAccountID);
                     entries.Add(entry);
                  }
               }
            });
         });

         UI.loadingPanel.display("Loading maps", task);
      }

      public void openLatestVersion (Map map) {
         UI.yesNoDialog.display(
            "Opening a map",
            $"Are you sure you want to open map { map.name }?\nAll unsaved progress will be permanently lost.",
             () => openLatestVersionConfirm(map),
             null);
      }

      public void openLatestVersionConfirm (Map map) {
         clearEverything();

         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            MapVersion version = null;
            try {
               version = DB_Main.getLatestMapVersionEditor(map);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.errorDialog.display(dbError);
               } else if (version == null) {
                  UI.errorDialog.display($"Could not find map { name } in the database");
               } else {
                  try {
                     Overlord.instance.applyData(version);
                     Overlord.loadAllRemoteData();
                     hide();
                     UI.mapList.close();
                  } catch (Exception ex) {
                     UI.errorDialog.display(ex.ToString());
                  }
               }
            });
         });

         UI.loadingPanel.display("Opening a map", task);
      }

      public void deleteMap (Map map) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a map",
            $"Are you sure you want to delete map {map.name} <b>completely</b>? All versions will be <b>permanently</b> lost.",
            () => deleteMapConfirm(map), null);
      }

      public void deleteMapConfirm (Map map) {
         clearEverything();
         if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapId == map.id) {
            DrawBoard.changeLoadedVersion(null);
         }

         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.deleteMap(map.id);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  UI.errorDialog.display(error);
               } else {
                  open();
               }
            });
         });

         UI.loadingPanel.display("Deleting a Map", task);
      }

      public void showMyMapsChanged () {
         showOnlyMyMaps = showMyMapsToggle.isOn;
         foreach (MapListEntry entry in entries) {
            entry.gameObject.SetActive(
               !showOnlyMyMaps ||
               entry.target.creatorID == MasterToolAccountManager.self.currentAccountID);
         }
      }

      public void close () {
         hide();
      }
   }
}
