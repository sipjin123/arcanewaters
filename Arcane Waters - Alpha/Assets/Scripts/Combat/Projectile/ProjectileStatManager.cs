using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class ProjectileStatManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ProjectileStatManager self;

   // Determines if the list is generated already
   public bool hasInitialized;

   // TODO: For editor/inspector preview only, remove after confirmation that this manager is working perfectly
   public List<ProjectileStatData> projectileDataList = new List<ProjectileStatData>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeDataCache () {
      if (!hasInitialized) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getProjectileXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  try {
                     TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                     ProjectileStatData projectileData = Util.xmlLoad<ProjectileStatData>(newTextAsset);
                     projectileData.projectileId = xmlPair.xmlId;
                     int uniqueID = projectileData.projectileId;
                     projectileData.projectileName = xmlPair.xmlName;

                     // Save the projectile data in the memory cache
                     if (!_projectileData.ContainsKey(uniqueID)) {
                        _projectileData.Add(uniqueID, projectileData);
                        projectileDataList.Add(projectileData);
                     }
                  } catch {
                     D.debug("Failed to load projectile xml data for: " + xmlPair.xmlId);
                  }
               }
               hasInitialized = true;
            });
         });
      }
   }

   public ProjectileStatData getProjectileData (int projectileId) {
      if (_projectileData.ContainsKey(projectileId)) {
         return _projectileData[projectileId];
      }
      return _projectileData.Values.ToList()[0];
   }

   public void receiveZipData (List<ProjectileStatPair> statDataGroup) {
      foreach (ProjectileStatPair statData in statDataGroup) {
         if (!_projectileData.ContainsKey(statData.xmlId)) {
            _projectileData.Add(statData.xmlId, statData.projectileData);
            projectileDataList.Add(statData.projectileData);
         }
      }
      hasInitialized = true;
   }

   #region Private Variables

   // The cached projectile data 
   private Dictionary<int, ProjectileStatData> _projectileData = new Dictionary<int, ProjectileStatData>();

   #endregion
}
