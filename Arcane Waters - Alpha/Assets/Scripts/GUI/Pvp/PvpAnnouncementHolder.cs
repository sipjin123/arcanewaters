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

   private PvpAnnouncement createAnnouncement(Transform parent, bool blink) {
      clearAnnouncements();

      if (pvpAnnouncementPrefab == null) {
         return null;
      }

      GameObject newAnnouncementInstance = Instantiate(pvpAnnouncementPrefab);
      PvpAnnouncement newAnnouncement = newAnnouncementInstance.GetComponent<PvpAnnouncement>();

      if (newAnnouncement == null) {
         return null;
      }

      newAnnouncement.transform.SetParent(parent.transform);
      newAnnouncement.isBlinking = blink;

      return newAnnouncement;
   }

   public void addTowerDestructionAnnouncement(NetEntity attacker, PvpTower target, bool blink = false, PvpTeamType pvpTeam = PvpTeamType.None) {
      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.blinkingColor = PvpGame.getColorForTeam(pvpTeam);
         newAnnouncement.announceTowerDestruction(attacker, target);
      }
   }

   public void addKillAnnouncement (NetEntity attacker, NetEntity target, bool blink = false, PvpTeamType pvpTeam = PvpTeamType.None) {
      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.blinkingColor = PvpGame.getColorForTeam(pvpTeam);
         newAnnouncement.announceKill(attacker, target);
      }
   }

   public void addAnnouncement (string announcementText, bool blink = false) {
      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.announce(announcementText);
      }
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
