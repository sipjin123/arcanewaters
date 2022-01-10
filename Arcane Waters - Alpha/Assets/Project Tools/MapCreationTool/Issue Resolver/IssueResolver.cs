using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityThreading;

namespace MapCreationTool.IssueResolving
{
   public class IssueResolver : MonoBehaviour
   {
      public static IssueResolver instance { get; private set; }

      public static bool running { get; private set; }

      private static List<Map> maps;
      private static ConcurrentQueue<MapVersion> scheduledForResolve;
      private static ConcurrentQueue<(Action action, string mapName)> scheduledUpload;
      private static List<Task> downloadTasks;
      private static List<Task> uploadTasks;
      private static int resolvedMaps;
      private static string rootReportFolder;
      private static List<MapSummary> mapSummaries;
      private static Config config;
      private static IssueContainer issueContainer;
      private static string logs = "";

      private void Awake () {
         instance = this;
      }

      public static async void run (Config config) {
         if (running) {
            UI.messagePanel.displayError("Issue resolver is already running.");
            return;
         }

         IssueResolver.config = config;
         running = true;
         scheduledForResolve = new ConcurrentQueue<MapVersion>();
         scheduledUpload = new ConcurrentQueue<(Action, string)>();
         resolvedMaps = 0;
         downloadTasks = new List<Task>();
         uploadTasks = new List<Task>();
         rootReportFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Issue Resolver " + DateTime.Now.ToString("MMMM dd H.mm.ss");
         mapSummaries = new List<MapSummary>();
         logs = "";

         try {
            issueContainer = Resources.Load<IssueContainer>("Issue Container");
            issueContainer.fillDataStructures();
         } catch (Exception ex) {
            encounteredError(ex);
            return;
         }

         try {
            Directory.CreateDirectory(rootReportFolder);
         } catch (Exception ex) {
            encounteredError(ex);
            return;
         }

         UI.loadingPanel.display("Resolving Issues");

         try {
            maps = (await DB_Main.execAsync(DB_Main.getMaps))
               .Where(m => m.editorType == EditorType.Sea)
               .ToList();
            receivedMaps();
         } catch (Exception ex) {
            encounteredError(ex);
         }
      }

      private static void receivedMaps () {
         // Download all latest map versions
         foreach (Map map in maps) {
            Task task = Utilities.doBackgroundTask(() => scheduledForResolve.Enqueue(DB_Main.getLatestMapVersionEditor(map, true)), null, encounteredError);
            downloadTasks.Add(task);
         }

         instance.StartCoroutine(instance.resolveMapsRoutine());
         instance.StartCoroutine(instance.controlUploadTasks());
      }


      private IEnumerator resolveMapsRoutine () {
         while (resolvedMaps < maps.Count) {
            UI.loadingPanel.display($"Resolving Issues ({ resolvedMaps }/{ maps.Count })");

            if (scheduledForResolve.TryDequeue(out MapVersion version)) {
               // Create directory for individual map
               string mapDirectory = rootReportFolder + @"\" + version.map.name;
               doIOAction(() => Directory.CreateDirectory(mapDirectory));

               // Apply map data to the editor
               Overlord.instance.applyData(version);

               yield return new WaitForEndOfFrame();

               // Form tile data dictionary
               Dictionary<TileBase, PaletteTilesData.TileData> tileDataDictionary = Palette.instance.paletteData.formTileDataDictionary();

               // Save report about map before any issue resolving
               int misusedBeforeCount = 0;
               doIOAction(() => File.WriteAllBytes(mapDirectory + @"\" + "before.png", ScreenRecorder.recordPng()));
               doIOAction(() => File.WriteAllText(mapDirectory + @"\" + "before misused tiles.txt", getMisusedTilesReport(tileDataDictionary, out misusedBeforeCount)));

               // Resolve issues
               IssueResolvingResult? resolvingResult = config.alterData ? resolveMap() : (IssueResolvingResult?) null;
               if (resolvingResult != null) {
                  doIOAction(() => File.WriteAllText(mapDirectory + @"\" + "resolver report.txt", resolvingResult.Value.formReport()));
               }

               yield return new WaitForEndOfFrame();

               // Save report about map after issue resolving
               int misusedAfterCount = 0;
               doIOAction(() => File.WriteAllBytes(mapDirectory + @"\" + "after.png", ScreenRecorder.recordPng()));
               doIOAction(() => File.WriteAllText(mapDirectory + @"\" + "after misused tiles.txt", getMisusedTilesReport(tileDataDictionary, out misusedAfterCount)));
               doIOAction(() => File.WriteAllText(mapDirectory + @"\" + "removed data fields.txt", getRemovedDataFieldsReport(resolvingResult)));

               Biome.Type biome = Tools.biome;
               EditorType editorType = Tools.editorType;

               if (config.saveMaps && resolvingResult?.exception == null) {
                  MapVersion newVersion = serializeVersion();
                  if (config.createNewVersion) {
                     bool wasLatestVersion = DrawBoard.loadedVersion.map.publishedVersion == DrawBoard.loadedVersion.version;
                     Action uploadAction = () => {
                        MapVersion uploadedVersion = DB_Main.createNewMapVersion(newVersion, biome);

                        // Publish the uploaded version if configured and the previous version was the latest one before
                        if (config.publishMapIfLatest) {
                           if (wasLatestVersion) {
                              DB_Main.setLiveMapVersion(uploadedVersion);
                           } else {
                              logUnhandled("Not publishing live version of map: " + uploadedVersion.map.name + " " + uploadedVersion.version);
                           }
                        }
                     };

                     scheduledUpload.Enqueue((uploadAction, newVersion.map.name));
                  } else {
                     newVersion.version = DrawBoard.loadedVersion.version;
                     newVersion.createdAt = DrawBoard.loadedVersion.createdAt;
                     scheduledUpload.Enqueue((() => DB_Main.updateMapVersion(newVersion, biome, editorType, false), newVersion.map.name));
                  }
               }

               resolvedMaps++;

               mapSummaries.Add(new MapSummary {
                  mapName = version.map.name,
                  misuedTilesBeforeResolving = misusedBeforeCount,
                  misuedTilesAfterResolving = misusedAfterCount,
                  issueResolvingResult = resolvingResult
               });
            }
            yield return new WaitForEndOfFrame();
         }

         doIOAction(() => File.WriteAllText(rootReportFolder + @"\" + "short summary.txt", formShortSummary(mapSummaries)));
         doIOAction(() => File.WriteAllText(rootReportFolder + @"\" + "long summary.txt", formLongSummary(mapSummaries)));
         doIOAction(() => File.WriteAllText(rootReportFolder + @"\" + "unresolved short summary.txt", formShortSummary(mapSummaries.Where(ms => ms.misuedTilesAfterResolving != 0))));

         StartCoroutine(waitForEndOfUpload());
      }

      private static IssueResolvingResult resolveMap () {
         try {
            IssueResolvingResult result = new IssueResolvingResult {
               layerChanges = replaceTileLayers(),
               tilesToPrefabChanges = replaceTilesWithPrefabs(),
               removedUnnecessaryTiles = removeUnnecessaryTiles(),
               removedDataFields = removeMissingDataFields()
            };

            return result;
         } catch (Exception ex) {
            return new IssueResolvingResult { exception = ex };
         }
      }

      private static List<LayerChangeResult> replaceTileLayers () {
         List<LayerChangeResult> result = new List<LayerChangeResult>();

         foreach (PlacedTile placedTile in allPlacedTiles) {
            if (issueContainer.wrongLayer.TryGetValue(placedTile.tileBase, out IssueContainer.WrongLayerIssue issue)) {
               if (placedTile.layer.CompareTo(issue.fromLayer) == 0 && placedTile.sublayer == issue.fromSublayer) {
                  TileBase blockingTile = getLayer(issue.toLayer, issue.toSublayer).getTile(placedTile.position);

                  DrawBoard.instance.changeBoard(new List<TileChange> {
                        new TileChange { tile = null, position = placedTile.position, layer = getLayer(issue.fromLayer, issue.fromSublayer) },
                        new TileChange { tile = issue.tileBase, position = placedTile.position, layer = getLayer(issue.toLayer, issue.toSublayer) }
                     });

                  result.Add(new LayerChangeResult {
                     misplacedTile = placedTile,
                     fixedWithTile = new PlacedTile { tileBase = issue.tileBase, position = placedTile.position, layer = issue.toLayer, sublayer = issue.toSublayer },
                     blockingTile = blockingTile == null ? (PlacedTile?) null : new PlacedTile { tileBase = blockingTile, position = placedTile.position, layer = issue.toLayer, sublayer = issue.toSublayer }
                  });
               }
            }
         }

         return result;
      }

      private static List<TilesToPrefabChangeResult> replaceTilesWithPrefabs () {
         List<TilesToPrefabChangeResult> result = new List<TilesToPrefabChangeResult>();
         foreach (Layer layer in DrawBoard.instance.nonEmptySublayers()) {
            for (int i = 0; i < layer.size.x; i++) {
               for (int j = 0; j < layer.size.y; j++) {
                  Vector3Int anchorPos = new Vector3Int(i, j, 0) + layer.origin;
                  TileBase anchorTile = layer.getTile(anchorPos);
                  if (anchorTile != null) {
                     if (issueContainer.tileToPrefab.TryGetValue(anchorTile, out IssueContainer.TileNotPrefabIssue issue)) {
                        if (getLayer(issue.layer, issue.sublayer) == layer) {
                           // At this point, issue is confirmed. Try to include as many tiles as possible
                           (int x, int y) from = (anchorPos.x - issue.tileIndex.x, anchorPos.y - issue.tileIndex.y);
                           (int x, int y) to = (from.x + issue.tileMatrixSize.x, from.y + issue.tileMatrixSize.y);
                           List<PlacedTile> tilesToReplace = new List<PlacedTile>();
                           for (int x = from.x; x < to.x; x++) {
                              for (int y = from.y; y < to.y; y++) {
                                 TileBase tile = layer.getTile(x, y);
                                 if (tile != null && issue.allTiles[x - from.x, y - from.y] == tile) {
                                    tilesToReplace.Add(new PlacedTile {
                                       tileBase = tile,
                                       layer = issue.layer,
                                       sublayer = issue.sublayer,
                                       position = new Vector3Int(x, y, 0)
                                    });
                                 }
                              }
                           }

                           Vector3 targetPosition = new Vector3((from.x + to.x) * 0.5f, (from.y + to.y) * 0.5f) + (Vector3) issue.prefabOffset;

                           BoardChange change = new BoardChange();
                           change.tileChanges = tilesToReplace.Select(t => new TileChange { tile = null, position = t.position, layer = getLayer(t.layer, t.sublayer) }).ToList();
                           change.prefabChanges.Add(new PrefabChange {
                              prefabToPlace = issue.prefab,
                              positionToPlace = targetPosition
                           });
                           DrawBoard.instance.changeBoard(change);

                           result.Add(new TilesToPrefabChangeResult {
                              removedTiles = tilesToReplace,
                              placedPrefab = issue.prefab,
                              placedAtPosition = targetPosition
                           });
                        }
                     }
                  }
               }
            }
         }

         return result;
      }

      private static List<PlacedTile> removeUnnecessaryTiles () {
         List<PlacedTile> result = new List<PlacedTile>();

         foreach (PlacedTile placedTile in allPlacedTiles) {
            if (issueContainer.unnecessaryTiles.TryGetValue(placedTile.tileBase, out IssueContainer.UnnecessaryTileIssue issue)) {
               if (placedTile.layer.CompareTo(issue.layer) == 0 && placedTile.sublayer == issue.sublayer) {
                  DrawBoard.instance.changeBoard(new List<TileChange> {
                        new TileChange { tile = null, position = placedTile.position, layer = getLayer(issue.layer, issue.sublayer) }
                     });

                  result.Add(placedTile);
               }
            }
         }

         return result;
      }

      public static List<string> removeMissingDataFields () {
         List<string> result = new List<string>();

         foreach (PlacedPrefab pref in DrawBoard.instance.prefabsBetween(new Vector3(-1000, -1000, 0), new Vector3(1000, 1000, 0))) {
            if (issueContainer.removedDataFields.TryGetValue(pref.original, out HashSet<string> removedFields)) {
               List<string> keysToRemove = new List<string>();
               foreach (string key in pref.data.Keys) {
                  if (removedFields.Contains(key)) {
                     keysToRemove.Add(key);
                  }
               }

               foreach (string key in keysToRemove) {
                  DrawBoard.instance.setPrefabData(pref, key, null, false);
                  result.Add(pref.placedInstance.name + ": " + key);
               }
            }
         }

         return result;
      }
      private static string toStringLines<T> (IEnumerable<T> collection, string beforeEachLine = "") {
         return string.Join(Environment.NewLine, collection.Select(e => beforeEachLine + e.ToString())) + Environment.NewLine;
      }

      private static string getMisusedTilesReport (Dictionary<TileBase, PaletteTilesData.TileData> tileDataDictionary, out int misuedCount) {
         var misuedTiles = getMisusedTiles(tileDataDictionary);
         misuedCount = misuedTiles.Item1.Count + misuedTiles.Item2.Count;
         return toStringLines(misuedTiles.Item1, "Missing from palette: ") + toStringLines(misuedTiles.Item2, "Incorrect layer: ");
      }

      private static string getRemovedDataFieldsReport (IssueResolvingResult? resolveResult) {
         if (resolveResult == null) {
            return "Resolver didn't run";
         }

         if (resolveResult.Value.exception != null) {
            return "Resolver encountered an exception: \n" + resolveResult.Value.exception;
         }

         return string.Join(Environment.NewLine, resolveResult.Value.removedDataFields);
      }

      private static (List<PlacedTile>, List<PlacedTile>) getMisusedTiles (Dictionary<TileBase, PaletteTilesData.TileData> tileDataDictionary) {
         List<PlacedTile> missingFromPalette = new List<PlacedTile>();
         List<PlacedTile> wrongLayer = new List<PlacedTile>();

         foreach (PlacedTile placedTile in allPlacedTiles) {
            if (placedTile.tileBase == AssetSerializationMaps.transparentTileBase) continue;

            if (tileDataDictionary.TryGetValue(placedTile.tileBase, out PaletteTilesData.TileData tileData)) {
               if (tileData.layer.CompareTo(placedTile.layer) != 0 && tileData.subLayer != placedTile.sublayer) {
                  wrongLayer.Add(placedTile);
               }
            } else {
               missingFromPalette.Add(placedTile);
            }
         }

         return (missingFromPalette, wrongLayer);
      }

      private static void doIOAction (Action action) {
         try {
            action();
         } catch (Exception ex) {
            encounteredError(ex);
         }
      }

      private static string formLongSummary (IEnumerable<MapSummary> summaries) {
         return
            (config.saveMaps ? "Maps saved after resolving issues" : "Maps NOT saved after resolving issues")
            + $" (New version = {config.createNewVersion}, Publish version = {config.publishMapIfLatest})" + Environment.NewLine +
            (config.alterData ? "Issue resolving excecuted" : "Issue Resolver NOT excecuted") + Environment.NewLine + Environment.NewLine +
            string.Join(Environment.NewLine, summaries.Select(ms => ms.toLongSummary())) + Environment.NewLine;
      }

      private static string formShortSummary (IEnumerable<MapSummary> summaries) {
         return
            (config.saveMaps ? "Maps saved after resolving issues" : "Maps NOT saved after resolving issues")
            + $" (New version = {config.createNewVersion}, Publish version = {config.publishMapIfLatest})" + Environment.NewLine +
            (config.alterData ? "Issue resolving excecuted" : "Issue Resolver NOT excecuted") + Environment.NewLine + Environment.NewLine +
            toStringLines(summaries.Select(ms => ms.toShortSummary()));
      }

      private static IEnumerable<PlacedTile> allPlacedTiles
      {
         get
         {
            foreach (var kv in DrawBoard.instance.layers) {
               for (int k = 0; k < kv.Value.subLayers.Length; k++) {
                  Layer layer = kv.Value.subLayers[k];
                  for (int i = 0; i < layer.size.x; i++) {
                     for (int j = 0; j < layer.size.y; j++) {
                        Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
                        TileBase tileBase = layer.getTile(pos);
                        if (tileBase != null) {
                           yield return new PlacedTile {
                              tileBase = tileBase,
                              layer = kv.Key,
                              sublayer = k,
                              position = pos
                           };
                        }
                     }
                  }
               }
            }
         }
      }

      private IEnumerator controlUploadTasks () {
         while (runningUploadTasks > 0 || runningDownloadTasks > 0 || scheduledUpload.Count > 0 || resolvedMaps < maps.Count) {
            yield return new WaitForEndOfFrame();
            if (runningUploadTasks < 2 && scheduledUpload.Count > 0) {
               if (scheduledUpload.TryDequeue(out (Action, string) job)) {
                  Task task = Utilities.doBackgroundTask(job.Item1, null,
                     logException(job.Item2 + "_uploadError.txt", "There was an error uploading the version:"));
                  uploadTasks.Add(task);
               }
            }
         }
      }

      private IEnumerator waitForEndOfUpload () {
         while (runningUploadTasks > 0 || scheduledUpload.Count > 0) {
            UI.loadingPanel.display($"Resolving Issues - waiting for maps to finish uploading - { runningUploadTasks + scheduledUpload.Count }");
            yield return new WaitForEndOfFrame();
         }

         UI.loadingPanel.close();
         if (running) {
            UI.messagePanel.displayInfo("Resolving Issues Ended", "Resolving Issue process has successfully ended.\nSummary is saved in:\n" + rootReportFolder);
            running = false;
         }

         File.WriteAllText(rootReportFolder + @"\logs.txt", logs);
      }

      private static MapVersion serializeVersion () {
         return new MapVersion {
            mapId = DrawBoard.loadedVersion.map.id,
            version = -1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
            editorData = DrawBoard.instance.formSerializedData(),
            gameData = DrawBoard.instance.formExportData(),
            map = DrawBoard.loadedVersion.map,
            spawns = DrawBoard.instance.formSpawnList(DrawBoard.loadedVersion.mapId, DrawBoard.loadedVersion.version)
         };
      }

      private static int runningDownloadTasks
      {
         get { return downloadTasks.Count(t => !t.HasEnded); }
      }

      private static int runningUploadTasks
      {
         get { return uploadTasks.Count(t => !t.HasEnded); }
      }

      private static Action<Exception> logException (string fileName, string header) {
         return (ex) => {
            doIOAction(() => File.WriteAllText(rootReportFolder + @"\" + fileName, header + Environment.NewLine + ex.ToString()));
         };
      }

      private static void logUnhandled (string message) {
         logs += message + Environment.NewLine;
      }

      private static Action<Exception> encounteredError
      {
         get
         {
            return (ex) => {
               UI.loadingPanel.close();
               UI.messagePanel.displayError("Encountered an error while resolving issues:\n" + ex.ToString());
               foreach (Task task in downloadTasks.Union(uploadTasks)) {
                  if (!task.HasEnded) {
                     task.Abort();
                  }
               }
               instance.StopAllCoroutines();
               running = false;

               File.WriteAllText(rootReportFolder + @"\logs.txt", logs);
            };
         }
      }

      private static Layer getLayer (string name, int sublayer) {
         return DrawBoard.instance.layers[name].subLayers[sublayer];
      }

      public struct PlacedTile
      {
         public TileBase tileBase { get; set; }
         public Vector3Int position { get; set; }
         public string layer { get; set; }
         public int sublayer { get; set; }

         public override string ToString () {
            return $"{ tileBase.name }, { position.x }:{ position.y }, { layer }_{ sublayer }";
         }
      }

      public struct IssueResolvingResult
      {
         public Exception exception { get; set; }
         public List<LayerChangeResult> layerChanges { get; set; }
         public List<TilesToPrefabChangeResult> tilesToPrefabChanges { get; set; }
         public List<PlacedTile> removedUnnecessaryTiles { get; set; }
         public List<string> removedDataFields { get; set; }

         public string formReport () {
            if (exception != null) {
               return "Encountered an exception: " + Environment.NewLine + exception.ToString() + Environment.NewLine;
            } else {
               return
                  toStringLines(layerChanges.Select(l => l.ToString()), "Layer change - ") +
                  toStringLines(tilesToPrefabChanges.Select(t => t.ToString()), "Tiles to prefab - ") +
                  toStringLines(removedUnnecessaryTiles.Select(t => t.ToString()), "Removed =");
            }
         }
      }

      public struct LayerChangeResult
      {
         public PlacedTile misplacedTile { get; set; }
         public PlacedTile fixedWithTile { get; set; }
         public PlacedTile? blockingTile { get; set; }

         public override string ToString () {
            return $"{misplacedTile.tileBase.name} ({misplacedTile.position.x}:{misplacedTile.position.y}): " +
                  $"{misplacedTile.layer}_{misplacedTile.sublayer} -> {fixedWithTile.layer}_{fixedWithTile.sublayer}" +
                  (blockingTile == null ? "" : " | had to remove: " + blockingTile);
         }
      }

      public struct TilesToPrefabChangeResult
      {
         public List<PlacedTile> removedTiles { get; set; }
         public GameObject placedPrefab { get; set; }
         public Vector3 placedAtPosition { get; set; }

         public override string ToString () {
            return placedPrefab.name + " at position " + placedAtPosition.ToString() + " instead of tiles at positions " +
               string.Join(", ", removedTiles.Select(t => t.position.x + ":" + t.position.y));
         }
      }

      public struct MapSummary
      {
         public string mapName { get; set; }
         public int misuedTilesBeforeResolving { get; set; }
         public int misuedTilesAfterResolving { get; set; }
         public IssueResolvingResult? issueResolvingResult { get; set; }

         public string toLongSummary () {
            return
            new string('=', 100) + Environment.NewLine +
               mapName + Environment.NewLine +
               new string('-', mapName.Length) + Environment.NewLine +
               misuedTilesBeforeResolving + " tiles were misused before resolving issues" + Environment.NewLine +
               misuedTilesAfterResolving + " tiles were misused after resolving issues" + Environment.NewLine +
               "Issue resolver status - " + issueResolverStatus() + Environment.NewLine +
               "Attempted to change layer of " + resolvedTileWrongLayers + " tiles" + Environment.NewLine +
               "Added " + resolvedPrefabsFromTiles + " prefabs instead of " + removedTilesForPrefabs + " tiles" + Environment.NewLine +
               "Removed " + removedUnnecessaryTiles + " unnecessary tiles" + Environment.NewLine +
               "Removed " + removedDataFields + " missing data fields" + Environment.NewLine +
               new string('=', 100) + Environment.NewLine;
         }

         public string toShortSummary () {
            int resolvedIssues = resolvedTileWrongLayers + resolvedPrefabsFromTiles + removedUnnecessaryTiles + removedDataFields;
            return mapName + " - " + misuedTilesBeforeResolving + "->" + misuedTilesAfterResolving +
               " misusedTiles. Resolver: " + issueResolverStatus() + ", detected " + resolvedIssues + " issues";
         }

         public int resolvedTileWrongLayers
         {
            get
            {
               return issueResolvingResult == null || issueResolvingResult.Value.exception != null ? 0
                  : issueResolvingResult.Value.layerChanges.Count;
            }
         }

         public int removedUnnecessaryTiles
         {
            get
            {
               return issueResolvingResult == null || issueResolvingResult.Value.exception != null ? 0
                  : issueResolvingResult.Value.removedUnnecessaryTiles.Count;
            }
         }

         public int resolvedPrefabsFromTiles
         {
            get
            {
               return issueResolvingResult == null || issueResolvingResult.Value.exception != null ? 0
                  : issueResolvingResult.Value.tilesToPrefabChanges.Count;
            }
         }

         public int removedDataFields
         {
            get
            {
               return issueResolvingResult == null || issueResolvingResult.Value.exception != null ? 0
                  : issueResolvingResult.Value.removedDataFields.Count;
            }
         }

         public int removedTilesForPrefabs
         {
            get
            {
               return issueResolvingResult == null || issueResolvingResult.Value.exception != null ? 0
                  : issueResolvingResult.Value.tilesToPrefabChanges.SelectMany(t => t.removedTiles).Count();
            }
         }

         public string issueResolverStatus () {
            if (issueResolvingResult == null) {
               return "not excecuted";
            } else {
               if (issueResolvingResult.Value.exception == null) {
                  return "finished successfully";
               } else {
                  return "encountered an exception";
               }
            }
         }
      }

      [Serializable]
      public struct Config
      {
         public bool alterData;
         public bool saveMaps;
         public bool createNewVersion;
         public bool publishMapIfLatest;
      }
   }
}