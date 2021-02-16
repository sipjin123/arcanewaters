using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VoyageGroupIconsManager : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      InvokeRepeating("cleanUpIconsList", 0.0f, 300.0f);
   }

   private void cleanUpIconsList () {
      // Clean up unused icons from the list to speed up searching
      for (int i = _groupIcons.Count - 1; i >= 0; i--) {
         MM_GroupPlayerIcon icon = _groupIcons[i];
         if (icon == null) {
            _groupIcons.RemoveAt(i);
         }
      }
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      if (AreaManager.self.getAreaSpecialType(Global.player.areaKey) != Area.SpecialType.Town) {
         return;
      }

      if (Global.player.voyageGroupId != -1 && Global.player.transform.parent != null) {
         NetEntity[] players = Global.player.transform.parent.GetComponentsInChildren<NetEntity>();
         foreach (NetEntity player in players) {
            if (player.voyageGroupId == Global.player.voyageGroupId && player.areaKey == Global.player.areaKey && player != Global.player) {
               if (!_groupIcons.Find(x => x.player == player)) {
                  MM_GroupPlayerIcon icon = Instantiate(Minimap.self.groupPlayerIconPrefab, Minimap.self.iconContainer.transform);
                  icon.player = player;
                  icon.tooltip.text = player.entityName;
                  _groupIcons.Add(icon);
               }
            }
         }
      }
   }

   #region Private Variables

   // Currently used icons to represent players in voyage group (town only)
   private List<MM_GroupPlayerIcon> _groupIcons = new List<MM_GroupPlayerIcon>();

   #endregion
}
