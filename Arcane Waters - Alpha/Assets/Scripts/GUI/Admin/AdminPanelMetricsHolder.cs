using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class AdminPanelMetricsHolder : MonoBehaviour
{
   #region Public Variables

   // Reference to the container that will hold metrics
   public VerticalLayoutGroup container = null;

   // Prefab that holds overview about a server
   public AdminPanelServerOverview metricPrefab = null;

   // Text that displays total players in all servers
   public Text totalPlayersText = null;

   #endregion

   public void clearMetrics () {
      foreach (AdminPanelServerOverview overview in GetComponentsInChildren<AdminPanelServerOverview>()) {
         Destroy(overview.gameObject);
      }
   }

   public void addServerOverview (List<ServerOverview> allOverviews, ServerOverview newOverview) {
      addServerOverview(newOverview);

      totalPlayersText.text = "TOTAL PLAYERS: " + allOverviews.Sum(s => s.instances.Sum(i => i.count));
   }

   private void addServerOverview (ServerOverview overview) {
      AdminPanelServerOverview serverEntry = Instantiate(metricPrefab);
      serverEntry.transform.SetParent(container.transform);
      serverEntry.setServerOverview(overview);
   }

   #region Private Variables

   #endregion
}
