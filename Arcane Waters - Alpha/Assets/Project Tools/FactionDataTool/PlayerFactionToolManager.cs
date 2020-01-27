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

public class PlayerFactionToolManager : XmlDataToolManager
{
   #region Public Variables

   // Holds the main scene for the player faction
   public PlayerFactionScene factionScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerFaction";

   // Self
   public static PlayerFactionToolManager self;

   #endregion

   private void Awake () {
      self = this;
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
         userIdData = DB_Main.getSQLDataByID(editorToolType);

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
