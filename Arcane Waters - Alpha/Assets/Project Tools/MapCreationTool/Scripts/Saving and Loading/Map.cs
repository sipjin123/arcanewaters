using System;

namespace MapCreationTool.Serialization
{
   [Serializable]
   public class Map
   {
      public int id;
      public string name;
      public string displayName;
      public DateTime createdAt;
      public int creatorID;
      public int publishedVersion = -1;
      public int sourceMapId;
      public string notes;
      public EditorType editorType;
      public Biome.Type biome;
      public Area.SpecialType specialType;
      public WeatherEffectType weatherEffectType;
      public int maxPlayerCount;
      public PvpGameMode pvpGameMode;
      public PvpArenaSize pvpArenaSize;
      public bool spawnsSeaMonsters = true;
      public string creatorName;
      public int specialState = 0;
   }
}
