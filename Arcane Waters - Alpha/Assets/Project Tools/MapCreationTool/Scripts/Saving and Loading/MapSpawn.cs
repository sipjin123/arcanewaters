using System;

namespace MapCreationTool.Serialization
{
   public class MapSpawn
   {
      public string name { get; set; }
      public int mapId { get; set; }
      public int mapVersion { get; set; }
      public float posX { get; set; }
      public float posY { get; set; }

      public string mapName { get; set; }
   }
}

