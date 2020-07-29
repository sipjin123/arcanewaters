using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionUserPanel : MonoBehaviour {
   #region Public Variables

   // The loading blockers
   public GameObject[] loadBlockers;

   // The item holder
   public Transform itemParent;

   // The item prefab
   public UserItemTemplate itemPrefab;

   // Basic item info display
   public Image itemImage;
   public Text itemName;
   public Text itemDescription;
   public Text itemRarity;
   public Text itemQuantity;
   public Text itemCategory;

   // The list of created templates
   public List<UserItemTemplate> userItemTemplateList = new List<UserItemTemplate>();

   // The selected item template
   public UserItemTemplate selectedTemplate;

   // The item price
   public InputField itemPrice;

   // Popup Buttons for posting auctioned item
   public Button postAuction, confirmButton, cancelButton, exitButton;

   // The confirmation modal
   public GameObject confirmationModal;

   // Filter buttons
   public Button allFilterButton, armorFilterButton, weaponFilterButton, hatFilterButton, ingredientFilterButton;

   // The current user inventory page
   public int currentPage = 0;

   #endregion

   private void Awake () {
      cancelButton.onClick.AddListener(() => {
         confirmationModal.SetActive(false);
      });

      postAuction.onClick.AddListener(() => {
         confirmationModal.SetActive(true);
      });

      confirmButton.onClick.AddListener(() => {
         confirmationModal.SetActive(false);
         gameObject.SetActive(false);

         // Do server logic here
      });

      exitButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      allFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(currentPage, Item.Category.None);
      });
      armorFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(currentPage, Item.Category.Armor);
      });
      weaponFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(currentPage, Item.Category.Weapon);
      });
      ingredientFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(currentPage, Item.Category.CraftingIngredients);
      });
      hatFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(currentPage, Item.Category.Hats);
      });
   }

   public void loadUserItemList (List<Item> userItemList) {
      setBlockers(false);
      itemParent.gameObject.DestroyChildren();
      userItemTemplateList = new List<UserItemTemplate>();
      foreach (Item item in userItemList) {
         UserItemTemplate itemInventoryTemplate = Instantiate(itemPrefab, itemParent).GetComponent<UserItemTemplate>();
         itemInventoryTemplate.setCellForItem(item);
         itemInventoryTemplate.setPanel(this);

         userItemTemplateList.Add(itemInventoryTemplate);
      }
   }

   public void selectItem (UserItemTemplate itemTemplate) {
      selectedTemplate = itemTemplate;

      foreach (UserItemTemplate template in userItemTemplateList) {
         template.setObjHighlight(false);
      }
      selectedTemplate.setObjHighlight(true);

      itemImage.sprite = ImageManager.getSprite(EquipmentXMLManager.self.getItemIconPath(selectedTemplate.itemCache));
      itemName.text = EquipmentXMLManager.self.getItemName(selectedTemplate.itemCache);
      itemDescription.text = EquipmentXMLManager.self.getItemDescription(selectedTemplate.itemCache);
      itemRarity.text = "Undefined";
      itemQuantity.text = "Undefined";
      itemCategory.text = selectedTemplate.itemCache.category.ToString();
   }

   public void setBlockers (bool isOn) {
      foreach (GameObject obj in loadBlockers) {
         obj.SetActive(isOn);
      }
   }

   #region Private Variables

   #endregion
}
