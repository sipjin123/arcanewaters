using System;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapRenamePanel : UIPanel
   {
      [SerializeField]
      private Text label = null;
      [SerializeField]
      private InputField inputField = null;

      private Map targetMap;

      public void open (Map map) {
         if (!MasterToolAccountManager.canAlterResource(map.creatorID, out string errorMessage)) {
            UI.errorDialog.displayUnauthorized(errorMessage);
            return;
         }

         targetMap = map;
         inputField.text = map.name;
         label.text = $"Enter a new <b>unique</b> name for the map { targetMap.name }";
         show();
      }

      public void confirm () {
         try {
            string newName = inputField.text;

            if (string.IsNullOrWhiteSpace(newName)) {
               throw new Exception("Name cannot be empty");
            }

            UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  DB_Main.renameMap(targetMap.id, newName);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (dbError != null) {
                     UI.errorDialog.display(dbError.ToString());
                  } else {
                     if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapId == targetMap.id) {
                        MapVersion version = DrawBoard.loadedVersion;
                        version.map.name = newName;
                        DrawBoard.changeLoadedVersion(version);
                     }
                     Overlord.loadAllRemoteData();
                     UI.mapList.open();
                     hide();
                  }
               });

            });

            UI.loadingPanel.display("Renaming a map", task);
         } catch (Exception ex) {
            UI.errorDialog.display(ex.ToString());
         }
      }

      public void close () {
         hide();
      }
   }
}
