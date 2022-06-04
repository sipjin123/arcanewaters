using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SteamLoginSystem;
using UnityEngine.Networking;
using System.Text;

public class BugReportManager : GenericGameManager
{
   #region Public Variables

   // Self
   public static BugReportManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void sendBugReport (string subject) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendBugReport(subject));
   }

   private IEnumerator CO_CollectDataAndSendBugReport (string subject) {
      NetEntity player = Global.player;

      if (player == null) {
         D.warning("Can't submit a bug report because we have no player object.");
         yield break;
      }

      // Require a decent subject
      if (subject.Length < _minSubjectLength) {
         ChatManager.self.addChat("Please include a descriptive subject for the bug report.", ChatInfo.Type.System);
         yield break;
      }

      // Make sure the subject isn't too long
      if (subject.Length > _maxSubjectLength) {
         ChatManager.self.addChat("The subject of the bug report is too long.", ChatInfo.Type.System);
         yield break;
      }

      string clientLogs = D.getLogString();
      int ping = Util.getPing();
      int userId = player.userId;

      // Make sure we're not spamming the server
      if (_lastBugReportTime.ContainsKey(userId)) {
         if (Time.time - _lastBugReportTime[userId] < _bugReportInterval) {
            D.warning("Bug report already submitted.");
            yield break;
         }
      }

      // Calculate the frames per second over the given time interval
      float totalTime = 0;
      int frameCount = 0;

      while (totalTime < _fpsCalculationInterval) {
         totalTime += Time.unscaledDeltaTime;
         frameCount++;
         yield return null;
      }

      int fps = Mathf.FloorToInt(((float) frameCount) / totalTime);

      string screenResolution = Screen.currentResolution.ToString();
      string gameResolution = Screen.width + " x " + Screen.height;
      string operatingSystem = SystemInfo.operatingSystem;
      string steamState = SteamLoginManager.getSteamState();

      // Getting deploymentId on client side
      int deploymentId = Util.getDeploymentId();

      // Getting player area
      string playerPosition = Util.formatAreaPosition(AreaManager.self.getArea(player.areaKey), player.gameObject.transform.position.x, player.gameObject.transform.position.y);

      // Getting the screenshot
      yield return new WaitForEndOfFrame();
      Texture2D standardTex = ScreenCapture.CaptureScreenshotAsTexture();

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding bug report data as form data
      formData.Add(new MultipartFormDataSection("accId", player.accountId.ToString()));
      formData.Add(new MultipartFormDataSection("usrId", player.userId.ToString()));
      formData.Add(new MultipartFormDataSection("usrName", player.entityName));
      formData.Add(new MultipartFormDataSection("bugSubject", subject));
      formData.Add(new MultipartFormDataSection("ping", ping.ToString()));
      formData.Add(new MultipartFormDataSection("fps", fps.ToString()));
      formData.Add(new MultipartFormDataSection("playerPosition", playerPosition));
      formData.Add(new MultipartFormDataSection("screenResolution", screenResolution));
      formData.Add(new MultipartFormDataSection("gameResolution", gameResolution));
      formData.Add(new MultipartFormDataSection("operatingSystem", operatingSystem));
      formData.Add(new MultipartFormDataSection("deploymentId", deploymentId.ToString()));

      if (!string.IsNullOrEmpty(steamState)) {
         formData.Add(new MultipartFormDataSection("steamState", steamState));
      }

      if (!string.IsNullOrEmpty(clientLogs)) {
         formData.Add(new MultipartFormFileSection("clientLogs", Encoding.ASCII.GetBytes(clientLogs), "clientLogs.txt", "text/plain"));
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
            ChatManager.self.addChat("Bug report submitted successfully, thanks!", ChatInfo.Type.System);

            int bugReportId = int.Parse(wwwSubmit.downloadHandler.text);

            // After successfully storing the bug report, we store the server log
            Global.player.rpc.Cmd_StoreServerLogForBugReport(bugReportId, token);

         } else {
            D.error("Could not submit bug report, needs investigation");
         }

         _lastBugReportTime[Global.player.userId] = Time.time;
      } else {
         D.error("Could not get token for bug report submit action.");
      }
   }

   [ServerOnly]
   public void sendBugReportServerLog (int bugReportId, byte[] serverLog, string token) {
      StartCoroutine(CO_SendServerLog(bugReportId, serverLog, token));
   }

   [ServerOnly]
   private IEnumerator CO_SendServerLog (int bugReportId, byte[] serverLog, string token) {
      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      formData.Add(new MultipartFormDataSection("bugId", bugReportId.ToString()));
      // We send the serverLog as a file!
      formData.Add(new MultipartFormFileSection("serverLog", serverLog));

      UnityWebRequest www = UnityWebRequest.Post(_submitLogsEndpoint, formData);

      // Send the game token as header
      www.SetRequestHeader("Token", token);

      yield return www.SendWebRequest();

      if (www.responseCode != 200) {
         D.error("Could not submit server log!");
      }
   }

   #region Private Variables

   // The token endpoint
   //private string _tokenEndpoint = "https://localhost:5001/api/Tokens/Generate";
   private string _tokenEndpoint = "https://tools.arcanewaters.com/api/Tokens/Generate";

   //private string _submitEndpoint = "https://localhost:5001/api/Tasks/Submit";
   private string _submitEndpoint = "https://tools.arcanewaters.com/api/Tasks/Submit";

   //private string _submitLogsEndpoint = "https://localhost:5001/api/Tasks/SubmitServerLogs";
   private string _submitLogsEndpoint = "https://tools.arcanewaters.com/api/Tasks/SubmitServerLogs";

   // The amount of time to wait between consecutive bug reports
   private float _bugReportInterval = 5f;

   // The minimum subject length for bugs
   private int _minSubjectLength = 3;

   // The maximum subject length for bugs
   private int _maxSubjectLength = 256;

   // The time interval used to calculate the average fps
   private float _fpsCalculationInterval = 1f;

   // Stores the time at which a bug report was last submitted, indexed by user ID for the server's sake
   private Dictionary<int, float> _lastBugReportTime = new Dictionary<int, float>();

   #endregion
}
