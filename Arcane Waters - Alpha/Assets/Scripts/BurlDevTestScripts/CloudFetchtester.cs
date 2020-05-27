using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudBuildDataFetch
{
   public class CloudFetchtester : MonoBehaviour
   {
      #region Public Variables

      // Data saving values
      public static string DIRECTORY = "C:/CloudDataFiles";
      public static string FILE_NAME = "buildList.txt";

      // User auth key for cloud build data request
      public static string USER_AUTH_KEY = "d437a754dc5d7c76979dea90a0049425";

      // Data cache
      public List<Root> rootList = new List<Root>();
      public CloudBuildData cloudData;

      // Build count to fetch
      public static int BUILD_FETCH_COUNT = 1;

      // How frequent the system checks for updates
      public static int CHECK_REPEAT_DELAY = 60;

      // Request parameters
      public static string DEFAULT_REQUEST_URL = "https://build-api.cloud.unity3d.com/api/v1/orgs/";
      public static string ORG_ID = "20066090156130";
      public static string PROJECT_ID = "0ea8276a-68a2-46b7-b2d9-8c33417a2770";
      public static string BUILD_TARGET = "windows-desktop-64-bit-client";

      #endregion

      private void Awake () {
         if (!Directory.Exists(DIRECTORY)) {
            Directory.CreateDirectory(DIRECTORY);
         }

         string fileDirectory = DIRECTORY + "/" + FILE_NAME + ".txt";
         if (!System.IO.File.Exists(fileDirectory)) {
            System.IO.File.Create(fileDirectory).Close();
         }
      }

      private void Start () {
         InvokeRepeating("triggerCloudChecker", 3, CHECK_REPEAT_DELAY);
      }

      private void triggerCloudChecker () {
         Debug.Log("Triggered Cloud checker");
         StartCoroutine(getBuildList());
      }

      private void processDatabaseBuild () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            cloudData = DB_Main.getCloudData();
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Debug.Log("Done Fetching Database Cloud Data");
               crossCheckBuildData();
            });
         });
      }

      private void crossCheckBuildData () {
         if (cloudData.buildId < rootList[0].build) {
            Debug.Log("Cloud database is outdated, now updating: " + cloudData.buildId + " to " + rootList[0].build);

            CloudBuildData newCloudBuildData = new CloudBuildData();
            newCloudBuildData.buildId = rootList[0].build;

            string compiledCommitMessage = "";
            foreach (Changeset commitLogs in rootList[0].changeset) {
               compiledCommitMessage += commitLogs.message + "\n";
            }
            newCloudBuildData.buildMessage = compiledCommitMessage;
            newCloudBuildData.buildDateTime = rootList[0].finished;

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               DB_Main.addNewCloudData(newCloudBuildData);
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Done");
               });
            });
         } else {
            Debug.Log("Cloud is up to date: " + cloudData.buildId);
         }
      }

      private void OnGUI () {
         if (GUILayout.Button("Get Auti Log")) {
            StartCoroutine(CO_GetAuditLog());
         }
         if (GUILayout.Button("Get Build List")) {
            StartCoroutine(getBuildList());
         }
         if (GUILayout.Button("Get Build Info")) {
            StartCoroutine(CO_GetBuildLog(511));
         }
         if (GUILayout.Button("Get Database Info")) {
            D.editorLog("Start Calling");
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               cloudData = DB_Main.getCloudData();
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Done");
               });
            });
         }
      }

      IEnumerator CO_GetAuditLog () {
         string webPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/auditlog";
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);

         WWW www = new WWW(webPage, null, headers);
         yield return www;
         Debug.LogError(www.text);
      }

      IEnumerator CO_GetBuildLog (int buildId) {
         string webPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/builds/" + buildId + "/";
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);

         WWW www = new WWW(webPage, null, headers);
         yield return www;
         Debug.LogError(www.text);
      }

      IEnumerator getBuildList () {
         yield return new WaitForSeconds(3);

         string requestPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/builds?per_page=" + BUILD_FETCH_COUNT;
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);
         WWW www = new WWW(requestPage, null, headers);
         yield return www;

         // Write file to data for review
         string fileDirectory = DIRECTORY + "/" + FILE_NAME + ".txt";
         System.IO.File.WriteAllText(fileDirectory, www.text);

         // Data translation and extraction
         JArray newArray = (JArray) JsonConvert.DeserializeObject(www.text);
         for (int i = 0; i < newArray.Count; i++) {
            Root contentRoot = JsonUtility.FromJson<Root>(newArray[i].ToString());
            rootList.Add(contentRoot);
         }

         Debug.Log("Done Fetching Unity Cloud Data");
         processDatabaseBuild();
      }

      #region Private Variables

      #endregion
   }
}