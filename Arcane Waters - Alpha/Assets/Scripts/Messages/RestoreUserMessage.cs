using Mirror;

public class RestoreUserMessage : NetworkMessage
{
   #region Public Variables

   // The user id
   public int userId;

   #endregion

   public RestoreUserMessage () { }

   public RestoreUserMessage (int userId) {
      this.userId = userId;
   }
}
