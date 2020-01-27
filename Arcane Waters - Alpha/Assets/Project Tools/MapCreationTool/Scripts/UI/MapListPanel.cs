using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapListPanel : UIPanel
   {
      [SerializeField]
      private MapListEntry entryPref = null;
      [SerializeField]
      private MapListEntry versionListEntry = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Text secondTitleText = null;
      [SerializeField]
      private Button backButton = null;
      [SerializeField]
      private Text loadingText = null;
      [SerializeField]
      private Text errorText = null;

      private List<MapListEntry> entries = new List<MapListEntry>();

      private void clearEverything () {
         foreach (MapListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();

         secondTitleText.text = "";
         loadingText.text = "";
         errorText.text = "";
         backButton.gameObject.SetActive(false);
      }

      public void open () {
         clearEverything();
         setLoadingText();
         show();
         loadList();
      }

      private void loadList () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapDTO> maps = null;
            string error = "";
            try {
               maps = DB_Main.getMapDataDescriptions();
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               if (maps == null) {
                  errorText.text = error;
               } else {
                  secondTitleText.text = $"Map count - {maps.Count}";
                  foreach (MapDTO map in maps) {
                     MapListEntry entry = Instantiate(entryPref, entryParent);
                     entry.set(map, openVersionList, () => openMap(map.name));
                     entries.Add(entry);
                  }
               }
            });
         });
      }

      public void openMap (string name, int? version = null) {
         UI.yesNoDialog.display(
            "Opening a map",
            $"Are you sure you want to open map {name}?\nAll unsaved progress will be permanently lost.",
             () => openMapConfirm(name, version),
             null);
      }

      public void openMapConfirm (string name, int? version = null) {
         clearEverything();
         setLoadingText();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            MapDTO map = null;
            string dbError = null;
            try {
               map = version == null
                  ? DB_Main.getEditorMapData(name)
                  : DB_Main.getEditorMapData(name, version.Value);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  loadingText.text = "";
                  errorText.text = dbError;
               } else if (map == null) {
                  loadingText.text = "";
                  errorText.text = $"Could not find map {name} in the database";
               } else {
                  try {
                     Overlord.instance.applyFileData(map.editorData, map);
                     hide();
                  } catch (Exception ex) {
                     loadingText.text = "";
                     errorText.text = ex.Message;
                  }
               }
            });
         });
      }

      public void openVersionList (string name) {
         clearEverything();
         setLoadingText();
         show();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapDTO> maps = null;
            string error = "";
            try {
               maps = DB_Main.getMapVersionsDescriptions(name);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               backButton.gameObject.SetActive(true);
               if (maps == null) {
                  errorText.text = error;
               } else {
                  secondTitleText.text = $"Map {name} has {maps.Count} versions";
                  foreach (MapDTO map in maps) {
                     MapListEntry entry = Instantiate(versionListEntry, entryParent);
                     entry.set(map, (n) => publishVersion(map), () => openMap(map.name, map.version));
                     if (map.liveVersion != null && map.liveVersion.Value == map.version) {
                        entry.setAsLiveMap();
                     }
                     entries.Add(entry);
                  }
               }
            });
         });
      }

      public void publishVersion (MapDTO map) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }
         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }
         UI.yesNoDialog.display(
            "Publishing a map",
            $"Are you sure you want to publish version {map.version} of map {map.name}?\nA published version will <b>immediately</b> become available for the users.",
             () => publishVersionConfirm(map),
             null);
      }

      public void publishVersionConfirm (MapDTO map) {
         clearEverything();
         setLoadingText();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.setLiveMapVersion(map, MasterToolAccountManager.self.currentAccountID);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  loadingText.text = "";
                  errorText.text = error;
               } else {
                  try {
                     hide();
                  } catch (Exception ex) {
                     loadingText.text = "";
                     errorText.text = ex.Message;
                  }
               }
            });
         });
      }

      public void close () {
         hide();
      }

      private void setLoadingText () {
         loadingText.text = "Loading...";
      }
   }
}
