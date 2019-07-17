
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class CraftingPanel : Panel, IPointerClickHandler
{
   #region Public Variables
   public CombinationDataList combinationDataList;
   // The components we manage
   public Text resultItemText;
   public Text itemTitleText;
   public Text itemInfoText;
   public Text goldText;
   public Text gemsText;

   [SerializeField]
   private Image resultImage;

   [SerializeField]
   private CraftingMaterialRow craftingMetrialRow;

   private CraftingMaterialRow currCraftingMaterialRow;
   private CraftingRow currCraftingRow;

   [SerializeField]
   private List<CraftingRow> craftingRowList;

   [SerializeField]
   private Transform listParent;

   [SerializeField]
   private Button craftButton, useButton, removeButton, clearButton;

   [SerializeField]
   private Item craftableItem;
   #endregion

   public override void Start () {
      base.Start();

      for (int i = 0; i < craftingRowList.Count; i++) {
         var rowData = craftingRowList[i];
         craftingRowList[i].button.onClick.AddListener(() => {
            ClickCraftRow(rowData);
         });
      }

      clearButton.onClick.AddListener(() => { Purge(); });
      useButton.onClick.AddListener(() => { SelectItem(); });
      removeButton.onClick.AddListener(() => { RemoveItem(); });
      craftButton.onClick.AddListener(() => { Craft(); });
   }

   public void requestInventoryFromServer (int pageNumber) {

      // Get the latest info from the server to show in our character stack
      Global.player.rpc.Cmd_RequestItemsFromServer(pageNumber, 15);
   }

   private void ClickMaterialRow (CraftingMaterialRow currItem) {
      if (currCraftingMaterialRow != null) {
         if (currCraftingMaterialRow != currItem)
            currCraftingMaterialRow.DeselectItem();
      }
      currItem.SelectItem();
      currCraftingMaterialRow = currItem;
   }
   private void ClickCraftRow (CraftingRow currItem) {
      if (currItem == null) {
         return;
      }
      if (currCraftingRow != null) {
         if (currCraftingRow == currItem) {
            return;
         } else {
            currCraftingRow.UnselectItem();
         }
      }

      currCraftingRow = currItem;
      currCraftingRow.SelectItem();
   }

   private void Purge () {
      ComputeCombinations();
      for (int i = 0; i < craftingRowList.Count; i++) {
         craftingRowList[i].PurgeData();
      }
   }
   private void Craft () {
      if (craftableItem != null) {
         Item item = craftableItem;
         PanelManager.self.rewardScreen.Show(item);
         Global.player.rpc.Cmd_DirectAddItem(item);
         PanelManager.self.get(Type.Craft).hide();
         craftableItem = null;
      }
   }
   private void SelectItem () {
      if (currCraftingMaterialRow == null)
         return;

      bool hasInjected = false;
      for (int i = 0; i < craftingRowList.Count; i++) {
         if (!craftingRowList[i].hasData) {
            hasInjected = true;
            craftingRowList[i].InjectItem(currCraftingMaterialRow.ItemData);
            break;
         }
      }
      if (hasInjected == false) {
         craftingRowList[0].InjectItem(currCraftingMaterialRow.ItemData);
      }

      if (currCraftingRow)
         currCraftingRow.UnselectItem();
      currCraftingRow = null;
      if (currCraftingMaterialRow)
         currCraftingMaterialRow.DeselectItem();
      currCraftingMaterialRow = null;

      ComputeCombinations();
   }
   private void RemoveItem () {
      if (currCraftingRow == null)
         return;
      currCraftingRow.UnselectItem();
      currCraftingRow.PurgeData();
      ComputeCombinations();
      currCraftingRow = null;
   }

   void ComputeCombinations () {
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

         var dataList = combinationDataList.ComboDataList;
         for (int i = 0; i < dataList.Count; i++) {
            if (dataList[i].CheckIfRequirementsPass(rawIngredients)) {
               resultImage.sprite = ImageManager.getSprite(dataList[i].ResultItem.getCastItem().getIconPath());
               craftableItem = dataList[i].ResultItem.getCastItem();
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
            child.GetComponent<CraftingMaterialRow>().Button.onClick.RemoveAllListeners();
         }
      }
      listParent.gameObject.DestroyChildren();

      List<Item> itemList = new List<Item>();
      foreach (Item item in itemArray) {
         itemList.Add(item.getCastItem());
      }

      for (int i = 0; i < itemList.Count; i++) {
         var itemData = itemList[i].getCastItem();


         if (itemData.category != Item.Category.CraftingIngredients) {

         } else {
            var prefab = Instantiate(craftingMetrialRow.gameObject, listParent);
            var materialRow = prefab.GetComponent<CraftingMaterialRow>();

            int ingredient = itemData.itemTypeId;
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            materialRow.Button.onClick.AddListener(() => {
               ClickMaterialRow(materialRow);
            });
            materialRow.InitData(item);
            prefab.SetActive(true);
         }
      }

   }


   public void OnPointerClick (PointerEventData eventData) {

   }
}
