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
      private InputField displayNameInput = null;
      [SerializeField]
      private Dropdown sourceMapDropdown = null;
      [SerializeField]
      private InputField notesInput = null;
      [SerializeField]
      private Dropdown typeDropdown = null;
      [SerializeField]
      private Dropdown weatherEffectDropdown = null;
      [SerializeField]
      private InputField maxPlayerCountInput = null;
      [SerializeField]
      private Dropdown pvpGameModeDropdown = null;
      [SerializeField]
      private Dropdown pvpArenaSizeDropdown = null;

      private Map targetMap;
      private (int id, string displayText)[] sourceOptions;
      private List<(Area.SpecialType type, string displayText)> typeOptions;
      private List<(WeatherEffectType type, string displayText)> weatherOptions;
      private List<(PvpGameMode type, string displayText)> pvpGameModeOptions;
      private List<(PvpArenaSize type, string displayText)> pvpArenaSizeOptions;

      private void OnEnable () {
         // Set the type options and their values in the dropdown
         typeOptions = new List<(Area.SpecialType type, string displayText)>();
         foreach (Area.SpecialType type in Enum.GetValues(typeof(Area.SpecialType))) {
            typeOptions.Add((type, type.ToString()));
         }

         typeDropdown.options = typeOptions.Select(to => new Dropdown.OptionData { text = to.displayText }).ToList();

         // Set weather options and their values in the dropdown
         weatherOptions = new List<(WeatherEffectType type, string displayText)>();
         foreach (WeatherEffectType type in Enum.GetValues(typeof(WeatherEffectType))) {
            weatherOptions.Add((type, type.ToString()));
         }

         weatherEffectDropdown.options = weatherOptions.Select(to => new Dropdown.OptionData { text = to.displayText }).ToList();

         // Set pvp game mode options and their values in the dropdown
         pvpGameModeOptions = new List<(PvpGameMode type, string displayText)>();
         foreach (PvpGameMode type in Enum.GetValues(typeof(PvpGameMode))) {
            pvpGameModeOptions.Add((type, type.ToString()));
         }

         pvpGameModeDropdown.options = pvpGameModeOptions.Select(to => new Dropdown.OptionData { text = to.displayText }).ToList();

         // Set pvp area size options and their values in the dropdown
         pvpArenaSizeOptions = new List<(PvpArenaSize type, string displayText)>();
         foreach (PvpArenaSize type in Enum.GetValues(typeof(PvpArenaSize))) {
            pvpArenaSizeOptions.Add((type, type.ToString()));
         }

         pvpArenaSizeDropdown.options = pvpArenaSizeOptions.Select(to => new Dropdown.OptionData { text = to.displayText }).ToList();
      }

      public void open (Map map) {
         if (!MasterToolAccountManager.canAlterResource(map.creatorID, out string errorMessage)) {
            UI.messagePanel.displayUnauthorized(errorMessage);
            return;
         }

         targetMap = map;

         sourceOptions = Enumerable.Repeat((0, "None"), 1).Union(Overlord.remoteMaps.maps
            .Where(m => m.Value.specialType == Area.SpecialType.Town).Select(m => (m.Value.id, m.Value.name))).ToArray();

         nameInput.text = map.name;
         displayNameInput.text = map.displayName;
         notesInput.text = map.notes;
         maxPlayerCountInput.text = map.maxPlayerCount.ToString();
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

         int weatherIndex = weatherOptions.FindIndex(to => map.weatherEffectType == to.type);
         if (weatherIndex == -1) {
            weatherIndex = 0;
         }
         weatherEffectDropdown.SetValueWithoutNotify(weatherIndex);

         int pvpGameModeIndex = pvpGameModeOptions.FindIndex(to => map.pvpGameMode == to.type);
         if (pvpGameModeIndex == -1) {
            pvpGameModeIndex = 0;
         }
         pvpGameModeDropdown.SetValueWithoutNotify(pvpGameModeIndex);

         int pvpArenaSizeIndex = pvpArenaSizeOptions.FindIndex(to => map.pvpArenaSize == to.type);
         if (pvpArenaSizeIndex == -1) {
            pvpArenaSizeIndex = 0;
         }
         pvpArenaSizeDropdown.SetValueWithoutNotify(pvpArenaSizeIndex);

         topLabel.text = $"Editing details of map { targetMap.name }";
         show();
      }

      public void confirm () {
         try {
            if (!int.TryParse(maxPlayerCountInput.text, out int maxPlayerCount)) {
               throw new ArgumentException($"Could not parse the max player count: {maxPlayerCountInput.text}");
            }

            Map newMap = new Map {
               id = targetMap.id,
               name = nameInput.text,
               displayName = displayNameInput.text,
               notes = notesInput.text,
               sourceMapId = sourceOptions[sourceMapDropdown.value].id,
               specialType = typeOptions[typeDropdown.value].type,
               weatherEffectType = weatherOptions[weatherEffectDropdown.value].type,
               maxPlayerCount = maxPlayerCount,
               pvpGameMode = pvpGameModeOptions[pvpGameModeDropdown.value].type,
               pvpArenaSize = pvpArenaSizeOptions[pvpArenaSizeDropdown.value].type,
            };

            if (string.IsNullOrWhiteSpace(newMap.name)) {
               throw new ArgumentException("Name cannot be empty");
            }

            if (newMap.specialType == Area.SpecialType.Voyage && targetMap.editorType != EditorType.Sea) {
               throw new ArgumentException("Only sea maps can be voyage maps");
            }

            if (newMap.specialType == Area.SpecialType.League && targetMap.editorType != EditorType.Sea) {
               throw new ArgumentException("Only sea maps can be league maps");
            }
            
            if (newMap.specialType == Area.SpecialType.LeagueSeaBoss && targetMap.editorType != EditorType.Sea) {
               throw new ArgumentException("Only sea maps can be league boss maps");
            }

            if (newMap.specialType == Area.SpecialType.TreasureSite && targetMap.editorType != EditorType.Area) {
               throw new ArgumentException("Only outside area maps can be treasure site maps");
            }

            if (newMap.specialType == Area.SpecialType.Town && targetMap.editorType != EditorType.Area) {
               throw new ArgumentException("Only outside area maps can be town maps");
            }

            if (newMap.specialType != Area.SpecialType.PvpArena && newMap.pvpGameMode != PvpGameMode.None) {
               throw new ArgumentException("Only pvp arenas can have a game mode");
            }

            if (newMap.specialType != Area.SpecialType.PvpArena && newMap.pvpArenaSize != PvpArenaSize.None) {
               throw new ArgumentException("Only pvp arenas can have an arena size");
            }

            if (newMap.specialType == Area.SpecialType.PvpArena && newMap.pvpGameMode == PvpGameMode.None) {
               throw new ArgumentException("Please specify a game mode for the pvp arena");
            }

            if (newMap.specialType == Area.SpecialType.PvpArena && newMap.pvpArenaSize == PvpArenaSize.None) {
               throw new ArgumentException("Please specify an arena size for the pvp arena");
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
                        version.map.weatherEffectType = newMap.weatherEffectType;
                        version.map.maxPlayerCount = newMap.maxPlayerCount;
                        version.map.pvpGameMode = newMap.pvpGameMode;
                        version.map.pvpArenaSize = newMap.pvpArenaSize;
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
