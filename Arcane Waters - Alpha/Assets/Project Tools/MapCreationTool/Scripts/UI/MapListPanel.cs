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

      private List<Map> loadedMaps;
      private HashSet<int> expandedMaps = new HashSet<int>();

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
      }

      private void loadList () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            loadedMaps = null;
            string error = "";
            try {
               loadedMaps = DB_Main.getMaps();
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (loadedMaps == null) {
                  UI.errorDialog.display(error);
               } else {
                  updateShowedMaps();
               }
            });
         });

         UI.loadingPanel.display("Loading maps", task);
      }

      private void updateShowedMaps () {
         clearEverything();

         if (loadedMaps == null) return;

         IEnumerable<Map> maps = showOnlyMyMaps
            ? loadedMaps.Where(m => m.creatorID == MasterToolAccountManager.self.currentAccountID)
            : loadedMaps;

         IEnumerable<Map> groupRoots = maps.Where(m => !(!maps.Any(other => other.sourceMapId == m.id) && maps.Any(other => other.id == m.sourceMapId)));

         var groups = groupRoots.Select(gr => new KeyValuePair<Map, IEnumerable<Map>>(gr,
            maps.Where(m => m.sourceMapId == gr.id && m.sourceMapId != m.id && !maps.Any(other => other.sourceMapId == m.id))));

         foreach (var group in groups) {
            MapListEntry entry = Instantiate(entryPref, entryParent);
            entry.set(group.Key, null);
            entries.Add(entry);
            bool expandable = group.Value.Count() > 0;
            bool expanded = expandedMaps.Contains(group.Key.id);
            entry.setExpandable(expandable, false);
            if (expandable) {
               entry.setExpanded(expanded);
            }

            foreach (Map child in group.Value) {
               MapListEntry cEntry = Instantiate(entryPref, entryParent);
               cEntry.set(child, group.Key.id);
               entries.Add(cEntry);
               cEntry.setExpandable(false, true);
               cEntry.gameObject.SetActive(expanded);
            }
         }
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
               Overlord.loadAllRemoteData();
            });
         });

         UI.loadingPanel.display("Deleting a Map", task);
      }

      public void toggleExpandMap (Map map) {
         if (expandedMaps.Contains(map.id)) {
            expandedMaps.Remove(map.id);
         } else {
            expandedMaps.Add(map.id);
         }

         bool expand = expandedMaps.Contains(map.id);
         entries.FirstOrDefault(e => e.target.id == map.id)?.setExpanded(expand);

         foreach (MapListEntry entry in entries) {
            if (entry.childOf != null && entry.childOf.Value == map.id) {
               entry.gameObject.SetActive(expand);
            }
         }
      }

      public void showMyMapsChanged () {
         showOnlyMyMaps = showMyMapsToggle.isOn;
         updateShowedMaps();
      }

      public void close () {
         hide();
      }
   }
}
