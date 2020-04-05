using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

namespace MapCreationTool
{
   public class MapEditorDiscoveriesManager : MonoBehaviour
   {
      #region Public Variables

      // The singleton instance
      public static MapEditorDiscoveriesManager instance;

      // A dictionary that maps the ID of the discovery to the actual discovery data
      public Dictionary<int, DiscoveryData> idToDiscovery = new Dictionary<int, DiscoveryData>();

      // The number of discoveries that exist in the DB
      public int discoveriesCount { get { return _discoveries.Count; } }

      #endregion

      private void Awake () {
         instance = this;
      }

      private void Start () {
         fetchDiscoveries();
      }

      public SelectOption[] formSelectionOptions () {
         return _discoveries.Select(n => new SelectOption(n.discoveryId.ToString(), n.name)).ToArray();
      }

      private void fetchDiscoveries () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            _discoveries = DB_Main.getDiscoveriesList();
            idToDiscovery = _discoveries.ToDictionary(d => d.discoveryId, d => d);
         });
      }

      #region Private Variables

      // The discoveries collection in the database
      private List<DiscoveryData> _discoveries;

      #endregion   
   }
}