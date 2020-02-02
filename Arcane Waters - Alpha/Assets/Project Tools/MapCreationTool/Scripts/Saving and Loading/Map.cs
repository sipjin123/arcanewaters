using System;

namespace MapCreationTool.Serialization
{
   public class Map
   {
      public string name { get; set; }
      public DateTime createdAt { get; set; }
      public int creatorID { get; set; }
      public int? publishedVersion { get; set; }

      public string creatorName { get; set; }
   }
}
