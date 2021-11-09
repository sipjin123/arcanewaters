public class SupportTicketInfo
{
   #region Public Variables

   // The ticket's title
   public string ticketSubject;

   // The ticket's description
   public string ticketDescription;

   // The ticket's source account's id
   public int sourceAccId;

   // The ticket's source user's id
   public int sourceUsrId;

   // The ticket's source user's name
   public string sourceUsrName;

   // The ticket's target account's id
   public int targetAccId;

   // The ticket's target user's id
   public int targetUsrId;

   // The ticket's target user's name
   public string targetUsrName;

   // The ticket's source type
   public WebToolsUtil.ActionSource sourceType = WebToolsUtil.ActionSource.Game;

   // The ticket's type
   public WebToolsUtil.SupportTicketType ticketType;

   // The ticket's Ip Address
   public string ticketIpAddress;

   // The ticket's machine identifier
   public string ticketMachineIdentifier;

   // The player's position
   public string playerPosition;

   // The ticket's deployment id
   public int deploymentId;

   // The ticket's steam state
   public string steamState;

   #endregion

   public SupportTicketInfo () { }

   public SupportTicketInfo (string ticketSubject, string ticketDescription, int sourceAccId, int sourceUsrId, string sourceUsrName, int targetAccId,
      int targetUsrId, string targetUsrName, string ticketIpAddress, WebToolsUtil.SupportTicketType ticketType, WebToolsUtil.ActionSource sourceType = WebToolsUtil.ActionSource.Game) {
      this.ticketSubject = ticketSubject;
      this.ticketDescription = ticketDescription;
      this.sourceAccId = sourceAccId;
      this.sourceUsrId = sourceUsrId;
      this.sourceUsrName = sourceUsrName;
      this.targetAccId = targetAccId;
      this.targetUsrId = targetUsrId;
      this.targetUsrName = targetUsrName;
      this.sourceType = sourceType;
      this.ticketType = ticketType;
      this.ticketIpAddress = ticketIpAddress;
   }

   #region Private Variables

   #endregion
}
