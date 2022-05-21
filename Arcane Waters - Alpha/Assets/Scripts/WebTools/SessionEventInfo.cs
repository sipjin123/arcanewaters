public class SessionEventInfo
{
   #region Public Variables

   // The account id of this event
   public int accId;

   // The user id of this event
   public int usrId;

   // The username of the player
   public string usrName;

   // The IP address of the player
   public string ipAddress;

   // The type of this event
   public Type eventType;
   
   // The machine identifier of the player
   public string machineIdent;
   
   // Deployment's id of the game
   public int deploymentId;

   // The types of session events
   public enum Type
   {
      Login = 1,
      UserCreate = 2,
      UserDestroy = 3
   }

   public SessionEventInfo (int accId, int usrId, string usrName, string ipAddress, Type eventType, string machineIdent, int deploymentId) {
      this.accId = accId;
      this.usrId = usrId;
      this.usrName = usrName;
      this.ipAddress = ipAddress;
      this.eventType = eventType;
      this.machineIdent = machineIdent;
      this.deploymentId = deploymentId;
   }

   #endregion



   #region Private Variables

   #endregion
}