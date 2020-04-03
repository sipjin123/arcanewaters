using System.Collections.Generic;
using UnityEngine;

public class DiscoveriesToolManager : XmlDataToolManager
{
   #region Public Variables

   // Holds the main scene for the data templates
   public DiscoveriesToolScene discoveriesToolScene;

   #endregion

   public void saveDiscoveryData (DiscoveryData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.upsertDiscovery(data);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadDiscoveriesList();
         });
      });
   }

   public void deleteDiscoveryData (DiscoveryData data) {
      if (data.discoveryId > 0) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.deleteDiscoveryById(data.discoveryId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadDiscoveriesList();
            });
         });
      }
   }

   public void loadDiscoveriesList () {
      _discoveriesDataList = new Dictionary<int, DiscoveryData>();

      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<DiscoveryData> discoveries = DB_Main.getDiscoveriesList();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (DiscoveryData discovery in discoveries) {
               // Save the discovery data in the memory cache
               if (_discoveriesDataList.ContainsKey(discovery.discoveryId)) {
                  Debug.LogWarning("Duplicated ID: " + discovery.discoveryId);
               } else {
                  _discoveriesDataList.Add(discovery.discoveryId, discovery);
               }
            }
            discoveriesToolScene.loadDiscoveryData(_discoveriesDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void duplicateDiscovery (DiscoveryData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.duplicateDiscovery(data);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadDiscoveriesList();
         });
      });
   }

   #region Private Variables

   // Holds the list of discovery data
   private Dictionary<int, DiscoveryData> _discoveriesDataList = new Dictionary<int, DiscoveryData>();

   #endregion
}
