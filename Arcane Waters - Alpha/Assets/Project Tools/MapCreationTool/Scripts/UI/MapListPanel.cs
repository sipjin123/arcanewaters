using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using System.Collections;

namespace MapCreationTool
{
   public class MapListPanel : UIPanel
   {
      public enum OrderingType { None = 0, NameAsc = 1, NameDesc = 2, DateAsc = 3, DateDesc = 4, CreatorAsc = 5, CreatorDesc = 6 }

      [SerializeField]
      private UserMapsEntry entryPref = null;
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

      private List<UserMapsEntry> entries = new List<UserMapsEntry>();
      private List<Map> loadedMaps = new List<Map>();

      public GameObject focusBlocker = default;

      // If the windows is focused
      public bool isFocused = true;

      // Reference to self
      public static MapListPanel self;

      protected override void Awake () {
         base.Awake();
         self = this;
      }

      private void clearEverything () {
         foreach (UserMapsEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();
      }

      public void open () {
         clearEverything();
         show();
         loadList();

         showMyMapsToggle.SetIsOnWithoutNotify(MapListPanelState.self.showOnlyMyMaps);
      }

      private async void loadList () {
         loadedMaps = null;

         try {
            var task = DB_Main.execAsync(DB_Main.getMaps);
            UI.loadingPanel.display("Loading maps", task);
            loadedMaps = await task;
            updateShowedMaps();
         } catch (Exception ex) {
            UI.messagePanel.displayError(ex.Message);
         }
      }

      private void updateShowedMaps () {
         clearEverything();

         if (loadedMaps == null) return;

         IEnumerable<Map> maps = MapListPanelState.self.showOnlyMyMaps
            ? loadedMaps.Where(m => m.creatorID == MasterToolAccountManager.self.currentAccountID)
            : loadedMaps;

         maps = order(maps);

         IEnumerable<Map> groupRoots = maps.Where(m => !(!maps.Any(other => other.sourceMapId == m.id) && maps.Any(other => other.id == m.sourceMapId)));

         var groups = groupRoots.Select(gr => new KeyValuePair<Map, IEnumerable<Map>>(gr,
            maps.Where(m => m.sourceMapId == gr.id && m.sourceMapId != m.id && !maps.Any(other => other.sourceMapId == m.id))));

         var userGroups = groups.GroupBy(g => (g.Key.creatorID, g.Key.creatorName));

         foreach (var userGroup in userGroups) {
            UserMapsEntry entry = Instantiate(entryPref, entryParent);
            entry.set(userGroup);
            entries.Add(entry);
         }

         updateColumnLabels();
      }

      public void openLatestVersion (Map map) {
         UI.yesNoDialog.displayIfMapStateModified(
            "Opening a map",
            $"Are you sure you want to open map { map.name }?\nAll unsaved progress will be permanently lost.",
             () => openLatestVersionConfirm(map),
             null);
      }

      private IEnumerable<Map> order (IEnumerable<Map> input) {
         switch (MapListPanelState.self.orderingType) {
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
         if (MapListPanelState.self.orderingType == OrderingType.NameAsc) {
            nameLabel.text = "Name↑";
         } else if (MapListPanelState.self.orderingType == OrderingType.NameDesc) {
            nameLabel.text = "Name↓";
         } else {
            nameLabel.text = "Name";
         }

         if (MapListPanelState.self.orderingType == OrderingType.DateAsc) {
            dateLabel.text = "Created At↑";
         } else if (MapListPanelState.self.orderingType == OrderingType.DateDesc) {
            dateLabel.text = "Created At↓";
         } else {
            dateLabel.text = "Created At";
         }

         if (MapListPanelState.self.orderingType == OrderingType.CreatorAsc) {
            creatorLabel.text = "Creator↑";
         } else if (MapListPanelState.self.orderingType == OrderingType.CreatorDesc) {
            creatorLabel.text = "Creator↓";
         } else {
            creatorLabel.text = "Creator";
         }
      }

      void OnApplicationFocus (bool hasFocus) {
         focusBlocker.SetActive(!hasFocus);
         StartCoroutine(CO_ResetFocus(hasFocus));
      }

      private IEnumerator CO_ResetFocus (bool hasFocus) {
         if (hasFocus) {
            yield return new WaitForSeconds(.5f);
            isFocused = true;
         } else {
            isFocused = false;        
         }
      }

      public void openLatestVersionConfirm (Map map) {
         if (Overlord.isOpenWorldMapKey(map.name, out int mapX, out int mapY)) {
            UI.yesNoDialog.display(
                        "Adjacent maps",
                        $"Would you like to open reference images for adjacent open world maps? This process would take a while.",
                         () => StartCoroutine(openLatestVersionConfirmWithAdjacent(map, mapX, mapY)),
                         () => openLatestVersionConfirmRegular(map));
         } else {
            openLatestVersionConfirmRegular(map);
         }
      }

      private IEnumerator openLatestVersionConfirmWithAdjacent (Map map, int mapX, int mapY) {
         clearEverything();
         UI.loadingPanel.display("Fetching adjacency info");
         yield return new WaitForEndOfFrame();

         // Find info about adjacent maps
         (string areaKey, Map map, Texture2D screenshot)[] adjacent = null;
         try {
            adjacent = Overlord.createAdjacentMapsArray(mapX, mapY);
            for (int i = 0; i < adjacent.Length; i++) {
               if (adjacent[i].areaKey == null) {
                  continue;
               }

               foreach (Map m in loadedMaps) {
                  if (adjacent[i].areaKey.Equals(m.name)) {
                     adjacent[i].map = m;
                     break;
                  }
               }

               if (adjacent[i].map == null) {
                  throw new Exception("Could not find information about adjacent map " + adjacent[i].areaKey);
               }
            }
         } catch (Exception ex) {
            UI.loadingPanel.close();
            UI.messagePanel.displayError(ex.ToString());
            close();
            yield break;
         }

         // Load maps 1 by 1 and render their screen shot
         for (int i = 0; i < adjacent.Length; i++) {
            if (adjacent[i].map == null) {
               continue;
            }

            // Download the map data
            UI.loadingPanel.display($"Downloading and rendering the latest version for adjacent map {adjacent[i].map.name}");
            string error = null;
            MapVersion version = null;
            UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               try {
                  version = DB_Main.getLatestMapVersionEditor(adjacent[i].map);
               } catch (Exception ex) {
                  error = ex.Message;
               }
            });

            // Wait for download to complete
            while (!task.HasEnded) {
               yield return new WaitForEndOfFrame();
            }

            if (error != null) {
               UI.loadingPanel.close();
               UI.messagePanel.displayError(error);
               close();
               yield break;
            } else if (version == null) {
               UI.loadingPanel.close();
               UI.messagePanel.displayError($"Could not find map { name } in the database");
               close();
               yield break;
            }

            // Apply map data and render
            Overlord.instance.applyData(version);
            yield return new WaitForEndOfFrame();
            adjacent[i].screenshot = ScreenRecorder.recordTexture();
            yield return new WaitForEndOfFrame();
         }

         // We should now have all adjacent screen shots, load the target map itself
         openLatestVersionConfirmRegular(map, adjacent);
      }

      private void openLatestVersionConfirmRegular (Map map, (string areaKey, Map map, Texture2D screenshot)[] adjacent = null) {
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

                     // Set adjacent map images if we have such a thing
                     if (adjacent != null) {
                        Overlord.instance.setAdjacentMapImages(adjacent);
                     }

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

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && map.creatorID != MasterToolAccountManager.self.currentAccountID) {
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

      public void orderByName () {
         if (MapListPanelState.self.orderingType == OrderingType.NameAsc) {
            MapListPanelState.setOrderingType(OrderingType.None);
         } else if (MapListPanelState.self.orderingType == OrderingType.NameDesc) {
            MapListPanelState.setOrderingType(OrderingType.NameAsc);
         } else {
            MapListPanelState.setOrderingType(OrderingType.NameDesc);
         }

         updateShowedMaps();
      }

      public void orderByDate () {
         if (MapListPanelState.self.orderingType == OrderingType.DateAsc) {
            MapListPanelState.setOrderingType(OrderingType.None);
         } else if (MapListPanelState.self.orderingType == OrderingType.DateDesc) {
            MapListPanelState.setOrderingType(OrderingType.DateAsc);
         } else {
            MapListPanelState.setOrderingType(OrderingType.DateDesc);
         }

         updateShowedMaps();
      }

      public void orderByCreator () {
         if (MapListPanelState.self.orderingType == OrderingType.CreatorAsc) {
            MapListPanelState.setOrderingType(OrderingType.None);
         } else if (MapListPanelState.self.orderingType == OrderingType.CreatorDesc) {
            MapListPanelState.setOrderingType(OrderingType.CreatorAsc);
         } else {
            MapListPanelState.setOrderingType(OrderingType.CreatorDesc);
         }

         updateShowedMaps();
      }

      public void showMyMapsChanged () {
         MapListPanelState.setShowOnlyMyMaps(showMyMapsToggle.isOn);
         updateShowedMaps();
      }

      public void close () {
         hide();
      }
   }
}
