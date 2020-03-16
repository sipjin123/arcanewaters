using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class VersionListPanel : UIPanel
   {
      [SerializeField]
      private VersionListEntry entryPref = null;
      [SerializeField]
      private Transform entryParent = null;
      [SerializeField]
      private Text secondTitleText = null;
      [SerializeField]
      private Text loadingText = null;
      [SerializeField]
      private Text errorText = null;

      private List<VersionListEntry> entries = new List<VersionListEntry>();

      private void clearEverything () {
         foreach (VersionListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();

         secondTitleText.text = "";
         loadingText.text = "";
         errorText.text = "";
      }

      public void open (Map map) {
         clearEverything();
         setLoadingText();
         show();
         loadList(map);
      }

      public void loadList (Map map) {
         clearEverything();
         setLoadingText();
         show();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapVersion> mapVersions = null;
            string error = "";
            try {
               mapVersions = DB_Main.getMapVersions(map);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadingText.text = "";
               if (mapVersions == null) {
                  errorText.text = error;
               } else {
                  secondTitleText.text = map.name;
                  foreach (MapVersion mapVersion in mapVersions) {
                     VersionListEntry entry = Instantiate(entryPref, entryParent);
                     Action onDelete = null;
                     if (mapVersions.Count > 1) {
                        onDelete = () => deleteMapVersion(mapVersion);
                     }
                     entry.set(mapVersion, () => publishVersion(mapVersion), onDelete, () => openMapVersion(mapVersion));
                     entries.Add(entry);
                  }
               }
            });
         });
      }

      public void openMapVersion (MapVersion mapVersion) {
         UI.yesNoDialog.display(
            "Opening a map",
            $"Are you sure you want to open map {mapVersion.map.name}?\nAll unsaved progress will be permanently lost.",
             () => openMapConfirm(mapVersion),
             null);
      }

      public void openMapConfirm (MapVersion mapVersion) {
         clearEverything();
         setLoadingText();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            string editorData = null;
            try {
               editorData = DB_Main.getMapVersionEditorData(mapVersion);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  loadingText.text = "";
                  errorText.text = dbError;
               } else if (editorData == null) {
                  loadingText.text = "";
                  errorText.text = $"Could not find map {name} in the database";
               } else {
                  try {
                     mapVersion.editorData = editorData;
                     Overlord.instance.applyData(mapVersion);
                     Overlord.loadAllRemoteData();
                     hide();
                     UI.mapList.close();
                  } catch (Exception ex) {
                     loadingText.text = "";
                     errorText.text = ex.Message;
                  }
               }
            });
         });
      }

      public void publishVersion (MapVersion mapVersion) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }
         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && mapVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }
         UI.yesNoDialog.display(
            "Publishing a map",
            $"Are you sure you want to publish version {mapVersion.version} of map {mapVersion.map.name}?\nA published version will <b>immediately</b> become available for the users.",
             () => publishVersionConfirm(mapVersion),
             null);
      }

      public void publishVersionConfirm (MapVersion mapVersion) {
         clearEverything();
         setLoadingText();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.setLiveMapVersion(mapVersion);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  loadingText.text = "";
                  errorText.text = error;
               } else {
                  UI.mapList.open();
                  hide();
               }
            });
         });
      }

      public void deleteMapVersion (MapVersion version) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin && version.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a map version",
            $"Are you sure you want to delete map {version.map.name} version {version.version}?",
            () => deleteMapVersionConfirm(version), null);
      }

      public void deleteMapVersionConfirm (MapVersion version) {
         clearEverything();
         setLoadingText();
         if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.CompareTo(version) == 0) {
            DrawBoard.changeLoadedVersion(null);
         }
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.deleteMapVersion(version);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  loadingText.text = "";
                  errorText.text = error;
               } else {
                  UI.mapList.open();
                  open(version.map);
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
