using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

namespace BackgroundTool
{
   public class DataManager : MonoBehaviour
   {
      #region Public Variables

      // Button to save the data
      public Button saveButton;

      // Load selected dropdown file
      public Button loadButton;

      // Dropdown refering to saved files
      public Dropdown dropDownFiles;

      // List of background data loaded from the server
      public List<BackgroundContentData> backgroundContentList;

      // Reference to the error indicators
      public GameObject errorPanel;
      public Text errorText;
      public Button exitErrorPanelButton;

      // Error Messages
      public static string SPAWNPOINT_ERROR = "Not enough spawn points in the scene.";

      #endregion

      private void Start () {
         saveButton.onClick.AddListener(() => {
            List<SpriteTemplateData> spriteTempList = ImageManipulator.self.spriteTemplateDataList;
            if (spriteTempList.FindAll(_ => _.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Defenders).Count >= 6 && spriteTempList.FindAll(_ => _.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Attackers).Count >= 6) {
               BackgroundContentData newContentData = new BackgroundContentData();
               newContentData.backgroundName = "Test1";
               newContentData.xmlId = 1;
               newContentData.spriteTemplateList = spriteTempList;

               saveData(newContentData);
            } else {
               showErrorPanel(true, SPAWNPOINT_ERROR);
            }
         });

         loadButton.onClick.AddListener(() => {
            string currentOption = dropDownFiles.options[dropDownFiles.value].text;
            BackgroundContentData contentData = backgroundContentList.Find(_ => _.backgroundName == currentOption);
            if (contentData != null) {
               ImageManipulator.self.generateSprites(contentData.spriteTemplateList);
            }
         });

         exitErrorPanelButton.onClick.AddListener(() => {
            showErrorPanel(false, "");
         });

         Invoke("loadData", MasterToolScene.loadDelay);
      }

      private void showErrorPanel (bool isActive, string message) {
         errorText.text = message;
         errorPanel.SetActive(isActive);
      }

      public void saveData (BackgroundContentData data) {
         XmlSerializer ser = new XmlSerializer(data.GetType());
         var sb = new StringBuilder();
         using (var writer = XmlWriter.Create(sb)) {
            ser.Serialize(writer, data);
         }

         string xmlContent = sb.ToString();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.updateBackgroundXML(data.xmlId, xmlContent, data.backgroundName);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadData();
            });
         });
      }

      public void loadData () {
         XmlLoadingPanel.self.startLoading();
         backgroundContentList = new List<BackgroundContentData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> backgroundData = DB_Main.getBackgroundXML();
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in backgroundData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  BackgroundContentData bgContentData = Util.xmlLoad<BackgroundContentData>(newTextAsset);
                  backgroundContentList.Add(bgContentData);
               }

               setUIContent();
            });
         });
      }

      private void setUIContent () {
         dropDownFiles.ClearOptions();

         List<Dropdown.OptionData> optionList = new List<Dropdown.OptionData>();
         foreach (BackgroundContentData bgContentData in backgroundContentList) {
            Dropdown.OptionData newOptionData = new Dropdown.OptionData {
               image = null,
               text = bgContentData.backgroundName
            };
            optionList.Add(newOptionData);
         }
         dropDownFiles.AddOptions(optionList);
         XmlLoadingPanel.self.finishLoading();
      }

      #region Private Variables

      #endregion
   }
}