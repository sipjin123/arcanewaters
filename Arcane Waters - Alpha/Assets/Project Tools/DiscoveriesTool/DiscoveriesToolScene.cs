using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DiscoveriesToolScene : MonoBehaviour
{
   #region Public Variables

   // The template for list elements
   public DiscoveriesToolTemplate discoveryTemplate;

   // The discoveries tool manager
   public DiscoveriesToolManager toolManager;

   // Holds the discovery data panel
   public DiscoveriesToolPanel discoveriesDataPanel;

   // The parent holding the list element template
   public GameObject itemTemplateParent;

   // The create discovery button
   public Button createDiscoveryButton;

   // The button to return to main menu
   public Button mainMenuButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createDiscoveryButton.gameObject.SetActive(false);
      }

      createDiscoveryButton.onClick.AddListener(createEmptyDiscovery);
      mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(MasterToolScene.masterScene));
   }

   private void Start () {
      toolManager.loadDiscoveriesList();
   }

   private void createEmptyDiscovery () {
      DiscoveryData data = new DiscoveryData();
      data.name = "New Discovery";
      data.description = "";
      data.rarity = Rarity.Type.Common;

      DiscoveriesToolTemplate template = GenericEntryTemplate.createGenericTemplate(discoveryTemplate.gameObject, toolManager, itemTemplateParent.transform) as DiscoveriesToolTemplate;
      template.editButton.onClick.AddListener(() => {
         discoveriesDataPanel.loadData(data);
         discoveriesDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDiscoveryData(data);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateDiscovery(data);
      });

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadDiscoveryData (Dictionary<int, DiscoveryData> discoveryDataCollection) {
      itemTemplateParent.gameObject.DestroyChildren();

      List<DiscoveryData> sortedList = discoveryDataCollection.Values.ToList().OrderBy(w => w.discoveryId).ToList();

      // Create a row for each discovery element
      foreach (DiscoveryData discovery in sortedList) {
         // Ignore "undefined" discovery when creating the list
         if (discovery.discoveryId == 0) {
            continue;
         }

         DiscoveriesToolTemplate template = GenericEntryTemplate.createGenericTemplate(discoveryTemplate.gameObject, toolManager, itemTemplateParent.transform) as DiscoveriesToolTemplate;

         template.itemIcon.sprite = ImageManager.getSprite(discovery.spriteUrl);
         template.nameText.text = discovery.name;

         template.editButton.onClick.AddListener(() => {
            discoveriesDataPanel.loadData(discovery);
            discoveriesDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDiscoveryData(discovery);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateDiscovery(discovery);
         });

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
