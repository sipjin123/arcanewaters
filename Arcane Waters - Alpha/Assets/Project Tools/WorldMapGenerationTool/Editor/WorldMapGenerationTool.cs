using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using System;

public class WorldMapGenerationTool : MonoBehaviour
{
   #region Public Variables

   #endregion

   #region Generate
   
   [MenuItem("Util/Generate World Map")]
   public static void generatedworldMapCommand () {
      bool userHasAccepted = EditorUtility.DisplayDialog(_toolName, "This tool will generate the World Map. Do you want to proceed?", "Yes", "No");

      if (!userHasAccepted) {
         D.debug($"{_toolName}: Task canceled.");
         return;
      }

      WorldMapGenerationSettings settings = findSettings();

      if (settings == null) {
         EditorUtility.ClearProgressBar();
         return;
      }

      WorldMap map = generateWorldMap(settings);

      if (map == null) {
         EditorUtility.ClearProgressBar();
         Debug.LogError($"{_toolName}: Couldn't generate the World Map.");
         return;
      }

      bool success = uploadWorldMap(map);

      if (!success) {
         EditorUtility.ClearProgressBar();
         Debug.LogError($"{_toolName}: Couldn't upload the World Map.");
         return;
      }

      EditorUtility.ClearProgressBar();
      EditorUtility.DisplayDialog(_toolName, "Task completed!", "OK");
   }

   private static WorldMap generateWorldMap (WorldMapGenerationSettings settings) {
      WorldMap map = new WorldMap {
         rows = settings.rows,
         columns = settings.columns,
         sectorCount = settings.rows * settings.columns,
         sectors = new List<WorldMapSector>()
      };

      int counter = 0;

      for (int r = 0; r < settings.rows; r++) {
         for (int c = 0; c < settings.columns; c++) {
            WorldMapSector sector = computeWorldMapSector(settings.sourceTexture, c, r, settings);
            map.sectors.Add(sector);
            counter++;

            if (rep($"Generating Maps: {counter}/{map.sectorCount}", (float) counter / map.sectorCount)) {
               EditorUtility.ClearProgressBar();
               Debug.Log($"{_toolName}: Task aborted by the user.");
               return null;
            }
         }
      }

      D.debug($"{_toolName}: {map.sectorCount} sectors generated.");
      rep("Generating Maps: OK", 1.0f, cancelable: false);
      return map;
   }

   private static WorldMapSector computeWorldMapSector (Texture2D texture, int sectorX, int sectorY, WorldMapGenerationSettings settings) {
      WorldMapSector sector = new WorldMapSector {
         x = sectorX,
         y = sectorY,
         w = texture.width / settings.columns,
         h = texture.height / settings.rows,
         sectorIndex = settings.columns * sectorY + sectorX,
      };

      StringBuilder sb = new StringBuilder();

      for (int r = 0; r < sector.h; r++) {
         for (int c = 0; c < sector.w; c++) {
            int pixelX = (sector.w * sectorX) + c;
            int pixelY = (sector.h * sectorY) + r;
            Color pixel = texture.GetPixel(pixelX, pixelY);

            // We assume that all the channels will have the same value.
            if (pixel.r > 0.5f || pixel.g > 0.5f || pixel.b > 0.5f) {
               sb.Append(((int) WorldMapTile.TileType.Land).ToString()[0]);
            } else {
               sb.Append(((int) WorldMapTile.TileType.Water).ToString()[0]);
            }
         }

         sb.Append('\n');
      }

      sector.tilesString = sb.ToString();
      return sector;
   }

   #endregion

   #region Upload

   private static bool uploadWorldMap (WorldMap map) {
      DB_Main.clearWorldMap();
      int counter = 0;

      foreach (WorldMapSector sector in map.sectors) {
         string serializedSector = serializeSector(sector);
         string filenameInArchive = $"aw_world_map_sector_{sector.sectorIndex}.zip";
         byte[] compressedData = GZipUtility.compressString(filenameInArchive, serializedSector);

         if (compressedData == null) {
            EditorUtility.ClearProgressBar();
            D.error($"{_toolName}: Error encountered while compressing sector {sector.sectorIndex}. Aborting task.");
            return false;
         }

         bool uploaded = DB_Main.uploadWorldMapSector(compressedData);
         
         if (!uploaded) {
            EditorUtility.ClearProgressBar();
            D.error($"{_toolName}: Error encountered while uploading sector {sector.sectorIndex}. Aborting task.");
            return false;
         }

         counter++;

         if (rep($"Uploading Map: {counter}/{map.sectorCount}", (float) counter / map.sectorCount)) {
            EditorUtility.ClearProgressBar();
            D.debug($"{_toolName}: Task aborted by the user.");
            return false;
         }
      }

      D.debug($"{_toolName}: Map sectors uploaded.");
      return true;
   }

   #endregion

   #region Download

   private static WorldMapSector downloadWorldMapSector (int sectorIndex) {
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
      } catch (System.Exception ex) {
         D.error(ex.Message);
      }

      return sector;
   }

   #endregion

   #region Utils

   private static bool rep (string message, float progress, bool cancelable = true) {
      if (cancelable) {
         return EditorUtility.DisplayCancelableProgressBar(_toolName, message, progress);
      }

      EditorUtility.DisplayProgressBar(_toolName, message, progress);
      return false;
   }

   private static string serializeSector (WorldMapSector sector) {
      return $"{sector.sectorIndex} {sector.x} {sector.y} {sector.w} {sector.h}\n{sector.tilesString}";
   }

   public static WorldMapGenerationSettings findSettings () {
      WorldMapGenerationSettings settings = null;
      D.debug($"{_toolName}: Searching for settings...");
      rep($"{_toolName}: Searching for Settings...", 0.5f, cancelable: false);
      string[] guids = AssetDatabase.FindAssets("WorldMapGenerationSettings");

      if (guids == null || guids.Length == 0) {
         Debug.LogError("Searching for the settings: failed - none found. Exiting process.");
         return settings;
      }

      D.debug($"{_toolName}: Searching for settings: OK - found {guids.Length} GUIDs.");

      foreach (string guid in guids) {
         string path = AssetDatabase.GUIDToAssetPath(guid);

         D.debug($"{_toolName}: Found valid settings at {path}.");
         settings = AssetDatabase.LoadAssetAtPath<WorldMapGenerationSettings>(path);

         if (settings == null) {
            Debug.LogError($"{_toolName}: The settings object found at {path} wasn't valid after all.");
            continue;
         }

         if (settings.sourceTexture == null) {
            Debug.LogError($"{_toolName}: The settings object found at {path} is valid but there is no texture defined.");
            continue;
         }

         if (settings.columns <= 0) {
            Debug.LogError($"{_toolName}: The columns parameter in the settings is not valid. It must be an integer larger than zero.");
            continue;
         }

         if (settings.rows <= 0) {
            Debug.LogError($"{_toolName}: The rows parameter in the settings is not valid. It must be an integer larger than zero.");
            continue;
         }

         if (settings.sourceTexture == null) {
            Debug.LogError($"{_toolName}: The texture specifed is not valid.");
            continue;
         }

         if ((settings.sourceTexture.width % settings.columns) != 0) {
            Debug.LogError($"{_toolName}: The width of the texture is not valid. It should be a multiple of the columns specified.");
            continue;
         }

         if ((settings.sourceTexture.height % settings.rows) != 0) {
            Debug.LogError($"{_toolName}: The height of the texture is not valid. It should be a multiple of the rows specified.");
            continue;
         }

         if (!settings.sourceTexture.isReadable) {
            Debug.LogError($"{_toolName}: The import settings of the specified texture are not valid. The texture must be readable.");
            continue;
         }

         return settings;
      }

      return settings;
   }

   #endregion

   #region Private Variables

   // The name of the tool
   private static string _toolName = "World Map Generation Tool";

   #endregion
}