using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

namespace MapCreationTool
{
   public class SaveAsPanel : UIPanel
   {
      [SerializeField]
      private InputField inputField = null;
      [SerializeField]
      private Text errorText = null;
      [SerializeField]
      private Text updateText = null;
      [SerializeField]
      private Button saveButton = null;
      [SerializeField]
      private Text saveButtonText = null;

      public void open () {
         errorText.text = "";
         saveButtonText.text = "Save";
         saveButton.interactable = true;
         inputField.text = DrawBoard.loadedMap?.name ?? "";
         setUpdateText();
         show();
      }

      public void inputValueChanged () {
         setUpdateText();
      }

      public void save () {
         StartCoroutine(saveRoutine());
      }

      public IEnumerator saveRoutine () {
         if (!MasterToolAccountManager.canAlterData()) {
            UI.errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            yield break;
         }

         saveButton.interactable = false;
         updateText.text = "";
         saveButtonText.text = "Saving...";
         errorText.text = "";

         // Wait for a few frames so the UI can update
         yield return new WaitForEndOfFrame();
         yield return new WaitForEndOfFrame();
         yield return new WaitForEndOfFrame();
#if IS_SERVER_BUILD
         try {
            if (string.IsNullOrWhiteSpace(inputField.text)) {
               throw new Exception("Name cannot be empty");
            }

            if (shouldBeUpdating()) {
               if (DrawBoard.loadedMap.creatorID != MasterToolAccountManager.self.currentAccountID) {
                  throw new Exception("You are not the creator of this map");
               }
            }

            MapDTO map = new MapDTO {
               name = inputField.text,
               editorData = DrawBoard.instance.formSerializedData(),
               gameData = DrawBoard.instance.formExportData(),
               creatorID = MasterToolAccountManager.self.currentAccountID,
               version = shouldBeUpdating() ? -1 : 0
            };

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  DB_Main.createNewMapDataVersion(map);
               } catch (MySqlException ex) {
                  if (ex.Number == 1062) {
                     dbError = $"Map with name '{map.name}' already exists";
                  } else {
                     dbError = ex.Message;
                  }
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  updateText.text = "";
                  saveButton.interactable = true;
                  saveButtonText.text = "Save";

                  if (dbError != null) {
                     errorText.text = dbError;
                  } else {
                     DrawBoard.loadedMap = map;
                     hide();
                  }
               });
            });
         } catch (Exception ex) {
            errorText.text = ex.Message;

            updateText.text = "";
            saveButton.interactable = true;
            saveButtonText.text = "Save";
         }
#endif
      }

      private void setUpdateText () {
         if (shouldBeUpdating()) {
            updateText.text = $"Existing map '{inputField.text}' will be updated.";
         } else {
            updateText.text = "";
         }
      }

      private bool shouldBeUpdating () {
         return DrawBoard.loadedMap != null && DrawBoard.loadedMap.name.CompareTo(inputField.text) == 0;
      }

      public void close () {
         hide();
      }
   }
}
