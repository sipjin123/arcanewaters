using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

public class SupportTicketManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static SupportTicketManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   private void OnEnable () {
      Application.logMessageReceived += addErrorLogMessage;
   }

   private void OnDisable () {
      Application.logMessageReceived -= addErrorLogMessage;
   }

   private void addErrorLogMessage (string logString, string stackTrace, LogType type) {
      if (type == LogType.Exception) {
         D.log("[EXCEPTION]: " + logString + "\n" + stackTrace);
      }
   }

   public void sendComplaint (string message) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendComplaint(message));
   }

   private IEnumerator CO_CollectDataAndSendComplaint (string message) {
      NetEntity player = Global.player;

      if (player == null) {
         D.warning("Can't submit a complaint because we have no player object");
         yield break;
      }

      int sourceUsrId = player.userId;

      // Make sure we're not spamming the server
      if (_lastComplaintTime.ContainsKey(sourceUsrId)) {
         if (Time.time - _lastComplaintTime[sourceUsrId] < COMPLAINT_INTERVAL) {
            D.warning("Complaint already submitted");
            yield break;
         }
      }

      string targetUsername = extractComplainNameFromChat(message);
      string subject = extractComplainMessageFromChat(message, targetUsername);
      string machineIdentifier = SystemInfo.deviceName;

      int deploymentId = Util.getDeploymentId();

      string chatLog = ChatManager.self.getChatLog();

      // Getting player area
      string playerPosition = AreaManager.self.getArea(player.areaKey) + ": (" + player.gameObject.transform.position.x.ToString() + "; " + player.gameObject.transform.position.y.ToString() + ")";

      // Getting the screenshot
      int width = Screen.width;
      int height = Screen.height;
      Texture2D standardTex = BugReportManager.self.takeScreenshot(width, height);

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding complaint data as form data
      formData.Add(new MultipartFormDataSection("ticketSubject", subject));
      formData.Add(new MultipartFormDataSection("sourceUsrId", player.userId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceAccId", player.accountId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceUsrName", player.entityName));
      formData.Add(new MultipartFormDataSection("sourceMachineIdentifier", machineIdentifier));
      formData.Add(new MultipartFormDataSection("targetUsrName", targetUsername));

      if (!string.IsNullOrEmpty(chatLog)) {
         formData.Add(new MultipartFormDataSection("ticketLog", chatLog));
      }

      formData.Add(new MultipartFormDataSection("playerPosition", playerPosition));
      formData.Add(new MultipartFormDataSection("deploymentId", deploymentId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceType", ((int) TicketSourceType.Game).ToString()));
      formData.Add(new MultipartFormDataSection("branch", Util.getBranchType()));

      // Adding the screenshot as a form file
      formData.Add(new MultipartFormFileSection(standardTex.EncodeToPNG()));

      UnityWebRequest www = UnityWebRequest.Post(WebToolsUtil.COMPLAINT_SUBMIT, formData);

      // We need to set the GameToken Header
      www.SetRequestHeader(WebToolsUtil.GAME_TOKEN_HEADER, WebToolsUtil.GAME_TOKEN);

      yield return www.SendWebRequest();

      if (www.responseCode == WebToolsUtil.SUCCESS) {
         ChatManager.self.addChat("Complaint submitted successfully, thanks!", ChatInfo.Type.System);
      } else if (www.responseCode == WebToolsUtil.BAD_REQUEST) {
         int result = int.Parse(www.downloadHandler.text);
         string resultMessage = "";

         // -1 auto report, -2 player not found
         switch (result) {
            case -1:
               resultMessage = "You cannot report yourself!";
               break;
            case -2:
               resultMessage = $"Player with username '{targetUsername}' not found!";
               break;
         }

         ChatManager.self.addChat(resultMessage, ChatInfo.Type.System);
      } else {
         D.error("Could not submit complaint, needs investigation");
      }

      _lastComplaintTime[player.userId] = Time.time;
   }

   public string extractComplainNameFromChat (string message) {
      foreach (string command in ChatUtil.commandTypePrefixes[CommandType.Complain]) {
         message = message.Replace(command + " ", "");
      }

      StringBuilder extractedName = new StringBuilder();
      foreach (char letter in message) {
         if (letter == ' ') {
            break;
         }

         extractedName.Append(letter);
      }

      return extractedName.ToString();
   }

   public string extractComplainMessageFromChat (string message, string username) {
      foreach (string command in ChatUtil.commandTypePrefixes[CommandType.Complain]) {
         message = message.Replace(command, "");
      }

      return message.Replace($"{username} ", "").Trim();
   }

   #region Private Variables

   // The amount of time to wait between consecutive bug reports
   private static float COMPLAINT_INTERVAL = 5f;

   // Stores the time at which a complaint was last submitted, indexed by user ID for the server's sake
   protected Dictionary<int, float> _lastComplaintTime = new Dictionary<int, float>();

   #endregion
}
