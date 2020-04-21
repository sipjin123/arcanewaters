using UnityEngine;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[System.Serializable]
public class DiscoveryData
{
   #region Public Variables

   // The name of the discovery
   public string name;

   // The description of the discovery
   public string description;

   // The reference image url
   public string spriteUrl;

   // The spawn chances of this discovery
   [Range(0, 1)]
   public Rarity.Type rarity = Rarity.Type.Common;

   // The unique ID of this discovery
   public int discoveryId = 0;

   #endregion

#if IS_SERVER_BUILD

   public DiscoveryData (MySqlDataReader reader) {
      this.name = reader.GetString("discoveryName");
      this.description = reader.GetString("discoveryDescription");
      this.discoveryId = reader.GetInt32("discoveryId");
      this.spriteUrl = reader.GetString("sourceImageUrl");
      this.rarity = (Rarity.Type)reader.GetInt32("rarity");
   }

#endif

   public DiscoveryData (string name, string description, int discoveryId, string spriteUrl, Rarity.Type rarity) {
      this.name = name;
      this.description = description;
      this.discoveryId = discoveryId;
      this.spriteUrl = spriteUrl;
      this.rarity = rarity;
   }

   public DiscoveryData () { }

   #region Private Variables

   #endregion
}
