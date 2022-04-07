using UnityEngine;

// We are injecting ourselves into the Mail system,
// because there's no other easy way without rewriting a bunch of stuff.
// Currently, items the belong to Mail have a userId = -mailId.
// We'll use this negative ID and a fake mail entry to create a group of items that don't belong to any player
// This class is just an empty entry into the 'Mail' table, just to get a unique mailId to assign items to

/// <summary>
/// Used to create a set of items that exists in the game but does not belong to any 1 player.
/// See additional comments above the class for more explanation
/// </summary>
public class CustomItemCollection : MailInfo
{
   #region Public Variables

   // We used mailId as our collection Id
   public int id
   {
      get { return mailId; }
      set { mailId = value; }
   }

   #endregion

   public CustomItemCollection (int mailId = 0) {
      // All that matters for a CustomItemCollection is the Unique ID
      this.mailId = mailId;

      // Everything else can just be set to whatever
      recipientUserId = 0;
      senderUserId = 0;
      senderUserName = "Used by CustomItemCollection.cs";
      receptionDate = 0;
      isRead = false;
      mailSubject = "Used by CustomItemCollection.cs";
      message = "Used by CustomItemCollection.cs";

      // Attached item count doesn't do anything functionally
      // It's just a visual for mail UI
      attachedItemsCount = 0;

      // Don't delete this ever
      autoDelete = false;

      // Don't send it back
      sendBack = false;
   }

#if IS_SERVER_BUILD

   public CustomItemCollection (MySql.Data.MySqlClient.MySqlDataReader dataReader, bool isForList) : base(dataReader, isForList) { }

#endif

   #region Private Variables

   #endregion
}
