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
      _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerJobData jobData = Util.xmlLoad<PlayerJobData>(textAsset);
         Jobs.Type uniqueID = jobData.type;

         // Save the data in the memory cache
         if (!_jobData.ContainsKey(uniqueID)) {
            _jobData.Add(uniqueID, jobData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(PlayerJobToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Jobs.Type, PlayerJobData> _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

   #endregion
}