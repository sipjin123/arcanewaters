using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class TreasureStateData {

   // The user id
   public int userId;

   // The treasure id based on the area
   public int treasureId;

   // Name of the area
   public string areaName;

   // The auto generated id 
   public int dataId;

#if IS_SERVER_BUILD

   public TreasureStateData (MySqlDataReader dataReader) {
      this.dataId = DataUtil.getInt(dataReader, "id");
      this.userId = DataUtil.getInt(dataReader, "userId");
      this.treasureId = DataUtil.getInt(dataReader, "chestId");
      this.areaName = DataUtil.getString(dataReader, "areaId");
   }

#endif
}