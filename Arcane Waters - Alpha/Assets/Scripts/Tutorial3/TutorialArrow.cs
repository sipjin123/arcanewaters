using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialArrow : ArrowIndicator
{
   #region Public Variables

   #endregion

   protected override void Start () {
      base.Start();
      deactivate();

      // Cache a reference to the main camera if it doesn't exist
      if (_mainCamera == null) {
         _mainCamera = Camera.main;
      }
   }
   protected override void Update () {
      hideArrows();

      if (!TutorialManager3.self.isActive()) {
         deactivate();
         return;
      }

      base.Update();
   }

   public void setTarget (string targetAreaKey) {
      if (!string.IsNullOrEmpty(targetAreaKey) ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLobby ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLeagueNotLobby) {
         gameObject.SetActive(true);
         _target = null;
         StopAllCoroutines();
         StartCoroutine(CO_PointTo(targetAreaKey));
      } else {
         deactivate();
      }
   }

   private IEnumerator CO_PointTo (string targetAreaKey) {
      // Wait until we have finished instantiating the area
      while (Global.player == null || AreaManager.self.getArea(Global.player.areaKey) == null) {
         yield return 0;
      }

      // Search for the correct warp (entrance to target)
      foreach (Warp warp in AreaManager.self.getArea(Global.player.areaKey).getWarps()) {
         if (string.Equals(warp.targetInfo.name, targetAreaKey)) {
            _target = warp.gameObject;
            break;
         }
      }

      // Special case for league maps
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLobby ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLeagueNotLobby) {
         foreach (GenericActionTrigger genericTrigger in AreaManager.self.getArea(Global.player.areaKey).GetComponentsInChildren<GenericActionTrigger>()) {
            if (genericTrigger.actionName == GenericActionTrigger.WARP_TO_LEAGUE_ACTION) {
               _target = genericTrigger.gameObject;
               break;
            }
         }
      }

      if (_target == null) {
         deactivate();
         yield break;
      }
   }

   #region Private Variables

   #endregion
}
