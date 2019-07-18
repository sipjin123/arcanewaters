using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingPanel : Panel, IPointerClickHandler
{
   #region Public Variables
   // List of items that can be crafted
   public CombinationDataList combinationDataList;
   // The name of the item to be crafted 
   public Text resultItemText;
   // The name of the item above the description to be crafted
   public Text itemTitleText;
   // The details of the item to b crafted
   public Text itemInfoText;
   // The cost of crafting the item
   public Text goldText;
   public Text gemsText;

   // The Icon of the item to be crafted
   public Image resultImage;

   // The Templates for the raw materials used for crafting
   public CraftingMaterialRow craftingMetrialRow;

   // The crafting slots
   public List<CraftingRow> craftingRowList;

   // The holder of the current items that can be usef for crafting
   public Transform listParent;

   public Button craftButton, useButton, removeButton, clearButton;

   // Caches the item that can be crafted
   public Item craftableItem;
   #endregion

   public override void Start () {
      base.Start();

      for (int i = 0; i < craftingRowList.Count; i++) {
         CraftingRow rowData = craftingRowList[i];
         craftingRowList[i].button.onClick.AddListener(() => {
            clickCraftRow(rowData);
         });
      }

      clearButton.onClick.AddListener(() => { purge(); });
      useButton.onClick.AddListener(() => { selectItem(); });
      removeButton.onClick.AddListener(() => { removeItem(); });
      craftButton.onClick.AddListener(() => { craft(); });
   }

   public void requestInventoryFromServer (int pageNumber) {
      // Get the latest info from the server to show in our character stack
      Global.player.rpc.Cmd_RequestItemsFromServer(pageNumber, 15);
   }

   private void clickMaterialRow (CraftingMaterialRow currItem) {
      if (_currCraftingMaterialRow != null) {
         if (_currCraftingMaterialRow != currItem) {
            _currCraftingMaterialRow.deselectItem();
         }
      }
      currItem.selectItem();
      _currCraftingMaterialRow = currItem;
   }

   private void clickCraftRow (CraftingRow currItem) {
      if (currItem == null) {
         return;
      }

      if (_currCraftingRow != null) {
         if (_currCraftingRow == currItem) {
            return;
         } else {
            _currCraftingRow.unselectItem();
         }
      }
      _currCraftingRow = currItem;
      _currCraftingRow.selectItem();
   }

   private void purge () {
      computeCombinations();
      for (int i = 0; i < craftingRowList.Count; i++) {
         craftingRowList[i].purgeData();
      }
   }

   private void craft () {
      if (craftableItem != null) {
         Item item = craftableItem;
         PanelManager.self.rewardScreen.Show(item);
         Global.player.rpc.Cmd_DirectAddItem(item);
         PanelManager.self.get(Type.Craft).hide();
         craftableItem = null;
      }
   }

   private void selectItem () {
      if (_currCraftingMaterialRow == null) {
         return;
      }

      bool hasInjected = false;

      for (int i = 0; i < craftingRowList.Count; i++) {
         if (!craftingRowList[i].hasData) {
            hasInjected = true;
            craftingRowList[i].injectItem(_currCraftingMaterialRow.itemData);
            break;
         }
      }

      if (hasInjected == false) {
         craftingRowList[0].injectItem(_currCraftingMaterialRow.itemData);
      }

      if (_currCraftingRow) {
         _currCraftingRow.unselectItem();
      }
      _currCraftingRow = null;

      if (_currCraftingMaterialRow) {
         _currCraftingMaterialRow.deselectItem();
      }
      _currCraftingMaterialRow = null;

      computeCombinations();
   }

   private void removeItem () {
      if (_currCraftingRow == null) {
         return;
      }
      _currCraftingRow.unselectItem();
      _currCraftingRow.purgeData();
      computeCombinations();
      _currCraftingRow = null;
   }

   private void computeCombinations () {
      int counter = 0;
      List<Item> rawIngredients = new List<Item>();

      for (int i = 0; i < craftingRowList.Count; i++) {
         if (craftingRowList[i].hasData) {
            counter++;
            rawIngredients.Add(craftingRowList[i].item);
         }
      }

      itemTitleText.text = "";
      itemInfoText.text = "";
      resultImage.sprite = null;
      resultItemText.text = "";

      if (counter == 3) {
         List<CombinationData> dataList = combinationDataList.comboDataList;
         for (int i = 0; i < dataList.Count; i++) {
            if (dataList[i].checkIfRequirementsPass(rawIngredients)) {
               resultImage.sprite = ImageManager.getSprite(dataList[i].resultItem.getCastItem().getIconPath());
               craftableItem = dataList[i].resultItem.getCastItem();
               itemInfoText.text = craftableItem.getDescription();
               itemTitleText.text = craftableItem.getName();
               resultItemText.text = craftableItem.getName();
               break;
            }
         }
      }
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      if (listParent.childCount > 0) {
         foreach (Transform child in listParent) {
            child.GetComponent<CraftingMaterialRow>().button.onClick.RemoveAllListeners();
         }
      }
      listParent.gameObject.DestroyChildren();
      List<Item> itemList = new List<Item>();

      foreach (Item item in itemArray) {
         itemList.Add(item.getCastItem());
      }

      for (int i = 0; i < itemList.Count; i++) {
         Item itemData = itemList[i].getCastItem();

         if (itemData.category == Item.Category.CraftingIngredients) {
            GameObject prefab = Instantiate(craftingMetrialRow.gameObject, listParent);
            CraftingMaterialRow materialRow = prefab.GetComponent<CraftingMaterialRow>();

            int ingredient = itemData.itemTypeId;
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            materialRow.button.onClick.AddListener(() => {
               clickMaterialRow(materialRow);
            });
            materialRow.initData(item);
            prefab.SetActive(true);
         }
      }
   }

   public void OnPointerClick (PointerEventData eventData) {

   }

   #region Private Variables
   private CraftingMaterialRow _currCraftingMaterialRow;
   private CraftingRow _currCraftingRow;
   #endregion
}
