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

      #endregion

      private void Start () {
         saveButton.onClick.AddListener(() => {
            BackgroundContentData newContentData = new BackgroundContentData();
            newContentData.backgroundName = "Test1";
            newContentData.xmlId = 1;
            newContentData.spriteTemplateList = ImageManipulator.self.spriteTemplateList;

            saveData(newContentData);
         });
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
               // Unity Call here
            });
         });
      }

      #region Private Variables

      #endregion
   }
}