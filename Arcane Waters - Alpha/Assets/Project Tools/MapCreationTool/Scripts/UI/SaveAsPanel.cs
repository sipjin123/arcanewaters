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
         inputField.text = DrawBoard.loadedMapName ?? "";
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

            bool update = shouldBeUpdating();

            MapDTO map = new MapDTO {
               name = inputField.text,
               editorData = DrawBoard.instance.formSerializedData(),
               gameData = DrawBoard.instance.formExportData()
            };

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  if (update) {
                     DB_Main.updateMapData(map);
                  } else {
                     DB_Main.createMapData(map);
                  }
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
                     DrawBoard.loadedMapName = map.name;
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
         return DrawBoard.loadedMapName != null && DrawBoard.loadedMapName.CompareTo(inputField.text) == 0;
      }

      public void close () {
         hide();
      }
   }
}
