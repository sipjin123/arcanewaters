using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class WarpTreasureSite : Warp {
   #region Public Variables

   #endregion

   protected override void Awake() {
      base.Awake();

      // Temporarily disable to push to Unity collab until problem with Networking in Treasure Sites is solved
      warpToRandomTreasureSite = true;

      if (NetworkServer.active) {
         chooseRandomArea();
      } else {
         // Fill with any data to enable colliders; Real data of warp is available only on server
         areaTarget = "a";
         spawnTarget = "a";
      }
   }

   private void chooseRandomArea () {
      // Find potential maps
      if (_randomTreasureSites.Count == 0) {
         foreach (string key in AreaManager.self.getAreaKeys()) {
            if (AreaManager.self.getAreaSpecialType(key) == Area.SpecialType.TreasureSite) {
               _randomTreasureSites.Add(key);
            }
         }
      }

      // Choose map and target spawn
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<MapSpawn> mapSpawns = DB_Main.getMapSpawns();

         // Process downloaded data
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            while (_randomTreasureSites.Count > 0) {
               areaTarget = _randomTreasureSites.ChooseRandom();
               foreach (MapSpawn spawn in mapSpawns) {
                  if (spawn.mapName == areaTarget) {
                     spawnTarget = spawn.name;
                     break;
                  }
               }

               // If map doesn't have any spawn, try another treasure site map
               if (spawnTarget == "") {
                  _randomTreasureSites.Remove(areaTarget);
                  areaTarget = "";
               } else {
                  break;
               }
            }

            // If there are no existing treasure site maps or no treasure site map has correct spawn target
            if (areaTarget == "" || spawnTarget == "") {
               D.error("No treasure site maps available");
            }
         });
      });
   }

   #region Private Variables

   // Maps tht can be chosen as destination of this warp
   private static List<string> _randomTreasureSites = new List<string>();

   #endregion
}
