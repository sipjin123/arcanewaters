using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CompanionPanel : Panel {
   #region Public Variables

   // The total companions hired by the player
   public int totalEquippedCompanions;

   // The content holder for companions that can still be equipped
   public Transform availableCompanionsHolder;

   // The current companions hired
   public List<CompanionTemplate> equippedCompanionsSlot;

   // The template for previewing a companion info
   public CompanionTemplate companionTemplate;

   // The zone where grabbed companions can be dropped
   public ItemDropZone optionsDropZone;
   public List<ItemDropZone> equippedDropZone;

   // Self
   public static CompanionPanel self;

   // The template being dragged
   public CompanionTemplate grabbedCompanionTemplate;

   // The template recently interacted
   public CompanionTemplate recentCompanionTemplate;

   // Notifies that the drag has started
   public bool startDrag;

   // Holds the canvas blocker 
   public GameObject canvasBlocker;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public override void Update () {
      base.Update();
      if (grabbedCompanionTemplate != null) {
         grabbedCompanionTemplate.transform.position = Input.mousePosition;

         if (Input.GetMouseButtonDown(0) && startDrag) {
            dropTemplateToZone(Input.mousePosition);
         }
      }
   }

   private void dropTemplateToZone (Vector2 dropPosition) {
      bool droppedInRoster = optionsDropZone.isInZone(dropPosition);
      bool droppedInHiredList = false;
      CompanionTemplate droppedSlot = null;

      foreach (ItemDropZone zone in equippedDropZone) {
         if (zone.isInZone(dropPosition)) {
            droppedInHiredList = true;
            droppedSlot = zone.GetComponent<CompanionTemplate>();
            break;
         }
      }

      // Templates can be dragged from the roster slots to the hired slots or vice-versa
      if (droppedInHiredList && !droppedInRoster && !droppedSlot.isOccupied) {
         droppedSlot.isOccupied = true;
         droppedSlot.setData(grabbedCompanionTemplate);
         stopTemplateGrab(true);

         canvasBlocker.SetActive(true);
         Global.player.rpc.Cmd_UpdateCompanionRoster(droppedSlot.companionId, droppedSlot.equipmentSlot);
      } else if (droppedInRoster && !droppedInHiredList) {
         int currentId = grabbedCompanionTemplate.companionId;
         createTemplate(availableCompanionsHolder, grabbedCompanionTemplate.getInfo());
         stopTemplateGrab(true);

         canvasBlocker.SetActive(true);
         Global.player.rpc.Cmd_UpdateCompanionRoster(currentId, 0);
      } else {
         if (recentCompanionTemplate.equipmentSlot < 1) {
            createTemplate(availableCompanionsHolder, grabbedCompanionTemplate.getInfo());
         } else {
            recentCompanionTemplate.setData(grabbedCompanionTemplate);
         }
         stopTemplateGrab(false);
      }

      startDrag = false;
   }

   private void stopTemplateGrab (bool deleteRecentTemplate) {
      bool destroyOrigin = recentCompanionTemplate.equipmentSlot < 1;
      if (deleteRecentTemplate) {
         recentCompanionTemplate.setData(null);
      }

      if (destroyOrigin) {
         Destroy(recentCompanionTemplate.gameObject);
      }

      Destroy(grabbedCompanionTemplate.gameObject);

      grabbedCompanionTemplate = null;
      recentCompanionTemplate = null;
   }

   public void receiveCompanionData (List<CompanionInfo> companionInfo) {
      foreach (CompanionInfo companion in companionInfo.FindAll(_ => _.equippedSlot < 1)) {
         createTemplate(availableCompanionsHolder, companion);
      }

      foreach (CompanionInfo companion in companionInfo.FindAll(_ => _.equippedSlot > 0)) {
         CompanionTemplate newTemplate = equippedCompanionsSlot.Find(_=>_.equipmentSlot == companion.equippedSlot);
         newTemplate.setRawData(companion);
         newTemplate.isOccupied = true;

         newTemplate.gameObject.SetActive(true);
      }
      canvasBlocker.SetActive(false);
   }

   private void createTemplate (Transform parent, CompanionInfo companion) {
      CompanionTemplate newTemplate = Instantiate(companionTemplate.gameObject, parent).GetComponent<CompanionTemplate>();
      newTemplate.setRawData(companion);
      newTemplate.equipmentSlot = 0;

      newTemplate.gameObject.SetActive(true);
   }

   public void tryGrabTemplate (CompanionTemplate companionTemplate) {
      if (grabbedCompanionTemplate == null) {
         recentCompanionTemplate = companionTemplate;

         CompanionTemplate newTemplate = Instantiate(this.companionTemplate.gameObject, transform).GetComponent<CompanionTemplate>();
         newTemplate.setData(companionTemplate);
         newTemplate.gameObject.SetActive(true);

         grabbedCompanionTemplate = newTemplate;
         StartCoroutine(CO_DelayDrag());
      }
   }

   private IEnumerator CO_DelayDrag () {
      yield return new WaitForSeconds(.25f);
      startDrag = true;
   }

   #region Private Variables

   #endregion
}
