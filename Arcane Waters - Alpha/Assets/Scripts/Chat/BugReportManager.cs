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

   public void sendBugReport (string subjectString) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendBugReport(subjectString));
   }

   private IEnumerator CO_CollectDataAndSendBugReport (string bugSubject) {
      NetEntity player = Global.player;

      if (player == null) {
         D.warning("Can't submit a bug report because we have no player object.");
         yield break;
      }

      // Require a decent subject
      if (bugSubject.Length < WebToolsUtil.MinSubjectLength) {
         ChatManager.self.addChat("Please include a descriptive subject for the bug report.", ChatInfo.Type.System);
         yield break;
      }

      // Make sure the subject isn't too long
      if (bugSubject.Length > WebToolsUtil.MaxSubjectLength) {
         ChatManager.self.addChat("The subject of the bug report is too long.", ChatInfo.Type.System);
         yield break;
      }

      string clientLogs = D.getLogString();
      int ping = Util.getPing();
      int userId = player.userId;

      // Make sure we're not spamming the server
      if (_lastBugReportTime.ContainsKey(userId)) {
         if (Time.time - _lastBugReportTime[userId] < WebToolsUtil.ReportInterval) {
            D.warning("Bug report already submitted.");
            yield break;
         }
      }

      // Calculate the frames per second over the given time interval
      float totalTime = 0;
      int frameCount = 0;

      while (totalTime < FPS_CALCULATION_INTERVAL) {
         totalTime += Time.unscaledDeltaTime;
         frameCount++;
         yield return null;
      }

      int fps = Mathf.FloorToInt(((float) frameCount) / totalTime);

      string screenResolution = Screen.currentResolution.ToString();
      string gameResolution = Screen.width + " x " + Screen.height;
      string operatingSystem = SystemInfo.operatingSystem;
      string steamState = SteamLoginManager.getSteamState();

      //int maxPacketSize = Transport.activeTransport.GetMaxPacketSize();

      //addMetaDataToBugReport(ref bugReport);

      // Getting deploymentId on client side
      int deploymentId = Util.getDeploymentId();

      // Getting player area
      string playerPosition = WebToolsUtil.formatAreaPosition(AreaManager.self.getArea(player.areaKey), player.gameObject.transform.position.x, player.gameObject.transform.position.y);

      // Getting the screenshot
      yield return new WaitForEndOfFrame();
      Texture2D standardTex = ScreenCapture.CaptureScreenshotAsTexture();

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding bug report data as form data
      formData.Add(new MultipartFormDataSection("accId", player.accountId.ToString()));
      formData.Add(new MultipartFormDataSection("usrId", player.userId.ToString()));
      formData.Add(new MultipartFormDataSection("usrName", player.entityName));
      formData.Add(new MultipartFormDataSection("bugSubject", bugSubject));
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

      UnityWebRequest www = UnityWebRequest.Post(WebToolsUtil.BUG_REPORT_SUBMIT, formData);

      yield return www.SendWebRequest();

      if (www.responseCode == (int) WebToolsUtil.ResponseCode.Success) {
         ChatManager.self.addChat("Bug report submitted successfully, thanks!", ChatInfo.Type.System);

         int bugReportId = int.Parse(www.downloadHandler.text);

         // After successfully storing the bug report, we store the server log
         Global.player.rpc.Cmd_StoreServerLogForBugReport(bugReportId);

      } else {
         D.error("Could not submit bug report, needs investigation");
      }

      _lastBugReportTime[Global.player.userId] = Time.time;
   }

   [ServerOnly]
   public void sendBugReportServerLog (int bugReportId, byte[] serverLog) {
      StartCoroutine(CO_SendServerLog(bugReportId, serverLog));
   }

   [ServerOnly]
   private IEnumerator CO_SendServerLog (int bugReportId, byte[] serverLog) {
      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      formData.Add(new MultipartFormDataSection("bugId", bugReportId.ToString()));
      // We send the serverLog as a file!
      formData.Add(new MultipartFormFileSection("serverLog", serverLog));

      UnityWebRequest www = UnityWebRequest.Post(WebToolsUtil.BUG_REPORT_SERVER_LOG_SUBMIT, formData);

      // Send the game token as header
      www.SetRequestHeader(WebToolsUtil.GAME_TOKEN_HEADER, WebToolsUtil.GAME_TOKEN);

      yield return www.SendWebRequest();

      if (www.responseCode != 200) {
         D.error("Could not submit server log!");
      }
   }

   //private bool addMetaDataToBugReport (ref string bugReport) {
   //   // Make sure the bug report file hasn't grown too large
   //   if (System.Text.Encoding.Unicode.GetByteCount(bugReport) > MAX_BYTES) {
   //      int diff = (System.Text.Encoding.Unicode.GetByteCount(bugReport) - MAX_BYTES) / 2 + 1;
   //      bugReport = bugReport.Remove(0, diff);

   //      if (System.Text.Encoding.Unicode.GetByteCount(bugReport) > MAX_BYTES) {
   //         ChatManager.self.addChat("The bug report file is currently too large to submit.", ChatInfo.Type.System);
   //         return false;
   //      }
   //   }

   //   return true;
   //}

   //private Texture2D removeEvenRowsAndColumns (Texture2D tex) {
   //   List<Color[]> listColors = new List<Color[]>();
   //   List<Color> finalColors = new List<Color>();

   //   for (int y = 0; y < tex.height; y += 2) {
   //      listColors.Add(tex.GetPixels(0, y, tex.width, 1));
   //   }

   //   for (int i = 0; i < listColors.Count; i++) {
   //      for (int x = 0; x < listColors[i].Length; x += 2) {
   //         finalColors.Add(listColors[i][x]);
   //      }
   //   }
   //   tex = new Texture2D(tex.width / 2, tex.height / 2);
   //   tex.SetPixels(finalColors.ToArray());

   //   return tex;
   //}

   #region Private Variables

   // The amount of time to wait between consecutive bug reports
   //protected static float BUG_REPORT_INTERVAL = 5f;

   // The minimum subject length for bugs
   //protected static int MIN_SUBJECT_LENGTH = 3;

   // The max length of bug report subject lines
   //protected static int MAX_SUBJECT_LENGTH = 256;

   // The maximum number of bytes we'll allow a submitted bug report to have
   //protected static int MAX_BYTES = 5 * 1024 * 1024;

   // The maximum number of string length we'll allow a submitted bug report to have
   protected static int MAX_LOG_LENGTH = 1024 * 6;

   // The time interval used to calculate the average fps
   protected static float FPS_CALCULATION_INTERVAL = 1f;

   // Stores the time at which a bug report was last submitted, indexed by user ID for the server's sake
   protected Dictionary<int, float> _lastBugReportTime = new Dictionary<int, float>();

   // Stored screenshot to send after confirmation of bug report
   //protected byte[] _screenshotBytes;

   // Secret token used for submitting bug reports
   //protected const string secretToken = "arcane_game_vjk53fx";

   #endregion
}
