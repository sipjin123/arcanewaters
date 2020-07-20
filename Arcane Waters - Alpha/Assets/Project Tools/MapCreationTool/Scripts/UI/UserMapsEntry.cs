using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class UserMapsEntry : MonoBehaviour
   {
      #region Public Variables

      // Symbols for expansion button
      public const string DOWN_TRIANGLE = "▼";
      public const string LEFT_TRIANGLE = "▶";

      // Label showing the name of the user
      public Text userLabel;

      // Gameobject in which map entries are nested
      public GameObject mapListParent;

      // Entry of a single map
      public MapListEntry mapEntryPref;

      // Entry of a map group parent
      public MapListGroupParent mapGroupParentPref;

      #endregion

      public void set (IGrouping<(int creatorID, string creatorName), KeyValuePair<Map, IEnumerable<Map>>> userGroup) {
         _userId = userGroup.Key.creatorID;
         _userName = userGroup.Key.creatorName;

         userLabel.text = userGroup.Key.creatorName;

         foreach (var group in userGroup) {
            bool expandable = group.Value.Count() > 0;

            // If it is expandable, it's a map group, otherwise it's a single map
            if (expandable) {
               MapListGroupParent groupParent = Instantiate(mapGroupParentPref, mapListParent.transform);
               groupParent.set(group);
               groupParent.setExpanded(MapListPanelState.self.expandedMapIds.Contains(group.Key.id));
            } else {
               MapListEntry entry = Instantiate(mapEntryPref, mapListParent.transform);
               entry.set(group.Key, false);
            }
         }

         setExpanded(MapListPanelState.self.expandedUserIds.Contains(_userId));
      }

      public void setExpanded (bool expanded) {
         userLabel.text = (expanded ? DOWN_TRIANGLE : LEFT_TRIANGLE) + " " + _userName;
         mapListParent.SetActive(expanded);
      }

      public void toggleExpandGroup () {
         MapListPanelState.toggleExpandedUserId(_userId);
         setExpanded(MapListPanelState.self.expandedUserIds.Contains(_userId));
      }

      #region Private Variables

      // User id of this group's user
      private int _userId;

      // Name of this group's user
      private string _userName;

      #endregion
   }
}
