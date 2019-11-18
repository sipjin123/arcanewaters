using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class JobManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static JobManager self;

   // The files containing the job data
   public TextAsset[] jobDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data [FOR EDITOR DISPLAY DATA REVIEW]
   public List<PlayerJobData> jobDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerJobData getJobData (Jobs.Type jobtype) {
      PlayerJobData returnData = _jobData[jobtype];
      if (returnData == null) {
         Debug.LogWarning("The Job Does not Exist yet!: " + jobtype);
      }
      return returnData;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         jobDataList = new List<PlayerJobData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in jobDataAssets) {
            // Read and deserialize the file
            PlayerJobData jobData = Util.xmlLoad<PlayerJobData>(textAsset);
            Jobs.Type uniqueID = jobData.type;

            // Save the data in the memory cache
            if (!_jobData.ContainsKey(uniqueID)) {
               _jobData.Add(uniqueID, jobData);
               jobDataList.Add(jobData);
            }
         }
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Jobs.Type, PlayerJobData> _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

   #endregion
}