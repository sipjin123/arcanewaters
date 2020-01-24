using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using static ClassManager;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class PlayerFactionToolManager : MonoBehaviour
{
   #region Public Variables

   // Holds the main scene for the player faction
   public PlayerFactionScene factionScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerFaction";

   // Self
   public static PlayerFactionToolManager self;

   // Holds the collection of user id that created the data entry
   public List<SQLEntryIDClass> _userIdData = new List<SQLEntryIDClass>();

   #endregion

   private void Awake () {
      self = this;
   }

   public bool didUserCreateData (int entryID) {
      SQLEntryIDClass sqlEntry = _userIdData.Find(_ => _.dataID == entryID);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryID);
      }

      return false;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (PlayerFactionData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Faction);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (PlayerFactionData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePlayerClassXML(PlayerStatType.Faction, (int) data.type);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (PlayerFactionData data) {
      data.type = 0;
      data.factionName = "Undefined Faction";
      data.factionIconPath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Faction);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _playerFactionData = new Dictionary<Faction.Type, PlayerFactionData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(PlayerStatType.Faction);
         _userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.PlayerFaction);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerFactionData playerFactionData = Util.xmlLoad<PlayerFactionData>(newTextAsset);

               // Save the data in the memory cache
               if (!_playerFactionData.ContainsKey(playerFactionData.type)) {
                  _playerFactionData.Add(playerFactionData.type, playerFactionData);
               }
            }

            factionScene.loadPlayerFaction(_playerFactionData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of player faction data
   private Dictionary<Faction.Type, PlayerFactionData> _playerFactionData = new Dictionary<Faction.Type, PlayerFactionData>();

   #endregion
}
