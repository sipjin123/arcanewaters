using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class PerkToolScene : MonoBehaviour 
{
   #region Public Variables

   // The tool manager
   public PerkToolManager toolManager;

   // Holds the perk data panel
   public PerkToolDataPanel perkDataPanel;

   // The template for list elements
   public PerkToolListTemplate perkListElementTemplate;

   // The parent holding the perk templates
   public GameObject itemTemplateParent;

   // The create perk button
   public Button createButton;

   // The button to return to main menu
   public Button mainMenuButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }

      createButton.onClick.AddListener(createPerkTemplate);
      mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(MasterToolScene.masterScene));
   }

   private void Start () {
      toolManager.loadXMLData();
   }

   private void createPerkTemplate () {
      PerkData data = new PerkData();
      data.name = "Unnamed Perk";
      data.description = "";
      data.perkId = 0;
      data.perkTypeId = 1;
      data.boostFactor = 0;
      data.iconPath = "";

      PerkToolListTemplate template = GenericEntryTemplate.createGenericTemplate(perkListElementTemplate.gameObject, toolManager, itemTemplateParent.transform) as PerkToolListTemplate;
      template.editButton.onClick.AddListener(() => {
         perkDataPanel.loadData(data);
         perkDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteXMLData(data);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicatePerkData(data);
      });

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadPerkData (Dictionary<int, PerkData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      List<PerkData> sortedList = data.Values.ToList().OrderBy(w => w.perkTypeId).ToList();

      // Create a row for each perk element
      foreach (PerkData perkData in sortedList) {
         PerkToolListTemplate template = GenericEntryTemplate.createGenericTemplate(perkListElementTemplate.gameObject, toolManager, itemTemplateParent.transform) as PerkToolListTemplate;

         template.nameText.text = perkData.name;

         template.editButton.onClick.AddListener(() => {
            perkDataPanel.loadData(perkData);
            perkDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteXMLData(perkData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicatePerkData(perkData);
         });

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }

         template.itemIcon.sprite = ImageManager.getSprite(perkData.iconPath);
         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables
      
   #endregion
}
