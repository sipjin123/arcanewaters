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
   }
}

