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
      private enum OrderingType { None = 0, NameAsc = 1, NameDesc = 2, DateAsc = 3, DateDesc = 4, CreatorAsc = 5, CreatorDesc = 6 }

      [SerializeField]
      private MapListEntry entryPref = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Toggle showMyMapsToggle = null;

      [SerializeField, Space(5)]
      private Text nameLabel = null;
      [SerializeField]
      private Text dateLabel = null;
      [SerializeField]
      private Text creatorLabel = null;

      private List<MapListEntry> entries = new List<MapListEntry>();

      private bool showOnlyMyMaps = true;

      private List<Map> loadedMaps;
      private HashSet<int> expandedMaps = new HashSet<int>();
      private OrderingType orderingType = OrderingType.None;

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
                  UI.messagePanel.displayError(error);
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

         maps = order(maps);

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

         updateColumnLabels();
      }

      public void openLatestVersion (Map map) {
         UI.yesNoDialog.display(
            "Opening a map",
            $"Are you sure you want to open map { map.name }?\nAll unsaved progress will be permanently lost.",
             () => openLatestVersionConfirm(map),
             null);
      }

      private IEnumerable<Map> order (IEnumerable<Map> input) {
         switch (orderingType) {
            case OrderingType.NameAsc:
               return input.OrderBy(m => m.name);
            case OrderingType.NameDesc:
               return input.OrderByDescending(m => m.name);
            case OrderingType.DateAsc:
               return input.OrderBy(m => m.createdAt);
            case OrderingType.DateDesc:
               return input.OrderByDescending(m => m.createdAt);
            case OrderingType.CreatorAsc:
               return input.OrderBy(m => m.creatorName);
            case OrderingType.CreatorDesc:
               return input.OrderByDescending(m => m.creatorName);
            default:
               return input;
         }
      }

      private void updateColumnLabels () {
         if (orderingType == OrderingType.NameAsc) {
            nameLabel.text = "Name↑";
         } else if (orderingType == OrderingType.NameDesc) {
            nameLabel.text = "Name↓";
         } else {
            nameLabel.text = "Name";
         }

         if (orderingType == OrderingType.DateAsc) {
            dateLabel.text = "Created At↑";
         } else if (orderingType == OrderingType.DateDesc) {
            dateLabel.text = "Created At↓";
         } else {
            dateLabel.text = "Created At";
         }

         if (orderingType == OrderingType.CreatorAsc) {
            creatorLabel.text = "Creator↑";
         } else if (orderingType == OrderingType.CreatorDesc) {
            creatorLabel.text = "Creator↓";
         } else {
            creatorLabel.text = "Creator";
         }
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
                  UI.messagePanel.displayError(dbError);
               } else if (version == null) {
                  UI.messagePanel.displayError($"Could not find map { name } in the database");
               } else {
                  try {
                     Overlord.instance.applyData(version);
                     Overlord.loadAllRemoteData();
                     hide();
                     UI.mapList.close();
                  } catch (Exception ex) {
                     UI.messagePanel.displayError(ex.ToString());
                  }
               }
            });
         });

         UI.loadingPanel.display("Opening a map", task);
      }

      public void deleteMap (Map map) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a map",
            $"Are you sure you want to delete map {map.name} completely? All versions will be permanently lost.",
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
                  UI.messagePanel.displayError(error);
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

      public void orderByName () {
         if (orderingType == OrderingType.NameAsc) {
            orderingType = OrderingType.None;
         } else if (orderingType == OrderingType.NameDesc) {
            orderingType = OrderingType.NameAsc;
         } else {
            orderingType = OrderingType.NameDesc;
         }

         updateShowedMaps();
      }

      public void orderByDate () {
         if (orderingType == OrderingType.DateAsc) {
            orderingType = OrderingType.None;
         } else if (orderingType == OrderingType.DateDesc) {
            orderingType = OrderingType.DateAsc;
         } else {
            orderingType = OrderingType.DateDesc;
         }

         updateShowedMaps();
      }

      public void orderByCreator () {
         if (orderingType == OrderingType.CreatorAsc) {
            orderingType = OrderingType.None;
         } else if (orderingType == OrderingType.CreatorDesc) {
            orderingType = OrderingType.CreatorAsc;
         } else {
            orderingType = OrderingType.CreatorDesc;
         }

         updateShowedMaps();
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
