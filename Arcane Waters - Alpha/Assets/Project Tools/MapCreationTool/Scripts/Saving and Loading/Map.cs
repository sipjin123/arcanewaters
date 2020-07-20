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

      public string creatorName;
   }
}
