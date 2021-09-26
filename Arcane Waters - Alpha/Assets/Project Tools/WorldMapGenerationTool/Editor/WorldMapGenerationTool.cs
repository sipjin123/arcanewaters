using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;

public class WorldMapGenerationTool : MonoBehaviour
{
   #region Public Variables

   #endregion

   public static bool findSettings (out WorldMapGenerationSettings generationSettings) {
      generationSettings = null;

      D.debug("Searching for settings...");
      string[] guids = AssetDatabase.FindAssets("WorldMapGenerationSettings");

      if (guids == null || guids.Length == 0) {
         D.error("Searching for the settings: failed - none found. Exiting process.");
         return false;
      }

      D.debug($"Searching for settings: ok - found {guids.Length} GUIDs.");

      foreach (string guid in guids) {
         string path = AssetDatabase.GUIDToAssetPath(guid);

         D.debug($"Found valid settings at {path}.");
         WorldMapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<WorldMapGenerationSettings>(path);

         if (settings == null) {
            D.error($"The settings object found at {path} wasn't valid after all.");
            continue;
         }

         if (settings.sourceTexture == null) {
            D.error($"The settings object found at {path} is valid but there is no texture defined.");
            continue;
         }

         if (settings.columns <= 0) {
            D.error("The columns parameter in the settings is not valid. It must be an integer larger than zero.");
            continue;
         }

         if (settings.rows <= 0) {
            D.error("The rows parameter in the settings is not valid. It must be an integer larger than zero.");
            continue;
         }

         if (settings.sourceTexture == null) {
            D.error("The texture specifed is not valid.");
            continue;
         }

         if ((settings.sourceTexture.width % settings.columns) != 0) {
            D.error("The width of the texture is not valid. It should be a multiple of the columns specified.");
            continue;
         }

         if ((settings.sourceTexture.height % settings.rows) != 0) {
            D.error("The height of the texture is not valid. It should be a multiple of the rows specified.");
            continue;
         }

         if (!settings.sourceTexture.isReadable) {
            D.error("The import settings of the specified texture are not valid. The texture must be readable.");
            continue;
         }

         generationSettings = settings;
         return true;
      }

      return false;
   }

   #region World Map

   [MenuItem("Util/Generate World Map")]
   public static void generateWorldMap () {
      bool userHasAccepted = EditorUtility.DisplayDialog(_toolName, "This tool will generate the World Map. Do you want to proceed?", "Yes", "No");

      if (!userHasAccepted) {
         D.debug($"{_toolName}: Task aborted.");
         return;
      }

      D.debug($"{_toolName}: Task started...");
      rep("Searching for Settings...", 0.5f);

      if (!findSettings(out WorldMapGenerationSettings settings)) {
         EditorUtility.ClearProgressBar();
         return;
      }

      WorldMap map = generateMap(settings);
      uploadMap(map, settings.shouldCleanUp);

      EditorUtility.ClearProgressBar();
      EditorUtility.DisplayDialog(_toolName, "Task completed!", "OK");
   }

   private static WorldMap generateMap (WorldMapGenerationSettings settings) {
      int counter = 0;

      WorldMap map = new WorldMap {
         rows = settings.rows,
         columns = settings.columns,
         sectorCount = settings.rows * settings.columns,
         sectors = new List<WorldMapSector>()
      };

      for (int r = 0; r < settings.rows; r++) {
         for (int c = 0; c < settings.columns; c++) {
            WorldMapSector sector = computeSector(settings.sourceTexture, c, r, settings);
            map.sectors.Add(sector);
            counter++;
            rep($"Generating Maps: {counter}/{map.sectorCount}", (float) counter / map.sectorCount);
         }
      }

      D.debug($"{_toolName}: {map.sectorCount} sectors generated.");

      rep("Generating Maps: OK", 1.0f);
      return map;
   }

   private static void uploadMap (WorldMap map, bool cleanUp = false) {
      string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
      string awFolder = "aw-" + System.DateTime.Now.Ticks.ToString();
      string awFolderPath = Path.Combine(desktopPath, awFolder);

      if (!Directory.Exists(awFolderPath)) {
         Directory.CreateDirectory(awFolderPath);
      }

      DB_Main.clearWorldMap();

      int counter = 0;

      foreach (WorldMapSector sector in map.sectors) {
         // Serialize the map sector to disk
         string jsonFilePath = Path.Combine(awFolderPath, $"aw_world_map_sector_{sector.sectorIndex}.json");
         File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(sector, Formatting.Indented));

         // Compress the serialized map sector file
         string compressedFilePath = Path.Combine(awFolderPath, $"aw_world_map_sector_{sector.sectorIndex}.zip");

         using (ZipStorer storer = ZipStorer.Create(compressedFilePath)) {
            storer.AddFile(ZipStorer.Compression.Deflate, jsonFilePath, Path.GetFileName(jsonFilePath));
         }

         // Read the file and upload it to the database
         byte[] data = File.ReadAllBytes(compressedFilePath);
         DB_Main.uploadWorldMapSector(data);

         // Update progress
         counter++;
         rep($"Uploading Map: {counter}/{map.sectorCount}", (float) counter / map.sectorCount);
      }

      D.debug($"{_toolName}: Map sectors uploaded.");

      if (cleanUp) {
         rep("Cleaning Up...", 1.0f);

         if (Directory.Exists(awFolderPath)) {
            Directory.Delete(awFolderPath);
            D.debug($"{_toolName}: Map files at '{awFolderPath}' deleted.");
         }
      }
   }

   private static WorldMapSector computeSector (Texture2D texture, int sectorX, int sectorY, WorldMapGenerationSettings settings) {
      WorldMapSector sector = new WorldMapSector {
         x = sectorX,
         y = sectorY,
         w = texture.width / settings.columns,
         h = texture.height / settings.rows,
         map = new WorldMapInfo {
            columns = settings.columns,
            rows = settings.rows,
            sectors = settings.columns * settings.rows
         },
         sectorIndex = settings.columns * sectorY + sectorX,
         tiles = new List<WorldMapTile>()
      };

      for (int r = 0; r < sector.h; r++) {
         for (int c = 0; c < sector.w; c++) {
            int pixelX = (sector.w * sectorX) + c;
            int pixelY = (sector.h * sectorY) + r;
            Color pixel = texture.GetPixel(pixelX, pixelY);

            // We assume that all the channels will have the same value.
            if (pixel.r > 0.5f || pixel.g > 0.5f || pixel.b > 0.5f) {
               // This is a land tile. Add it to the set.
               WorldMapTile tile = new WorldMapTile {
                  x = pixelX,
                  y = pixelY
               };

               sector.tiles.Add(tile);
            }
         }
      }
      return sector;
   }

   #endregion

   #region Utils

   private static void rep (string message, float progress) {
      EditorUtility.DisplayProgressBar(_toolName, message, progress);
   }

   #endregion

   #region Private Variables

   // The name of the tool
   private static string _toolName = "World Map Generation";

   #endregion
}