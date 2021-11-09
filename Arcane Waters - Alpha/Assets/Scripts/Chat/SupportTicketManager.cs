using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;
using System.Text;

public class SupportTicketManager : GenericGameManager
{
   #region Public Variables

   // Self
   public static SupportTicketManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void requestComplaint (string message) {
      // Format is "username details"
      List<string> values = message.Split(' ').ToList();
      if (values.Count >= 2) {
         string username = values[0];
         string details = string.Join(" ", values.Skip(1).Take(values.Count - 1)).Trim();
         Global.player.rpc.Cmd_ComplaintTicket(username, details);
      } else {
         ChatManager.self.addChat("You must specify 2 parameters for this command", ChatInfo.Type.System);
      }
   }

   public void submitComplaint (SupportTicketInfo ticket) {
      StartCoroutine(CO_CollectDataAndSendComplaint(ticket));
   }

   private IEnumerator CO_CollectDataAndSendComplaint (SupportTicketInfo ticket) {
      NetEntity player = Global.player;

      if (player == null) {
         D.warning("Can't submit a complaint because we don't have a player object");
         yield break;
      }

      // Make sure we're not spamming the server
      if (_lastTicketTime.ContainsKey(player.userId)) {
         if (Time.time - _lastTicketTime[player.userId] < WebToolsUtil.ReportInterval) {
            D.warning("Complaint already submitted");
            yield break;
         }
      }

      string chatLogs = ChatManager.self.getChatLog();
      ticket.ticketMachineIdentifier = SystemInfo.deviceName;
      ticket.deploymentId = Util.getDeploymentId();

      // Getting player area
      ticket.playerPosition = WebToolsUtil.formatAreaPosition(AreaManager.self.getArea(player.areaKey), player.gameObject.transform.position.x, player.gameObject.transform.position.y);

      yield return new WaitForEndOfFrame();

      // Getting the screenshot
      Texture2D standardTex = ScreenCapture.CaptureScreenshotAsTexture();

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding complaint data as form data
      formData.Add(new MultipartFormDataSection("ticketSubject", ticket.ticketSubject));
      formData.Add(new MultipartFormDataSection("sourceAccId", ticket.sourceAccId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceUsrId", ticket.sourceUsrId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceUsrName", ticket.sourceUsrName));
      formData.Add(new MultipartFormDataSection("targetAccId", ticket.targetAccId.ToString()));
      formData.Add(new MultipartFormDataSection("targetUsrId", ticket.targetUsrId.ToString()));
      formData.Add(new MultipartFormDataSection("targetUsrName", ticket.targetUsrName));
      formData.Add(new MultipartFormDataSection("sourceType", ((int) ticket.sourceType).ToString()));
      formData.Add(new MultipartFormDataSection("ticketType", ((int) ticket.ticketType).ToString()));
      formData.Add(new MultipartFormDataSection("ticketIpAddress", ticket.ticketIpAddress));
      formData.Add(new MultipartFormDataSection("ticketMachineIdentifier", ticket.ticketMachineIdentifier));
      formData.Add(new MultipartFormDataSection("playerPosition", ticket.playerPosition));
      formData.Add(new MultipartFormDataSection("deploymentId", ticket.deploymentId.ToString()));

      if (!string.IsNullOrEmpty(ticket.steamState)) {
         formData.Add(new MultipartFormDataSection("steamState", ticket.steamState));
      }

      if (!string.IsNullOrEmpty(chatLogs)) {
         formData.Add(new MultipartFormFileSection("chatLogs", Encoding.ASCII.GetBytes(chatLogs), "chatLogs.txt", "text/plain"));
      }

      // Adding the screenshot as a form file
      formData.Add(new MultipartFormFileSection("screenshot", standardTex.EncodeToPNG(), "screenshot.png", "image/png"));

      UnityWebRequest www = UnityWebRequest.Post(WebToolsUtil.SubmitTicket, formData);

      yield return www.SendWebRequest();

      if (www.responseCode == WebToolsUtil.SUCCESS) {
         ChatManager.self.addChat("Complaint submitted successfully, thanks!", ChatInfo.Type.System);
      } else {
         ChatManager.self.addChat("Could not submit complaint", ChatInfo.Type.Error);
      }

      _lastTicketTime[player.userId] = Time.time;
   }

   #region Private Variables

   // Stores the time at which a complaint was last submitted, indexed by user ID for the server's sake
   protected Dictionary<int, float> _lastTicketTime = new Dictionary<int, float>();

   #endregion
}
