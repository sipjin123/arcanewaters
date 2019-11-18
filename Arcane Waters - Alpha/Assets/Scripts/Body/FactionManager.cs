using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FactionManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static FactionManager self;

   // The files containing the faction data
   public TextAsset[] factionDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data [FOR EDITOR DISPLAY DATA REVIEW]
   public List<PlayerFactionData> factionDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerFactionData getFactionData (Faction.Type factionType) {
      PlayerFactionData returnData = _factionData[factionType];
      if (returnData == null) {
         Debug.LogWarning("The Faction Does not Exist yet!: " + factionType);
      }
      return returnData;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         factionDataList = new List<PlayerFactionData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in factionDataAssets) {
            // Read and deserialize the file
            PlayerFactionData factionData = Util.xmlLoad<PlayerFactionData>(textAsset);
            Faction.Type uniqueID = factionData.type;

            // Save the data in the memory cache
            if (!_factionData.ContainsKey(uniqueID)) {
               _factionData.Add(uniqueID, factionData);
               factionDataList.Add(factionData);
            }
         }
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Faction.Type, PlayerFactionData> _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

   #endregion
}