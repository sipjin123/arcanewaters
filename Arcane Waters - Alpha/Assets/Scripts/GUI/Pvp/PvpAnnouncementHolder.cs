using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

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
      newAnnouncement.holder = this;

      _announcementList.Add(newAnnouncement);

      return newAnnouncement;
   }

   public void addTowerDestructionAnnouncement(NetEntity attacker, PvpTower target, bool blink = false, PvpTeamType pvpTeam = PvpTeamType.None) {
      PvpAnnouncement.Priority priority = PvpAnnouncement.Priority.ObjectiveUpdate;
      
      // If this announcement's priority is lower than the current one, don't show it
      if (priority < getCurrentAnnouncementPriority()) {
         return;
      }

      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.blinkingColor = PvpGame.getColorForTeam(pvpTeam);
         newAnnouncement.announceTowerDestruction(attacker, target);
         newAnnouncement.announcementPriority = priority;
      }
   }

   public void addKillAnnouncement (NetEntity attacker, NetEntity target, bool blink = false, PvpTeamType pvpTeam = PvpTeamType.None) {
      PvpAnnouncement.Priority priority = PvpAnnouncement.Priority.PlayerKill;

      // If this announcement's priority is lower than the current one, don't show it
      if (priority < getCurrentAnnouncementPriority()) {
         return;
      }

      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.blinkingColor = PvpGame.getColorForTeam(pvpTeam);
         newAnnouncement.announceKill(attacker, target);
         newAnnouncement.announcementPriority = priority;
      }
   }

   public void addAnnouncement (string announcementText, PvpAnnouncement.Priority priority, bool blink = false) {
      // If this announcement's priority is lower than the current one, don't show it
      if (priority < getCurrentAnnouncementPriority()) {
         return;
      }

      PvpAnnouncement newAnnouncement = createAnnouncement(this.transform, blink);

      if (newAnnouncement != null) {
         newAnnouncement.announce(announcementText);
         newAnnouncement.announcementPriority = priority;
      }
   }

   public void clearAnnouncements () {
      foreach (PvpAnnouncement announcement in _announcementList) {
         Destroy(announcement.gameObject);
      }

      _announcementList.Clear();
   }

   public void removeAnnouncement (PvpAnnouncement announcement) {
      if (_announcementList != null && _announcementList.Contains(announcement)) {
         _announcementList.Remove(announcement);
      }
   }

   private PvpAnnouncement getCurrentAnnouncement () {
      if (_announcementList == null) {
         return null;
      }

      return _announcementList.FirstOrDefault();
   }

   private PvpAnnouncement.Priority getCurrentAnnouncementPriority () {
      PvpAnnouncement currentAnnouncement = getCurrentAnnouncement();

      if (currentAnnouncement == null) {
         return PvpAnnouncement.Priority.None;
      }

      return currentAnnouncement.announcementPriority;
   }

   #region Private Variables

   // The set of announcements
   List<PvpAnnouncement> _announcementList = new List<PvpAnnouncement>();

   #endregion
}
