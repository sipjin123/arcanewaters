using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class MapListGroupParent : MonoBehaviour
   {
      #region Public Variables

      // Symbols for expansion button
      public const string DOWN_TRIANGLE = "▼";
      public const string LEFT_TRIANGLE = "▶";

      // Entry of a single map
      public MapListEntry mapEntryPref;

      // Parent of the child maps
      public GameObject entryParent;

      // Button for expanding/collapsing the group
      public Button arrowButton;

      // The label of the name of the group
      public Text nameText;

      // The button by which the group is deleted
      public Button deleteButton;

      #endregion

      public void set (KeyValuePair<Map, IEnumerable<Map>> group) {
         _map = group.Key;

         nameText.text = _map.name + " (group)";

         // Pick out a color for this group's markers
         _lastHue = (_lastHue + 0.15f) % 1f;
         Color markerColor = Color.HSVToRGB(_lastHue, 0.6f, 1f);

         arrowButton.GetComponentInChildren<Text>(true).color = markerColor;
         nameText.color = markerColor;

         MapListEntry pEntry = Instantiate(mapEntryPref, entryParent.transform);
         pEntry.set(_map, true);
         pEntry.setMarkerColor(markerColor);

         foreach (Map child in group.Value) {
            MapListEntry cEntry = Instantiate(mapEntryPref, entryParent.transform);
            cEntry.set(child, true);
            cEntry.setMarkerColor(markerColor);
         }
      }

      public void setExpanded (bool expanded) {
         entryParent.SetActive(expanded);
         arrowButton.GetComponentInChildren<Text>(true).text = expanded ? DOWN_TRIANGLE : LEFT_TRIANGLE;
      }

      public void toggleExpand () {
         MapListPanelState.toggleExpandedMapId(_map.id);
         setExpanded(MapListPanelState.self.expandedMapIds.Contains(_map.id));
      }

      #region Private Variables

      // Last marker hue value that was used
      private static float _lastHue = 0;

      // The parent map that this group is targeting
      private Map _map;

      #endregion
   }
}
