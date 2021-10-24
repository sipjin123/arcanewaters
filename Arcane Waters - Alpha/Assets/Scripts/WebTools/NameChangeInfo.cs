using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class NameChangeInfo {
   #region Public Variables

   // The username change SQL id
   public int id;

   // The source account's id
   public int sourceAccId;

   // The source user's id
   public int sourceUsrId;

   // The source user's name
   public string sourceUsrName;

   // The target account's id
   public int targetAccId;

   // The target user's id
   public int targetUsrId;

   // The target user's previous name
   public string prevUsrName;

   // The target user's new name
   public string newUsrName;

   // The reason for this username change
   public string reason;

   // The source of this username change
   public WebToolsUtil.ActionSource changeSource;

   #endregion

   public NameChangeInfo () { }

   public NameChangeInfo (int sourceAccId, int sourceUsrId, string sourceUsrName, int targetAccId, int targetUsrId, string prevUsrName, string newUsrName, string reason, WebToolsUtil.ActionSource changeSource) {
      this.sourceAccId = sourceAccId;
      this.sourceUsrId = sourceUsrId;
      this.sourceUsrName = sourceUsrName;
      this.targetAccId = targetAccId;
      this.targetUsrId = targetUsrId;
      this.prevUsrName = prevUsrName;
      this.newUsrName = newUsrName;
      this.reason = reason;
      this.changeSource = changeSource;
   }

   #region Private Variables

   #endregion
}
