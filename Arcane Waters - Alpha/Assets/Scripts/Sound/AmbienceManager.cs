using System.Collections.Generic;
using UnityEngine;

public class AmbienceManager : ClientMonoBehaviour
{
   #region Public Variables

   // Self
   public static AmbienceManager self;

   #endregion

   protected override void Awake () {
      D.adminLog("AmbienceManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      base.Awake();

      self = this;
      D.adminLog("AmbienceManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   protected void Start () {
      // No need for this in batch mode
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }
   }

   protected void Update () {
      // Check if our area has changed
      if (Global.player != null && Global.player.areaKey != _lastArea) {
         updateAmbienceForArea(Global.player.areaKey);

         // Make note of our current area
         _lastArea = Global.player.areaKey;
      }
   }

   protected void updateAmbienceForArea (string newAreaKey) {
      //SoundEffectManager.self.playAmbienceMusic(newAreaKey);
   }

   #region Private Variables

   // The last area we were in
   protected string _lastArea = "";

   #endregion
}
