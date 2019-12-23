using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class JobManager : XmlManager {
   #region Public Variables

   // Self
   public static JobManager self;

   // For editor preview of data
   public List<PlayerJobData> jobDataList = new List<PlayerJobData>();

   #endregion

   public void Awake () {
      self = this;
   }

   public PlayerJobData getJobData (Jobs.Type jobtype) {
      PlayerJobData returnData = _jobData[jobtype];
      if (returnData == null) {
         Debug.LogWarning("The Job Does not Exist yet!: " + jobtype);
      }
      return returnData;
   }

   public void initializeDataCache () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(ClassManager.PlayerStatType.Job);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerJobData jobData = Util.xmlLoad<PlayerJobData>(newTextAsset);
               Jobs.Type uniqueID = jobData.type;

               // Save the data in the memory cache
               if (!_jobData.ContainsKey(uniqueID)) {
                  _jobData.Add(uniqueID, jobData);
                  jobDataList.Add(jobData);
               }
            }
         });
      });
   }

   public void receiveDataFromServer (List<PlayerJobData> receivedJobDataList) {
      foreach (PlayerJobData jobData in receivedJobDataList) {
         Jobs.Type uniqueID = jobData.type;

         // Save the data in the memory cache
         if (!_jobData.ContainsKey(uniqueID)) {
            _jobData.Add(uniqueID, jobData);
            jobDataList.Add(jobData);
         }
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Jobs.Type, PlayerJobData> _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

   #endregion
}