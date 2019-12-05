using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class ClassManager : XmlManager {
   #region Public Variables

   // Self
   public static ClassManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
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
      _classData = new Dictionary<Class.Type, PlayerClassData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerClassData classData = Util.xmlLoad<PlayerClassData>(textAsset);
         Class.Type uniqueID = classData.type;

         // Save the data in the memory cache
         if (!_classData.ContainsKey(uniqueID)) {
            _classData.Add(uniqueID, classData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(PlayerClassTool.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Class.Type, PlayerClassData> _classData = new Dictionary<Class.Type, PlayerClassData>();

   #endregion
}