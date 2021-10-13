using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using MapCreationTool;
using System.Collections;
using System;
using UnityEngine.UI;

public class WorldMapTranslator : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static WorldMapTranslator instance;

   // The cached world map data list
   public WorldMapSector cachedWorldMap = new WorldMapSector();

   // The current map index
   public int currentMapIndex = 1;

   // The max counter before stopping sequence of map generator
   public int maxMapCounter = 5;

   // The textfield where the starting map sequence id is set
   public InputField mapStartSequenceIdText, mapEndSequenceIdText;

   // The map id display
   public Text displayMapId, displayMaxMapId;

   // The text that displays the map generator progress
   public Text progressText, currentMapTargetText;

   // The panel containing the customizable settings
   public GameObject settingsPanel;

   // The panel displaying the progress
   public GameObject progressPanel;

   // Constant values of the world map array
   public const int ROW_COUNT = 15;
   public const int COLUMN_COUNT = 9;
   public const string ALPHABET = "ABCDEFGHIJKLMNO";

   #endregion

   private void Awake () {
      instance = this;

      mapStartSequenceIdText.onValueChanged.AddListener(_ => {
         displayMapId.text = _.ToString();
      });
      mapEndSequenceIdText.onValueChanged.AddListener(_ => {
         displayMaxMapId.text = _.ToString();
      });
      displayMapId.text = currentMapIndex.ToString();
   }

   public void triggerSettingsPanel (bool isActive) {
      settingsPanel.SetActive(isActive);
   }

   public void startMapSequence () {
      try {
         int startSequenceValue = int.Parse(displayMapId.text);
         int endSequenceValue = int.Parse(displayMaxMapId.text);
         currentMapIndex = startSequenceValue;
         maxMapCounter = endSequenceValue;
         fetchWorldData(currentMapIndex);
      } catch {
         D.debug("Failed to parse value declared in input field! {" + mapStartSequenceIdText.text + "}");
      }
   }

   private IEnumerator CO_SimulateSave () {
      yield return new WaitForSeconds(1);

      // Setup Listener
      UI.saveAsPanel.saveCompleteEvent.RemoveAllListeners();
      UI.saveAsPanel.saveCompleteEvent.AddListener(() => {
         D.debug("{" + currentMapIndex + "} is saved! Now loading {" + (currentMapIndex + 1) + "}");
         currentMapIndex++;

         if (currentMapIndex < maxMapCounter) {
            fetchWorldData(currentMapIndex);
         } else {
            D.debug("Total map sequence has now reached max! Manual trigger again if needed");
         }
      });
      UI.self.saveAs();

      // Save map
      yield return new WaitForSeconds(1);
      DateTime currDateData = DateTime.UtcNow;
      int rowCount = 0;
      char columnValue = 'x';
      string saveName = "";
      bool includeTimeStamp = false;
      try {
         rowCount = (int) (currentMapIndex / ROW_COUNT);
         columnValue = ALPHABET[(currentMapIndex - (ROW_COUNT * rowCount))];
         saveName = "world_map_" + columnValue + rowCount + (includeTimeStamp ? ("_T:" + currDateData.Hour + ":" + currDateData.Minute) : "");
      } catch {
         saveName = "world_map_" + currentMapIndex + (includeTimeStamp ? ("_T:" + currDateData.Hour + ":" + currDateData.Minute) : "");
      }
      UI.saveAsPanel.forceSaveName(saveName);
      yield return new WaitForSeconds(1);
      UI.saveAsPanel.save();
   }

   private void Update () {
      if (KeyUtils.GetKeyDown(Key.Escape)) {
         D.debug("Process Aborted!");
         StopAllCoroutines();
         DrawBoardEvents.instance.stopAllCoroutines();
      }
   }

   private void fetchWorldData (int mapId) {
      D.debug("Fetching world data: " + mapId);
      DrawBoard.instance.newMap();
      Overlord.instance.editorTypeChanged(EditorType.Area, EditorType.Sea);

      cachedWorldMap = new WorldMapSector();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         WorldMapSector tempMapInfo = WorldMapTranslator.downloadWorldMapSector(mapId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            cachedWorldMap = tempMapInfo;
            D.debug("Start map gen process for: " + mapId);
            StartCoroutine(botProcess(cachedWorldMap));
         });
      });
   }

   private IEnumerator botProcess (WorldMapSector worldMapList) {
      // Board size setup
      Tools.changeBoardSize(new Vector2Int(worldMapList.w, worldMapList.h));
      yield return new WaitForSeconds(1);

      // Zoom out simulation
      DrawBoard.instance.pointerScroll(Vector3.zero, -999);
      yield return new WaitForSeconds(1);

      // Control brush to draw map
      StartCoroutine(DrawBoardEvents.instance.simulateBrushAction(worldMapList));
   }

   public static WorldMapSector downloadWorldMapSector (int sectorIndex) {
      WorldMapSector sector = null;
      try {
         int totalSectors = DB_Main.getWorldMapSectorsCount();
         if (sectorIndex < 0 || sectorIndex >= totalSectors) {
            D.error($"{_toolName}: Couldn't find the selected sector.");
            return sector;
         }

         byte[] compressedSectorData = DB_Main.fetchWorldMapSector(sectorIndex);
         Dictionary<string, byte[]> entries = GZipUtility.decompressData(compressedSectorData);

         if (entries == null) {
            D.error($"{_toolName}: Couldn't decompress the downloaded data.");
            return sector;
         }

         foreach (string filename in entries.Keys) {
            if (filename.ToLower().StartsWith("aw_world_map_sector")) {
               sector = WorldMapSector.parse(Encoding.ASCII.GetString(entries[filename]));
               break;
            }
         }
      } catch (Exception ex) {
         D.error(ex.Message);
      }

      return sector;
   }

   public void triggerProgressPanel (bool isActive) {
      progressPanel.SetActive(isActive);
   }

   public void updateDisplayText (string msg) {
      progressText.text = msg;
   }

   public void updateMapDataText (string msg) {
      currentMapTargetText.text = msg;
   }

   public void simulateSave () {
      StartCoroutine(CO_SimulateSave());
   }


   #region Private Variables

   // The name of the tool
   private static string _toolName = "World Map Generation Tool";

   #endregion
}
