using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using System.Linq;

namespace MapCreationTool
{
   public class RemoteSpawns : RemoteData<MapSpawn>
   {
      public Dictionary<int, List<MapSpawn>> mapSpawns { get; private set; }

      public SelectOption[] formSpawnsSelectOptions (int mapId) {
         if (mapSpawns.TryGetValue(mapId, out List<MapSpawn> spawns)) {
            return Enumerable.Repeat(new SelectOption("-1", ""), 1)
            .Union(spawns.Select(s => new SelectOption(s.name)))
            .ToArray();
         } else {
            return new SelectOption("-1", "").toArray();
         }
      }

      protected override List<MapSpawn> fetchData () {
         return DB_Main.getMapSpawns();
      }

      protected override void setData (List<MapSpawn> data) {
         mapSpawns = new Dictionary<int, List<MapSpawn>>();

         foreach (MapSpawn spawn in data) {
            if (mapSpawns.TryGetValue(spawn.mapId, out List<MapSpawn> spawns)) {
               if(!spawns.Any(s => s.name.CompareTo(spawn.name) == 0)) {
                  spawns.Add(spawn);
               }
            } else {
               mapSpawns.Add(spawn.mapId, new List<MapSpawn> { spawn });
            }
         }
      }
   }
}
