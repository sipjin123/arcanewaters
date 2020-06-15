using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace CloudBuildDataFetch {
   public class CloudBuildDataFetcher : MonoBehaviour {
      #region Public Variables

      // Self
      public static CloudBuildDataFetcher self;

      // User auth key for cloud build data request
      public static string USER_AUTH_KEY = "d437a754dc5d7c76979dea90a0049425";

      // Data cache
      public List<Root> rootList = new List<Root>();
      public CloudBuildData cloudData;

      // Build count to fetch
      public static int BUILD_FETCH_COUNT = 1;

      // How frequent the system checks for updates
      public static int CHECK_REPEAT_DELAY = 300;

      // Request parameters
      public static string DEFAULT_REQUEST_URL = "https://build-api.cloud.unity3d.com/api/v1/orgs/";
      public static string ORG_ID = "20066090156130";
      public static string PROJECT_ID = "0ea8276a-68a2-46b7-b2d9-8c33417a2770";
      public static string BUILD_TARGET = "windows-desktop-64-bit-client";

      // If data should be added to the logs
      public bool isLoggingData = false;

      #endregion

      private void Awake () {
         self = this;
      }

      private void Start () {
         // Only start the process if this is a cloud build server
         #if CLOUD_BUILD && IS_SERVER_BUILD
         InvokeRepeating("triggerCloudChecker", 3, CHECK_REPEAT_DELAY);
         #endif
      }

      private void triggerCloudChecker () {
         D.log("CloudDataLogger: Triggered Cloud checker: " + DateTime.UtcNow);
         StartCoroutine(CO_GetBuildList());
      }

      private void processDatabaseBuild () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            cloudData = DB_Main.getCloudData();
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (cloudData == null) {
                  D.log("CloudDataLogger: Fetched cloud data is null, try again later");
               } else {
                  logData("CloudDataLogger: Done Fetching Database Cloud Data");
                  crossCheckBuildData();
               }
            });
         });
      }

      private void crossCheckBuildData () {
         if (cloudData.buildId < rootList[0].build) {
            logData("CloudDataLogger: Cloud database is outdated, now updating: " + cloudData.buildId + " to " + rootList[0].build);

            CloudBuildData newCloudBuildData = new CloudBuildData();
            newCloudBuildData.buildId = rootList[0].build;

            string compiledCommitMessage = "";
            foreach (Changeset commitLogs in rootList[0].changeset) {
               compiledCommitMessage += commitLogs.message + "\n";
            }
            newCloudBuildData.buildMessage = compiledCommitMessage;
            try {
               // Make sure the data is valid
               DateTime newDateTime = DateTime.Parse(rootList[0].finished);
               newCloudBuildData.buildDateTime = newDateTime.ToString();
            } catch {
               // Set current time if invalid data
               newCloudBuildData.buildDateTime = DateTime.UtcNow.ToString(); ;
            }

            // Make sure the date is never null or blank
            if (newCloudBuildData.buildDateTime == null || newCloudBuildData.buildDateTime.Length < 5) {
               // Set current time if invalid data
               newCloudBuildData.buildDateTime = DateTime.UtcNow.ToString();
            }

            if (newCloudBuildData.buildId > 0 && newCloudBuildData.buildMessage.Length > 0) {
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  DB_Main.addNewCloudData(newCloudBuildData);
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     logData("CloudDataLogger: Done");
                  });
               });
            } else {
               D.log("CloudDataLogger: Prevent creating data, please check database for trash entry, build might be in progress");
               D.log("Build id = " + newCloudBuildData.buildId + " : Msg Length = " + newCloudBuildData.buildMessage.Length);
            }
         } else {
            logData("CloudDataLogger: Cloud is up to date: " + cloudData.buildId);
         }
      }

      private void logData (string message) {
         if (isLoggingData) {
            D.log(message);
         }
      }

      private IEnumerator CO_GetAuditLog () {
         string webPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/auditlog";
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);

         WWW www = new WWW(webPage, null, headers);
         yield return www;
         Debug.LogError(www.text);
      }

      private IEnumerator CO_GetBuildLog (int buildId) {
         string webPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/builds/" + buildId + "/";
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);

         WWW www = new WWW(webPage, null, headers);
         yield return www;
         Debug.LogError(www.text);
      }

      private IEnumerator CO_GetBuildList () {
         yield return new WaitForSeconds(3);

         string requestPage = DEFAULT_REQUEST_URL + ORG_ID + "/projects/" + PROJECT_ID + "/buildtargets/" + BUILD_TARGET + "/builds?per_page=" + BUILD_FETCH_COUNT;
         Dictionary<string, string> headers = new Dictionary<string, string>();
         headers.Add("Authorization", "basic " + USER_AUTH_KEY);
         WWW www = new WWW(requestPage, null, headers);
         yield return www;

         // Data translation and extraction
         JArray newArray = (JArray) JsonConvert.DeserializeObject(www.text);
         for (int i = 0; i < newArray.Count; i++) {
            Root contentRoot = JsonUtility.FromJson<Root>(newArray[i].ToString());
            rootList.Add(contentRoot);
         }

         logData("CloudDataLogger: Done Fetching Unity Cloud Data");
         processDatabaseBuild();
      }

      #region Private Variables

      #endregion
   }
}