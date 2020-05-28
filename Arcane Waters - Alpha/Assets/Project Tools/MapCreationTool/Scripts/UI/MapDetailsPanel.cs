﻿using System;
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
      [SerializeField]
      private Dropdown typeDropdown = null;

      private Map targetMap;
      private (int id, string displayText)[] sourceOptions;
      private List<(Area.SpecialType type, string displayText)> typeOptions;

      private void OnEnable () {
         // Set the type options and their values in the dropdown
         typeOptions = new List<(Area.SpecialType type, string displayText)>();
         foreach (Area.SpecialType type in Enum.GetValues(typeof(Area.SpecialType))) {
            typeOptions.Add((type, type.ToString()));
         }

         typeDropdown.options = typeOptions.Select(to => new Dropdown.OptionData { text = to.displayText }).ToList();
      }

      public void open (Map map) {
         if (!MasterToolAccountManager.canAlterResource(map.creatorID, out string errorMessage)) {
            UI.messagePanel.displayUnauthorized(errorMessage);
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

         int index = typeOptions.FindIndex(to => map.specialType == to.type);
         if (index == -1) {
            index = 0;
         }
         typeDropdown.SetValueWithoutNotify(index);

         topLabel.text = $"Editing details of map { targetMap.name }";
         show();
      }

      public void confirm () {
         try {
            Map newMap = new Map {
               id = targetMap.id,
               name = nameInput.text,
               notes = notesInput.text,
               sourceMapId = sourceOptions[sourceMapDropdown.value].id,
               specialType = typeOptions[typeDropdown.value].type
            };

            if (string.IsNullOrWhiteSpace(newMap.name)) {
               throw new ArgumentException("Name cannot be empty");
            }

            if (newMap.specialType == Area.SpecialType.Voyage && targetMap.editorType != EditorType.Sea) {
               throw new ArgumentException("Only sea maps can be voyage maps");
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
                     UI.messagePanel.displayError(dbError.ToString());
                  } else {
                     if (DrawBoard.loadedVersion != null && DrawBoard.loadedVersion.mapId == targetMap.id) {
                        MapVersion version = DrawBoard.loadedVersion;
                        version.map.name = newMap.name;
                        version.map.sourceMapId = newMap.sourceMapId;
                        version.map.notes = newMap.notes;
                        version.map.specialType = newMap.specialType;
                        DrawBoard.changeLoadedVersion(version);
                     }
                     Overlord.loadAllRemoteData();
                     UI.mapList.open();
                     hide();
                  }
               });

            });

            UI.loadingPanel.display("Updating map details", task);
         } catch (ArgumentException ex) {
            UI.messagePanel.displayError(ex.Message.ToString());
         } catch (Exception ex) {
            UI.messagePanel.displayError(ex.ToString());
         }
      }

      public void close () {
         hide();
      }
   }
}
