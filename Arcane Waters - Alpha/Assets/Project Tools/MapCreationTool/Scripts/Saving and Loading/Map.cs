using System;

namespace MapCreationTool.Serialization
{
   [Serializable]
   public class Map
   {
      public int id { get; set; }
      public string name { get; set; }
      public DateTime createdAt { get; set; }
      public int creatorID { get; set; }
      public int? publishedVersion { get; set; }
      public int sourceMapId { get; set; }
      public string notes { get; set; }
      public EditorType editorType { get; set; }
      public Biome.Type biome { get; set; }
      public Area.SpecialType specialType { get; set; }

      public string creatorName { get; set; }
   }
}
