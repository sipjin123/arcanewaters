using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

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

      private List<VersionListEntry> entries = new List<VersionListEntry>();

      private void clearEverything () {
         foreach (VersionListEntry entry in entries) {
            Destroy(entry.gameObject);
         }
         entries.Clear();

         secondTitleText.text = "";
      }

      public void open (Map map) {
         clearEverything();
         show();
         loadList(map);
      }

      public void loadList (Map map) {
         clearEverything();
         show();

         var task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapVersion> mapVersions = null;
            string error = "";
            try {
               mapVersions = DB_Main.getMapVersions(map);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (mapVersions == null) {
                  UI.messagePanel.displayError(error);
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

         UI.loadingPanel.display("Loading map versions", task);
      }

      public void openMapVersion (MapVersion mapVersion) {
         UI.yesNoDialog.displayIfMapStateModified(
            "Opening a map",
            $"Are you sure you want to open map {mapVersion.map.name}?\nAll unsaved progress will be permanently lost.",
             () => openMapConfirm(mapVersion),
             null);
      }

      public void openMapConfirm (MapVersion mapVersion) {
         clearEverything();
         var task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            string editorData = null;
            try {
               editorData = DB_Main.getMapVersionEditorData(mapVersion);
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.messagePanel.displayError(dbError);
               } else if (editorData == null) {
                  UI.messagePanel.displayError($"Could not find map {name} in the database");
               } else {
                  try {
                     mapVersion.editorData = editorData;
                     Overlord.instance.applyData(mapVersion);
                     Overlord.loadAllRemoteData();
                     hide();
                     UI.mapList.close();
                  } catch (Exception ex) {
                     UI.messagePanel.displayError(ex.Message);
                  }
               }
            });
         });

         UI.loadingPanel.display("Loading map", task);
      }

      public void publishVersion (MapVersion mapVersion) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }
         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && mapVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }
         UI.yesNoDialog.display(
            "Publishing a map",
            $"Are you sure you want to publish version {mapVersion.version} of map {mapVersion.map.name}?\nA published version will immediately become available for the users.",
             () => publishVersionConfirm(mapVersion),
             null);
      }

      public void publishVersionConfirm (MapVersion mapVersion) {
         clearEverything();
         var task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               PublishedVersionChange change = DB_Main.setLiveMapVersion(mapVersion);

               // Create a Discord report in the background
               if (change.currentVersion != change.previousVersion) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     createDiscordPublishReport(change);
                  });
               }
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  UI.messagePanel.displayError(error);
               } else {
                  UI.mapList.open();
                  hide();
               }
            });
         });

         UI.loadingPanel.display("Publishing a map", task);
      }

      private void createDiscordPublishReport (PublishedVersionChange change) {
         var task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<MapChangeComment> allComments = new List<MapChangeComment>();

            int from = Math.Min(change.currentVersion, change.previousVersion);
            int to = Math.Max(change.currentVersion, change.previousVersion);

            for (int i = from + 1; i <= to; i++) {
               List<MapChangeComment> comments = DB_Main.getVersionChangeComments(change.mapId, i);
               allComments.AddRange(comments);
            }

            if (change.previousVersion > change.currentVersion) {
               // If a previous version was published, note that these changes are reverted
               foreach (MapChangeComment comment in allComments) {
                  if (!string.IsNullOrWhiteSpace(comment.comment)) {
                     comment.comment = "(REVERTED): " + comment.comment;
                  }
               }
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               StartCoroutine(sendDiscordPublishReport(new MapChangeCommentCollection { records = allComments.ToArray() }));
            });
         });
         UI.loadingPanel.display("Forming Discord report", task);
      }

      private IEnumerator sendDiscordPublishReport (MapChangeCommentCollection report) {
         string body = JsonUtility.ToJson(report);

         _sendingDiscordReport = true;

         UnityThreading.Task t = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            while (_sendingDiscordReport) {
               System.Threading.Thread.Sleep(200);
            }
         });
         UI.loadingPanel.display("Sending Discord report", t);

         using (UnityWebRequest www = new UnityWebRequest(@"https://tools.arcanewaters.com/api/Reports/MapPublish", UnityWebRequest.kHttpVerbPOST)) {
            www.SetRequestHeader("Content-Type", "application/json");
            var jsonBytes = Encoding.UTF8.GetBytes(body);
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();
            _sendingDiscordReport = false;
            D.log("Discord map publish: " + www.responseCode + " " + www.error);
         }
      }

      public void deleteMapVersion (MapVersion version) {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin && version.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            UI.messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         UI.yesNoDialog.display(
            "Deleting a map version",
            $"Are you sure you want to delete map {version.map.name} version {version.version}?",
            () => deleteMapVersionConfirm(version), null);
      }

      public void deleteMapVersionConfirm (MapVersion version) {
         clearEverything();
         if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.CompareTo(version) == 0) {
            DrawBoard.changeLoadedVersion(null);
         }

         var task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string error = null;
            try {
               DB_Main.deleteMapVersion(version);
            } catch (Exception ex) {
               error = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (error != null) {
                  UI.messagePanel.displayError(error);
               } else {
                  UI.mapList.open();
                  open(version.map);
               }
            });
         });

         UI.loadingPanel.display("Deleting map version", task);
      }

      public void close () {
         hide();
      }

      // Are we sending a discord report at this moment
      private bool _sendingDiscordReport = false;
   }
}
