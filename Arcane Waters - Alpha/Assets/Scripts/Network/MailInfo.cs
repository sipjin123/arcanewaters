using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class MailInfo 
{
   #region Public Variables

   // The mail ID
   public int mailId;

   // The user ID
   public int recipientUserId;

   // The sender ID
   public int senderUserId;

   // The name of the sender
   public string senderUserName;

   // The date of mail reception
   public long receptionDate;

   // Gets set to true when the mail has been read
   public bool isRead;

   // The subject of the mail
   public string mailSubject;

   // The message text
   public string message;

   // The number of attached items
   public int attachedItemsCount;

   #endregion

   public MailInfo () { }

#if IS_SERVER_BUILD

   public MailInfo (MySqlDataReader dataReader, bool isForList) {
      this.mailId = DataUtil.getInt(dataReader, "mailId");
      this.recipientUserId = DataUtil.getInt(dataReader, "recipientUsrId");
      this.senderUserId = DataUtil.getInt(dataReader, "senderUsrId");
      this.senderUserName = DataUtil.getString(dataReader, "usrName");
      this.receptionDate = DataUtil.getDateTime(dataReader, "receptionDate").ToBinary();
      this.isRead = DataUtil.getBoolean(dataReader, "isRead");
      this.mailSubject = DataUtil.getString(dataReader, "mailSubject");
      if (isForList) {
         this.attachedItemsCount = DataUtil.getInt(dataReader, "attachedItemCount");
      } else {
         this.message = DataUtil.getString(dataReader, "message");
      }
   }

#endif

   public MailInfo (int mailId, int recipientUserId, int senderUserId, DateTime receptionDate, bool isRead,
      string mailSubject, string message) {
      this.mailId = mailId;
      this.recipientUserId = recipientUserId;
      this.senderUserId = senderUserId;
      this.receptionDate = receptionDate.ToBinary();
      this.isRead = isRead;
      this.mailSubject = mailSubject;
      this.message = message;
   }

   public override bool Equals (object rhs) {
      if (rhs is MailInfo) {
         var other = rhs as MailInfo;
         return (mailId == other.mailId);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * mailId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
