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

public class PlayerSpecialtyToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the player specialty
   public PlayerSpecialtyScene specialtySceneScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerSpecialty";

   // Self
   public static PlayerSpecialtyToolManager self;

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

   public void saveXMLData (PlayerSpecialtyData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Specialty);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (PlayerSpecialtyData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePlayerClassXML(PlayerStatType.Specialty, (int) data.type);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (PlayerSpecialtyData data) {
      data.type = 0;
      data.specialtyName = "Undefined Specialty";
      data.specialtyIconPath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Specialty);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _playerSpecialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(PlayerStatType.Specialty);
         _userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.PlayerSpecialty);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerSpecialtyData specialtyData = Util.xmlLoad<PlayerSpecialtyData>(newTextAsset);

               // Save the data in the memory cache
               if (!_playerSpecialtyData.ContainsKey(specialtyData.type)) {
                  _playerSpecialtyData.Add(specialtyData.type, specialtyData);
               }
            }

            specialtySceneScene.loadPlayerSpecialtyData(_playerSpecialtyData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of player specialty data
   private Dictionary<Specialty.Type, PlayerSpecialtyData> _playerSpecialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

   #endregion
}
