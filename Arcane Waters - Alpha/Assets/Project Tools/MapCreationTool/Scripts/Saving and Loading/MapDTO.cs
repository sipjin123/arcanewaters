using System;

namespace MapCreationTool
{
   public class MapDTO
   {
      public string name { get; set; }
      public string editorData { get; set; }
      public string gameData { get; set; }
      public DateTime updatedAt { get; set; }
      public DateTime createdAt { get; set; }
      public int version { get; set; }
      public int? liveVersion { get; set; }
      public int creatorID { get; set; }
      public string creatorName { get; set; }
   }
}

