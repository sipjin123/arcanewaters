﻿using System;
using System.Collections.Generic;

namespace MapCreationTool.Serialization
{
   public class MapVersion : IComparable<MapVersion>
   {
      public string mapName { get; set; }
      public int version { get; set; }
      public DateTime createdAt { get; set; }
      public DateTime updatedAt { get; set; }
      public string editorData { get; set; }
      public string gameData { get; set; }

      public Map map { get; set; }
      public List<MapSpawn> spawns { get; set; }

      public int CompareTo (MapVersion other) {
         int nameComp = mapName.CompareTo(other.mapName);
         if (nameComp != 0) {
            return nameComp;
         }
         return version - other.version;
      }
   }
}