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

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
      translateXMLData();
   }

   public PlayerFactionData getFactionData (Faction.Type factionType) {
      PlayerFactionData returnData = _factionData[factionType];
      if (returnData == null) {
         Debug.LogWarning("The Faction Does not Exist yet!: " + factionType);
      }
      return returnData;
   }

   private void translateXMLData () {
      _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerFactionData factionData = Util.xmlLoad<PlayerFactionData>(textAsset);
         Faction.Type uniqueID = factionData.type;

         // Save the data in the memory cache
         if (!_factionData.ContainsKey(uniqueID)) {
            _factionData.Add(uniqueID, factionData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(PlayerFactionToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Faction.Type, PlayerFactionData> _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

   #endregion
}