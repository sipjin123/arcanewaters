using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ClassManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ClassManager self;

   // The files containing the class data
   public TextAsset[] classDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data [FOR EDITOR DISPLAY DATA REVIEW]
   public List<PlayerClassData> classDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerClassData getClassData (Class.Type classType) {
      PlayerClassData returnData = _classData[classType];
      if (returnData == null) {
         Debug.LogWarning("The Class Does not Exist yet!: " + classType);
      }
      return returnData;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         classDataList = new List<PlayerClassData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in classDataAssets) {
            // Read and deserialize the file
            PlayerClassData classData = Util.xmlLoad<PlayerClassData>(textAsset);
            Class.Type uniqueID = classData.type;

            // Save the data in the memory cache
            if (!_classData.ContainsKey(uniqueID)) {
               _classData.Add(uniqueID, classData);
               classDataList.Add(classData);
            }
         }
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Class.Type, PlayerClassData> _classData = new Dictionary<Class.Type, PlayerClassData>();

   #endregion
}