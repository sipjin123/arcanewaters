﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class ClassManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ClassManager self;

   // For editor preview of data
   public List<PlayerClassData> classDataList = new List<PlayerClassData>();

   public enum PlayerStatType
   {
      None = 0,
      Class = 1,
      Faction = 2, 
      Specialty = 3,
      Job = 4
   }

   #endregion

   public void Awake () {
      self = this;
   }

   public PlayerClassData getClassData (Class.Type classType) {
      PlayerClassData returnData = _classData[classType];
      if (returnData == null) {
         Debug.LogWarning("The Class Does not Exist yet!: " + classType);
      }
      return returnData;
   }

   public void initializeDataCache () {
      _classData = new Dictionary<Class.Type, PlayerClassData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(PlayerStatType.Class);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerClassData classData = Util.xmlLoad<PlayerClassData>(newTextAsset);
               Class.Type uniqueID = classData.type;

               // Save the data in the memory cache
               if (!_classData.ContainsKey(uniqueID)) {
                  _classData.Add(uniqueID, classData);
                  classDataList.Add(classData);
               }
            }
         });
      });
   }

   public void addClassInfo (PlayerClassData classData) {
      Class.Type uniqueID = classData.type;

      // Save the data in the memory cache
      if (!_classData.ContainsKey(uniqueID)) {
         _classData.Add(uniqueID, classData);
         classDataList.Add(classData);
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Class.Type, PlayerClassData> _classData = new Dictionary<Class.Type, PlayerClassData>();

   #endregion
}
 