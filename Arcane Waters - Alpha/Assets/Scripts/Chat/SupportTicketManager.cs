using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using SteamLoginSystem;

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

   public void requestComplaint (string parameters) {
      List<string> values = parameters.Split(' ').ToList();

      if (values.Count < 2) {
         ChatManager.self.addChat("Please specify an username and a description for the complaint.", ChatInfo.Type.System);
         return;
      }

      string username = values[0];
      string description = string.Join(" ", values.Skip(1).Take(values.Count - 1)).Trim();

      if (description.Length < _minDescriptionLength) {
         ChatManager.self.addChat("The description for this complaint is too short.", ChatInfo.Type.System);
         return;
      }

      Global.player.rpc.Cmd_SubmitComplaint(username, description);
   }

   public void sendComplaint (int targetAccId, int targetUsrId, string targetUsername, string description) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendComplaint(targetAccId, targetUsrId, targetUsername, description));
   }

   private IEnumerator CO_CollectDataAndSendComplaint (int targetAccId, int targetUsrId, string targetUsername, string description) {
      NetEntity player = Global.player;

      if (player == null) {
         D.warning("Can't submit a complaint because we don't have a player object");
         yield break;
      }

      // Make sure we're not spamming the server
      if (_lastTicketTime.ContainsKey(player.userId)) {
         if (Time.time - _lastTicketTime[player.userId] < _complaintInterval) {
            D.warning("Complaint already submitted");
            yield break;
         }
      }

      string chatLogs = ChatManager.self.getChatLog();
      string machineIdentifier = SystemInfo.deviceName;
      string steamState = SteamLoginManager.getSteamState();

      int deploymentId = Util.getDeploymentId();

      // Getting player area
      string playerPosition = Util.formatAreaPosition(AreaManager.self.getArea(player.areaKey), player.gameObject.transform.position.x, player.gameObject.transform.position.y);

      // Getting the screenshot
      yield return new WaitForEndOfFrame();
      Texture2D standardTex = ScreenCapture.CaptureScreenshotAsTexture();

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding complaint data as form data
      formData.Add(new MultipartFormDataSection("ticketDescription", description));
      formData.Add(new MultipartFormDataSection("sourceAccId", player.accountId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceUsrId", player.userId.ToString()));
      formData.Add(new MultipartFormDataSection("sourceUsrName", player.entityName));
      formData.Add(new MultipartFormDataSection("targetAccId", targetAccId.ToString()));
      formData.Add(new MultipartFormDataSection("targetUsrId", targetUsrId.ToString()));
      formData.Add(new MultipartFormDataSection("targetUsrName", targetUsername));
      formData.Add(new MultipartFormDataSection("ticketMachineIdentifier", machineIdentifier));
      formData.Add(new MultipartFormDataSection("playerPosition", playerPosition));
      formData.Add(new MultipartFormDataSection("deploymentId", deploymentId.ToString()));

      if (!string.IsNullOrEmpty(steamState)) {
         formData.Add(new MultipartFormDataSection("steamState", steamState));
      }

      if (!string.IsNullOrEmpty(chatLogs)) {
         formData.Add(new MultipartFormFileSection("chatLogs", Encoding.ASCII.GetBytes(chatLogs), "chatLogs.txt", "text/plain"));
      }

      // Adding the screenshot as a form file
      formData.Add(new MultipartFormFileSection("screenshot", standardTex.EncodeToPNG(), "screenshot.png", "image/png"));

      // Getting token for security reasons. This token is unique and expires in 1 minute
      UnityWebRequest wwwToken = UnityWebRequest.Get(_tokenEndpoint);

      yield return wwwToken.SendWebRequest();

      if (wwwToken.responseCode == 200) {
         string token = wwwToken.downloadHandler.text;

         UnityWebRequest wwwSubmit = UnityWebRequest.Post(_submitEndpoint, formData);
         wwwSubmit.SetRequestHeader("Token", token);

         yield return wwwSubmit.SendWebRequest();

         if (wwwSubmit.responseCode == 200) {
            ChatManager.self.addChat("Complaint submitted successfully.", ChatInfo.Type.System);
         } else {
            ChatManager.self.addChat("Could not submit complaint", ChatInfo.Type.Error);
         }

         _lastTicketTime[Global.player.userId] = Time.time;
      } else {
         D.error("Could not get token for complaint submit action.");
      }
   }

   #region Private Variables

   // The token endpoint
   //private string _tokenEndpoint = "https://localhost:5001/api/Tokens/Generate";
   private string _tokenEndpoint = "https://tools.arcanewaters.com/api/Tokens/Generate";

   //private string _submitEndpoint = "https://localhost:5001/api/Tickets/Submit";
   private string _submitEndpoint = "https://tools.arcanewaters.com/api/Tickets/Submit";

   // The amount of time to wait between consecutive complaints
   private float _complaintInterval = 5f;

   // Minimum length for description
   private int _minDescriptionLength = 3;

   // Stores the time at which a complaint was last submitted, indexed by user ID for the server's sake
   private Dictionary<int, float> _lastTicketTime = new Dictionary<int, float>();

   #endregion
}
