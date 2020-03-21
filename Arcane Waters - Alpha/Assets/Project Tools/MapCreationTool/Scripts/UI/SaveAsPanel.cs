using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using System.Linq;

namespace MapCreationTool
{
   public class SaveAsPanel : UIPanel
   {
      [SerializeField]
      private InputField inputField = null;
      [SerializeField]
      private Text errorText = null;
      [SerializeField]
      private Button saveButton = null;
      [SerializeField]
      private Text saveButtonText = null;

      public void open () {
         errorText.text = "";
         saveButtonText.text = "Save";
         saveButton.interactable = true;
         inputField.text = "";
         show();
      }

      public void save () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }
         StartCoroutine(saveRoutine());
      }

      public IEnumerator saveRoutine () {
         saveButton.interactable = false;
         saveButtonText.text = "Saving...";
         errorText.text = "";

         // Wait for a few frames so the UI can update
         yield return new WaitForEndOfFrame();
         yield return new WaitForEndOfFrame();
         yield return new WaitForEndOfFrame();

         try {
            if (string.IsNullOrWhiteSpace(inputField.text)) {
               throw new Exception("Name cannot be empty");
            }

            MapVersion mapVersion = new MapVersion {
               version = 0,
               createdAt = DateTime.UtcNow,
               updatedAt = DateTime.UtcNow,
               editorData = DrawBoard.instance.formSerializedData(),
               gameData = DrawBoard.instance.formExportData(),
               map = new Map {
                  name = inputField.text,
                  createdAt = DateTime.UtcNow,
                  creatorID = MasterToolAccountManager.self.currentAccountID,
                  editorType = Tools.editorType
               },
               spawns = DrawBoard.instance.formSpawnList(null, 0)
            };

            // Make sure all spawns have unique names
            if (mapVersion.spawns.Count > 0 && mapVersion.spawns.GroupBy(s => s.name).Max(g => g.Count()) > 1) {
               throw new Exception("Not all spawn names are unique");
            }

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  DB_Main.createMap(mapVersion);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  saveButton.interactable = true;
                  saveButtonText.text = "Save";

                  if (dbError != null) {
                     errorText.text = dbError;
                  } else {
                     DrawBoard.changeLoadedVersion(mapVersion);
                     Overlord.loadAllRemoteData();
                     hide();
                  }
               });
            });
         } catch (Exception ex) {
            errorText.text = ex.Message;
            saveButton.interactable = true;
            saveButtonText.text = "Save";
         }
      }

      public void close () {
         hide();
      }
   }
}
