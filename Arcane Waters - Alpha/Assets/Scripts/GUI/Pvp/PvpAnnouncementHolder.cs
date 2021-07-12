using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpAnnouncementHolder : MonoBehaviour
{
   #region Public Variables

   // The prefab to instantiate PvpAnnouncement instances from
   public GameObject pvpAnnouncementPrefab;

   // Reference to singleton instance
   public static PvpAnnouncementHolder self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void addKillAnnouncement (NetEntity attacker, NetEntity target, bool blink = false, PvpTeamType pvpTeam = PvpTeamType.None) {
      clearAnnouncements();

      if (pvpAnnouncementPrefab == null) {
         return;
      }

      GameObject newAnnouncementInstance = Instantiate(pvpAnnouncementPrefab);
      PvpAnnouncement newAnnouncement = newAnnouncementInstance.GetComponent<PvpAnnouncement>();

      if (newAnnouncement == null) {
         return;
      }

      newAnnouncement.transform.SetParent(this.transform);
      newAnnouncement.blinkingColor = PvpGame.getColorForTeam(pvpTeam);
      newAnnouncement.isBlinking = blink;
      newAnnouncement.announceKill(attacker,target);
   }

   public void addAnnouncement (string announcementText, bool blink = false) {
      clearAnnouncements();

      if (pvpAnnouncementPrefab == null) {
         return;
      }

      GameObject newAnnouncementInstance = Instantiate(pvpAnnouncementPrefab);
      PvpAnnouncement newAnnouncement = newAnnouncementInstance.GetComponent<PvpAnnouncement>();

      if (newAnnouncement == null) {
         return;
      }

      newAnnouncement.transform.SetParent(this.transform);
      newAnnouncement.announce(announcementText);
   }

   public void clearAnnouncements () {
      int childrenCount = this.transform.childCount;

      for (int i = childrenCount-1; i >= 0; i--) {
         Transform childTransform = this.transform.GetChild(i);
         Destroy(childTransform.gameObject);
      }
   }

   #region Private Variables

   #endregion
}
