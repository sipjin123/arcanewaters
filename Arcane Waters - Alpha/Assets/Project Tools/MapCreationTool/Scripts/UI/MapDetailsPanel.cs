using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MapDetailsPanel : UIPanel
   {
      [SerializeField]
      private Text topLabel = null;
      [SerializeField]
      private InputField nameInput = null;
      [SerializeField]
      private Dropdown sourceMapDropdown = null;
      [SerializeField]
      private InputField notesInput = null;

      private Map targetMap;
      private (int id, string displayText)[] sourceOptions;

      public void open (Map map) {
         if (!MasterToolAccountManager.canAlterResource(map.creatorID, out string errorMessage)) {
            UI.errorDialog.displayUnauthorized(errorMessage);
            return;
         }

         targetMap = map;

         sourceOptions = Enumerable.Repeat((0, "None"), 1).Union(Overlord.remoteMaps.maps.Select(m => (m.Value.id, m.Value.name))).ToArray();

         nameInput.text = map.name;
         notesInput.text = map.notes;
         sourceMapDropdown.options = sourceOptions.Select(o => new Dropdown.OptionData { text = o.displayText }).ToList();

         sourceMapDropdown.value = 0;
         for (int i = 0; i < sourceOptions.Length; i++) {
            if (sourceOptions[i].id == map.sourceMapId) {
               sourceMapDropdown.value = i;
               break;
            }
         }

         topLabel.text = $"Editing details of map { targetMap.name }";
         show();
      }

      public void confirm () {
         try {
            Map newMap = new Map {
               id = targetMap.id,
               name = nameInput.text,
               notes = notesInput.text,
               sourceMapId = sourceOptions[sourceMapDropdown.value].id
            };

            if (string.IsNullOrWhiteSpace(newMap.name)) {
               throw new Exception("Name cannot be empty");
            }

            UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  DB_Main.updateMapDetails(newMap);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (dbError != null) {
                     UI.errorDialog.display(dbError.ToString());
                  } else {
                     if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapId == targetMap.id) {
                        MapVersion version = DrawBoard.loadedVersion;
                        version.map.name = newMap.name;
                        version.map.sourceMapId = newMap.sourceMapId;
                        version.map.notes = newMap.notes;
                        DrawBoard.changeLoadedVersion(version);
                     }
                     Overlord.loadAllRemoteData();
                     UI.mapList.open();
                     hide();
                  }
               });

            });

            UI.loadingPanel.display("Updating map details", task);
         } catch (Exception ex) {
            UI.errorDialog.display(ex.ToString());
         }
      }

      public void close () {
         hide();
      }
   }
}
