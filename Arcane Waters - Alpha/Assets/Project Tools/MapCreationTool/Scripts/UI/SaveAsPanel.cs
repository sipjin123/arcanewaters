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
      private Button saveButton = null;
      [SerializeField]
      private Text saveButtonText = null;

      public void open () {
         saveButtonText.text = "Save";
         saveButton.interactable = true;
         inputField.text = "";
         show();
      }

      public void save () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }
         StartCoroutine(saveRoutine());
      }

      public IEnumerator saveRoutine () {
         saveButton.interactable = false;
         saveButtonText.text = "Saving...";

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
                  editorType = Tools.editorType,
                  biome = Tools.biome
               },
               spawns = DrawBoard.instance.formSpawnList(null, 0)
            };

            if (!Overlord.validateMap(out string errors)) {
               throw new Exception("Failed validating a map:" + Environment.NewLine + Environment.NewLine + errors);
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
                     UI.messagePanel.displayError(dbError);
                  } else {
                     DrawBoard.changeLoadedVersion(mapVersion);
                     Overlord.loadAllRemoteData();
                     hide();
                  }
               });
            });
         } catch (Exception ex) {
            UI.messagePanel.displayError(ex.Message);
            saveButton.interactable = true;
            saveButtonText.text = "Save";
         }
      }

      public void close () {
         hide();
      }
   }
}
