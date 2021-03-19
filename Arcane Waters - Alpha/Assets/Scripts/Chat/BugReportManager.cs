using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MiniJSON;
using SteamLoginSystem;
using UnityEngine.Networking;

public class BugReportManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static BugReportManager self;

   // Reference to main canvas
   public Canvas canvasGUI;

   #endregion

   public void Awake () {
      self = this;
   }

   [ServerOnly]
   public void storeBugReportOnServer (NetEntity player, string subject, string message, int ping, int fps, byte[] screenshotBytes, string screenResolution, string operatingSystem, string steamState, string ipAddress, int deploymentId) {
      // Check when they last submitted a bug report
      if (_lastBugReportTime.ContainsKey(player.userId)) {
         float timeSinceLastReport = Time.time - _lastBugReportTime[player.userId];
         if (timeSinceLastReport < BUG_REPORT_INTERVAL) {
            D.warning("Ignoring bug report from player: " + player.userId + " who submitted one: " +
                timeSinceLastReport + " seconds ago.");
            return;
         }
      }

      // Keep track of the time they last submitted a bug report
      _lastBugReportTime[player.userId] = Time.time;

      string playerPosition = AreaManager.self.getArea(player.areaKey) + ": (" + player.gameObject.transform.position.x.ToString() + "; " + player.gameObject.transform.position.y.ToString() + ")";

      // Save the report in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         long bugId = DB_Main.saveBugReport(player, subject, message, ping, fps, playerPosition, screenshotBytes, screenResolution, operatingSystem, deploymentId, steamState, ipAddress);

         // Send a confirmation to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.BugReport, player, bugId.ToString());
         });
      });
   }

   [ServerOnly]
   public void storeBugReportOnServerScreenshot (NetEntity player, long bugId, byte[] screenshotBytes) {
      if (bugId != -1) {
         // Save the screenshot in the database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.saveBugReportScreenshot(player, bugId, screenshotBytes);
         });
      }
   }

   public void sendBugReportScreenshotToServer (long bugId) {
      Global.player.rpc.Cmd_BugReportScreenshot(bugId, _screenshotBytes);
   }

   public void sendBugReportToServer (string subjectString) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendBugReport(subjectString));
   }

   private IEnumerator CO_CollectDataAndSendBugReport (string subjectString) {
      NetEntity player = Global.player;
      string bugReport = D.getLogString();
      int ping = Util.getPing();

      if (player == null) {
         D.warning("Can't submit a bug report because we have no player object.");
         yield break;
      }

      // Require a decent subject
      if (subjectString.Length < MIN_SUBJECT_LENGTH) {
         ChatManager.self.addChat("Please include a descriptive subject for the bug report.", ChatInfo.Type.System);
         yield break;
      }

      // Make sure the subject isn't too long
      if (subjectString.Length > MAX_SUBJECT_LENGTH) {
         ChatManager.self.addChat("The subject of the bug report is too long.", ChatInfo.Type.System);
         yield break;
      }

      int userId = player.userId;

      // Make sure we're not spamming the server
      if (_lastBugReportTime.ContainsKey(userId)) {
         if (Time.time - _lastBugReportTime[userId] < BUG_REPORT_INTERVAL) {
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
      string operatingSystem = SystemInfo.operatingSystem;
      string steamState = SteamLoginManager.getSteamState();

      int width = Screen.width;
      int height = Screen.height;
      //int maxPacketSize = Transport.activeTransport.GetMaxPacketSize();

      addMetaDataToBugReport(ref bugReport);

      // Getting deploymentId on client side
      int deploymentId = Util.getDeploymentId();

      // Getting player area
      string playerPosition = AreaManager.self.getArea(player.areaKey) + ": (" + player.gameObject.transform.position.x.ToString() + "; " + player.gameObject.transform.position.y.ToString() + ")";

      // Getting the screenshot
      Texture2D standardTex = takeScreenshot(width, height);

      // Sending the request to Web Tools
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

      // Adding bug report data as form data
      formData.Add(new MultipartFormDataSection("secretToken", secretToken));
      formData.Add(new MultipartFormDataSection("userId", player.userId.ToString()));
      formData.Add(new MultipartFormDataSection("username", player.entityName));
      formData.Add(new MultipartFormDataSection("accId", player.accountId.ToString()));
      formData.Add(new MultipartFormDataSection("subject", subjectString));
      formData.Add(new MultipartFormDataSection("log", bugReport));
      formData.Add(new MultipartFormDataSection("ping", ping.ToString()));
      formData.Add(new MultipartFormDataSection("fps", fps.ToString()));
      formData.Add(new MultipartFormDataSection("resolution", screenResolution));
      formData.Add(new MultipartFormDataSection("operatingSystem", operatingSystem));
      formData.Add(new MultipartFormDataSection("steamState", steamState));
      formData.Add(new MultipartFormDataSection("deploymentId", deploymentId.ToString()));
      formData.Add(new MultipartFormDataSection("playerPosition", playerPosition));

      // Adding the screenshot as a form file
      formData.Add(new MultipartFormFileSection(standardTex.EncodeToPNG()));

      UnityWebRequest www = UnityWebRequest.Post("https://tools.arcanewaters.com/api/tasks/submit", formData);
      yield return www.SendWebRequest();

      if (www.responseCode == 201) {
         ChatManager.self.addChat("Bug report submitted successfully, thanks!", ChatInfo.Type.System);
      } else {
         D.error("Could not submit bug report, needs investigation");
      }

      // Not using the server
      //Global.player.rpc.Cmd_BugReport(subjectString, bugReport, ping, fps, new byte[0], screenResolution, operatingSystem, steamState, deploymentId);

      // Not compressing the screenshots, for now
      //// Find image quality of size small enough to send through Mirror Networking      
      //Texture2D standardTex = takeScreenshot(width, height);
      //_screenshotBytes = standardTex.EncodeToPNG();
      //if (_screenshotBytes.Length < maxPacketSize) {
      //   // Full quality image
      //} else {
      //   // Skip every other row and column (no quality loss except minimap and fonts, because assets are using 200% scale)
      //   Texture2D skippedRowsTex = removeEvenRowsAndColumns(standardTex);
      //   _screenshotBytes = skippedRowsTex.EncodeToPNG();
      //   if (_screenshotBytes.Length < maxPacketSize) {
      //      // Full quality with removed rows and columns
      //   } else {
      //      // Try to use texture with skipped rows/columns with lower resolution and quality (JPG)
      //      int quality = 100;
      //      while (quality >= 0) {
      //         quality = Mathf.Max(1, quality);
      //         skippedRowsTex = removeEvenRowsAndColumns(standardTex);
      //         _screenshotBytes = skippedRowsTex.EncodeToJPG(quality);
      //         if (_screenshotBytes.Length < maxPacketSize) {
      //            break;
      //         }

      //         if (quality <= 1) {
      //            D.error("Something went wrong. Bug report system need to be investigated!");
      //         }
      //         quality -= 5;
      //      }
      //   }
      //}

      _lastBugReportTime[Global.player.userId] = Time.time;
   }

   private bool addMetaDataToBugReport (ref string bugReport) {
      // Make sure the bug report file hasn't grown too large
      if (System.Text.Encoding.Unicode.GetByteCount(bugReport) > MAX_BYTES) {
         int diff = (System.Text.Encoding.Unicode.GetByteCount(bugReport) - MAX_BYTES) / 2 + 1;
         bugReport = bugReport.Remove(0, diff);

         if (System.Text.Encoding.Unicode.GetByteCount(bugReport) > MAX_BYTES) {
            ChatManager.self.addChat("The bug report file is currently too large to submit.", ChatInfo.Type.System);
            return false;
         }
      }

      return true;
   }

   private Texture2D removeEvenRowsAndColumns (Texture2D tex) {
      List<Color[]> listColors = new List<Color[]>();
      List<Color> finalColors = new List<Color>();

      for (int y = 0; y < tex.height; y += 2) {
         listColors.Add(tex.GetPixels(0, y, tex.width, 1));
      }

      for (int i = 0; i < listColors.Count; i++) {
         for (int x = 0; x < listColors[i].Length; x += 2) {
            finalColors.Add(listColors[i][x]);
         }
      }
      tex = new Texture2D(tex.width / 2, tex.height / 2);
      tex.SetPixels(finalColors.ToArray());

      return tex;
   }

   private Texture2D takeScreenshot (int resWidth, int resHeight) {
      // Prepare data
      Camera camera = Camera.main;

      // Use battle camera if player is currently in battle
      if (BattleCamera.self.GetComponent<Camera>() && BattleCamera.self.GetComponent<Camera>().enabled) {
         camera = BattleCamera.self.GetComponent<Camera>();
      }

      RenderTexture savedCameraRT = camera.targetTexture;
      RenderTexture savedActiveRT = RenderTexture.active;
      RenderMode cameraRenderMode = canvasGUI.renderMode;
      Camera canvasWorldCamera = canvasGUI.worldCamera;
      float planeDistance = canvasGUI.planeDistance;

      // Temporary change render mode of Canvas to "Screen Space - Camera" to enable UI capture in screenshot
      if (cameraRenderMode != RenderMode.ScreenSpaceCamera) {
         canvasGUI.renderMode = RenderMode.ScreenSpaceCamera;
         canvasGUI.worldCamera = camera;
         canvasGUI.planeDistance = camera == Camera.main ? 1.0f : -2.0f;
      }

      // Create render texture, assign and render to it
      RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
      camera.targetTexture = rt;
      Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
      camera.Render();

      // Revert changes in Canvas
      canvasGUI.renderMode = cameraRenderMode;
      canvasGUI.worldCamera = canvasWorldCamera;
      canvasGUI.planeDistance = planeDistance;

      // Read pixels to Texture2D
      RenderTexture.active = rt;
      screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);

      // Cleanup
      camera.targetTexture = savedCameraRT;
      RenderTexture.active = savedActiveRT;
      Destroy(rt);

      return screenShot;
   }

   #region Private Variables

   // The amount of time to wait between consecutive bug reports
   protected static float BUG_REPORT_INTERVAL = 5f;

   // The minimum subject length for bugs
   protected static int MIN_SUBJECT_LENGTH = 3;

   // The max length of bug report subject lines
   protected static int MAX_SUBJECT_LENGTH = 256;

   // The maximum number of bytes we'll allow a submitted bug report to have
   protected static int MAX_BYTES = 64 * 1024;

   // The maximum number of string length we'll allow a submitted bug report to have
   protected static int MAX_LOG_LENGTH = 1024 * 6;

   // The time interval used to calculate the average fps
   protected static float FPS_CALCULATION_INTERVAL = 1f;

   // Stores the time at which a bug report was last submitted, indexed by user ID for the server's sake
   protected Dictionary<int, float> _lastBugReportTime = new Dictionary<int, float>();

   // Stored screenshot to send after confirmation of bug report
   protected byte[] _screenshotBytes;

   // Secret token used for submitting bug reports
   protected const string secretToken = "arcane_bug_reports_vjk53fx";

   #endregion
}
