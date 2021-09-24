using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class UserAccountInfo {
   #region Public Variables

   // The user's id
   public int userId;

   // The user's nama
   public string username;

   // The user's account id
   public int accountId;

   // The user's account name
   public string accountName;

   // Single Player status
   public bool isSinglePlayer;

   // Force Single Player status
   public bool forceSinglePlayer;

   #endregion

   public UserAccountInfo () { }

   #if IS_SERVER_BUILD
   public UserAccountInfo (MySqlDataReader dataReader) {
      this.userId = dataReader.GetInt32("usrId");
      this.username = dataReader.GetString("usrName");
      this.accountId = dataReader.GetInt32("accId");
      this.accountName = dataReader.GetString("accName");
      this.isSinglePlayer = dataReader.GetInt32("isSinglePlayer") == 1;
      this.forceSinglePlayer = dataReader.GetInt32("forceSinglePlayer") == 1;
   }
   #endif

   #region Private Variables

   #endregion
}
