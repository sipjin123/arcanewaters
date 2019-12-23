using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class FactionManager : XmlManager {
   #region Public Variables

   // Self
   public static FactionManager self;

   // For editor preview of data
   public List<PlayerFactionData> factionDataList = new List<PlayerFactionData>();

   #endregion

   public void Awake () {
      self = this;
   }

   public PlayerFactionData getFactionData (Faction.Type factionType) {
      PlayerFactionData returnData = _factionData[factionType];
      if (returnData == null) {
         Debug.LogWarning("The Faction Does not Exist yet!: " + factionType);
      }
      return returnData;
   }

   public void initializeDataCache () {
      _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(ClassManager.PlayerStatType.Faction);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerFactionData factionData = Util.xmlLoad<PlayerFactionData>(newTextAsset);
               Faction.Type uniqueID = factionData.type;

               // Save the data in the memory cache
               if (!_factionData.ContainsKey(uniqueID)) {
                  _factionData.Add(uniqueID, factionData);
                  factionDataList.Add(factionData);
               }
            }
         });
      });
   }

   public void addFactionInfo (PlayerFactionData factionData) {
      Faction.Type uniqueID = factionData.type;

      // Save the data in the memory cache
      if (!_factionData.ContainsKey(uniqueID)) {
         _factionData.Add(uniqueID, factionData);
         factionDataList.Add(factionData);
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Faction.Type, PlayerFactionData> _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

   #endregion
}