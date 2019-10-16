using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingPanel : Panel, IPointerClickHandler
{
   #region Public Variables

   // Image icon of weapons
   public Sprite weaponBlueprintIcon;

   // Image icon of armors
   public Sprite armorBlueprintIcon;

   // The name of player
   public Text playerNameText;

   // The name of the item above the description to be crafted
   public Text itemTitleText;

   // The details of the item to b crafted
   public Text itemInfoText;

   // The cost of crafting the item
   public Text goldText;
   public Text gemsText;

   // An empty placeholder
   public Sprite emptyImage;

   // The Templates for the raw materials used for crafting
   public BlueprintRow blueprintRow;

   // The crafting slots
   public List<CraftingRow> craftingRowList;

   // List of Ingredients for Crafting
   public List<Item> ingredientList;

   // The holder of the current items that can be used for crafting
   public Transform listParent;

   // Primary buttons
   public Button craftButton, clearButton;

   // Disable Button Object for crafting
   public GameObject craftDisabled;

   // Caches the item that can be crafted
   public Item craftableItem;

   // Our character stack
   public CharacterStack characterStack;

   #endregion

   public override void Start () {
      base.Start();

      clearButton.onClick.AddListener(() => { purge(); });
      craftButton.onClick.AddListener(() => { craft(); });
   }

   private void OnEnable () {
      for (int i = 0; i < craftingRowList.Count; i++) {
         craftingRowList[i].purgeData();
      }
   }

   public void requestInventoryFromServer (int pageNumber) {
      // Get the latest info from the server to show in our character stack
      Global.player.rpc.Cmd_RequestItemsFromServer(pageNumber, 35);
   }

   private void clickMaterialRow (BlueprintRow currBlueprintRow) {
      Blueprint currItem = currBlueprintRow.itemData;
      Item convertedItem = Blueprint.getItemData(currItem.itemTypeId);

      CraftableItemRequirements itemCombo = RewardManager.self.craftableDataList.Find(_ => _.resultItem.category == Blueprint.getEquipmentType(currItem.itemTypeId) && _.resultItem.itemTypeId == convertedItem.itemTypeId);

      if (itemCombo == null) {
         D.error("Item does not exist");
         currBlueprintRow.itemName.text = "Missing Data";
      } else {
         // Un selects previous selected blueprint
         if (_currBlueprintRow != null) {
            if (_currBlueprintRow != currBlueprintRow) {
               _currBlueprintRow.deselectItem();
            }
         }
         // Enables Selection Frame
         currBlueprintRow.selectItem();
         _currBlueprintRow = currBlueprintRow;

         _craftingIngredientList = itemCombo.combinationRequirements;

         int requirementCount = itemCombo.combinationRequirements.Length;
         int passedRequirementCount = 0;

         // Clears previous requirement list
         for (int i = 0; i < craftingRowList.Count; i++) {
            craftingRowList[i].purgeData();
         }

         // Checks individual materials if complete
         for (int i = 0; i < requirementCount; i++) {
            Item requirement = itemCombo.combinationRequirements[i];
            Item myIngredient = ingredientList.Find(_ => _.itemTypeId == requirement.itemTypeId);
            int ingredientCount = 0;

            if (myIngredient != null) {
               ingredientCount = myIngredient.count;
            } 

            bool passedRequirement = false;

            if (ingredientCount >= requirement.count) {
               passedRequirement = true;
               passedRequirementCount++;
            }
            craftableItem = itemCombo.resultItem.getCastItem();
            craftingRowList[i].injectItem(requirement.getCastItem(), ingredientCount, requirement.count, passedRequirement);
         }

         // Enables/Disables Craft Button if valid requirements
         if(passedRequirementCount >= requirementCount) {
            craftButton.gameObject.SetActive(true);
            craftDisabled.SetActive(false);
         }
         else {
            craftButton.gameObject.SetActive(false);
            craftDisabled.SetActive(true);
         }

         // Updates the Icon and description of the item
         previewItem();

         if (craftableItem.category == Item.Category.Weapon) {
            characterStack.updateWeapon(_userObjects.userInfo.gender, (Weapon.Type) craftableItem.itemTypeId, ColorType.Black, ColorType.Blue);
         } else if (craftableItem.category == Item.Category.Armor) {
            characterStack.updateArmor(_userObjects.userInfo.gender, (Armor.Type) craftableItem.itemTypeId, ColorType.Black, ColorType.Blue);
         }
      }
   }

   private void purge () {
      previewItem();
      for (int i = 0; i < craftingRowList.Count; i++) {
         craftingRowList[i].purgeData();
      }
   }

   private void craft () {
      if (craftableItem != null) {
         Item item = craftableItem;

         // Tells the server the item was crafted
         Global.player.rpc.Cmd_CraftItem(Blueprint.getEquipmentType(_currBlueprintRow.itemData.itemTypeId), item.itemTypeId);

         PanelManager.self.get(Type.Craft).hide();
         craftableItem = null;
      }
   }

   private void previewItem () {
      itemTitleText.text = "";
      itemInfoText.text = "";

      List<CraftableItemRequirements> dataList = RewardManager.self.craftableDataList;
      itemInfoText.text = "A blueprint design for: "+craftableItem.getDescription();
      itemTitleText.text = craftableItem.getName() + " Design";
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      // Clears listeners for existing templates
      if (listParent.childCount > 0) {
         foreach (Transform child in listParent) {
            child.GetComponent<BlueprintRow>().button.onClick.RemoveAllListeners();
         }
      }
      listParent.gameObject.DestroyChildren();

      // Adds crafting materials to view panel
      ingredientList = new List<Item>();
      List<Item> itemList = new List<Item>();
      foreach (Item item in itemArray) {
         itemList.Add(item.getCastItem());
      }

      for (int i = 0; i < itemList.Count; i++) {
         Item itemData = itemList[i].getCastItem();

         // Handles all the blueprints that can be crafted
         if (itemData.category == Item.Category.Blueprint) {
            GameObject prefab = Instantiate(this.blueprintRow.gameObject, listParent);
            BlueprintRow blueprintRow = prefab.GetComponent<BlueprintRow>();

            // Setting up the data of the blueprint template
            int ingredient = itemData.itemTypeId;
            Blueprint blueprint = new Blueprint(0, ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
            blueprint.itemTypeId = ingredient;

            // Determines what icon to preview in crafting panel
            Sprite blueprintIcon = emptyImage;
            if (Blueprint.getEquipmentType(blueprint.itemTypeId) == Item.Category.Weapon) {
               blueprintIcon = weaponBlueprintIcon;
            } else if (Blueprint.getEquipmentType(blueprint.itemTypeId) == Item.Category.Armor) {
               blueprintIcon = armorBlueprintIcon;
            }

            blueprintRow.button.onClick.AddListener(() => {
               clickMaterialRow(blueprintRow);
            });
            blueprintRow.initData(blueprint, blueprintIcon);
            prefab.SetActive(true);
         }
         else if (itemData.category == Item.Category.CraftingIngredients) {
            ingredientList.Add(itemData);
         }
      }

      // Updates player data preview
      playerNameText.text = Global.player.entityName;
      _userObjects = userObjects;
      characterStack.updateLayers(userObjects);
   }

   public void OnPointerClick (PointerEventData eventData) {

   }

   #region Private Variables

   // Cached Material To Craft
   private BlueprintRow _currBlueprintRow;

   // Cached Recipe
   private CraftingRow _currCraftingRow;

   // The last set of User Objects that we received
   protected UserObjects _userObjects;

   // Caches ingredients that are needed for crafting
   private Item[] _craftingIngredientList;

   #endregion
}
