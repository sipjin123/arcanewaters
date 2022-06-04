using Mirror;

public class RestoreUserMessage : NetworkMessage
{
   #region Public Variables

   // The user id
   public int userId;

   // New user name
   public string newUsername;

   // Change the user's name during the restore operation
   public bool changeUsername;

   #endregion

   public RestoreUserMessage () { }

   public RestoreUserMessage (int userId, string newUsername = "", bool changeUsername = false) {
      this.userId = userId;
      this.newUsername = newUsername;
      this.changeUsername = changeUsername;
   }
}
