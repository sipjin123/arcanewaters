using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class PerkToolManager : XmlDataToolManager
{
   #region Public Variables

   // Holds the main scene for the data templates
   public PerkToolScene perkToolScene;

   #endregion

   public void saveXMLData (PerkData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePerksXML(longString, data.perkId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _perkDataList = new Dictionary<int, PerkData>();

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<PerkData> xmlData = DB_Main.getPerksXML(); 
         userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.Perks);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (PerkData data in xmlData) {
               // Save the perk data in the memory cache
               if (!_perkDataList.ContainsKey(data.perkId)) {
                  _perkDataList.Add(data.perkId, data);
               }
            }

            perkToolScene.loadPerkData(_perkDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteXMLData (PerkData data) {
      if (data.perkId > 0) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.deletePerkXML(data.perkId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadXMLData();
            });
         });
      }
   }

   public void duplicatePerkData (PerkData perk) {
      PerkData duplicate = new PerkData();
      duplicate.perkId = 0;
      duplicate.perkTypeId = perk.perkTypeId;
      duplicate.name = "[Copy] " + perk.name;
      duplicate.description = perk.description;
      duplicate.iconPath = perk.iconPath;
      duplicate.boostFactor = perk.boostFactor;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         saveXMLData(duplicate);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #region Private Variables

   private Dictionary<int, PerkData> _perkDataList = new Dictionary<int, PerkData>();

   #endregion
}
