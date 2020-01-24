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

public class PlayerClassTool : MonoBehaviour
{
   #region Public Variables

   // Holds the main scene for the player class
   public PlayerClassScene classScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerClass";

   // Self
   public static PlayerClassTool self;

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

   public void saveXMLData (PlayerClassData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Class);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (PlayerClassData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePlayerClassXML(PlayerStatType.Class, (int) data.type);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (PlayerClassData data) {
      data.type = 0;
      data.className = "Undefined Class";
      data.itemIconPath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int)data.type, PlayerStatType.Class);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _playerClassData = new Dictionary<string, PlayerClassData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(PlayerStatType.Class);
         _userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.PlayerClass);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerClassData classData = Util.xmlLoad<PlayerClassData>(newTextAsset);

               // Save the data in the memory cache
               if (!_playerClassData.ContainsKey(classData.className)) {
                  _playerClassData.Add(classData.className, classData);
               }
            }

            classScene.loadPlayerClass(_playerClassData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of player class data
   private Dictionary<string, PlayerClassData> _playerClassData = new Dictionary<string, PlayerClassData>();

   #endregion
}