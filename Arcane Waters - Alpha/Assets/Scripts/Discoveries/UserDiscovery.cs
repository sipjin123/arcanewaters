using UnityEngine;
using System;

[Serializable]
public class UserDiscovery
{
   #region Public Variables

   // The id of the user
   public int userId;

   // The id of the placed discovery, assigned from the map editor
   public int placedDiscoveryId;

   // Has this discovery been discovered by this user
   public bool discovered = false;

   #endregion

#if IS_SERVER_BUILD

   public UserDiscovery () {

   }

   public UserDiscovery (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      userId = DataUtil.getInt(dataReader, "userId");
      placedDiscoveryId = DataUtil.getInt(dataReader, "placedDiscoveryId");
      discovered = DataUtil.getInt(dataReader, "discovered") != 0;
   }

#endif

   #region Private Variables

   #endregion
}
