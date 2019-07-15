
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class CraftingPanel : Panel, IPointerClickHandler
{

    [SerializeField]
    private CombinationDataList combinationDataList;
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

    public override void Start()
    {
        base.Start();
        for (int i = 0; i < 5; i++)
        {
            var prefab = Instantiate(craftingMetrialRow.gameObject, listParent);
            var materialRow = prefab.GetComponent<CraftingMaterialRow>();


            int ingredient = (int)CraftingIngredients.Type.Lizard_Scale;
            if (i == 4) ingredient = (int)CraftingIngredients.Type.Flint;
            if (i == 3) ingredient = (int)CraftingIngredients.Type.Lumber;
            if (i == 2 ) ingredient = (int)CraftingIngredients.Type.Lizard_Claw;
            if (i == 1) ingredient = (int)CraftingIngredients.Type.Ore;

            CraftingIngredients craftingIngredients = new CraftingIngredients(0, ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int)craftingIngredients.type;
            Item item = craftingIngredients;



            materialRow.Button.onClick.AddListener(() =>
            {
                ClickMaterialRow(materialRow);
            });
            materialRow.InitData(item);
            prefab.SetActive(true);
        }

        for(int i = 0; i < craftingRowList.Count; i++)
        {
            var rowData = craftingRowList[i];
            craftingRowList[i].Button.onClick.AddListener(() => 
            {
                ClickCraftRow(rowData);
            });
        }

        clearButton.onClick.AddListener(() => { Purge(); });
        useButton.onClick.AddListener(() => { SelectItem(); });
        removeButton.onClick.AddListener(() => { RemoveItem(); });
        craftButton.onClick.AddListener(() => { Craft(); });
    }

    private void ClickMaterialRow(CraftingMaterialRow currItem)
    {
        DebugCustom.Print("Curr item is : " + currItem.ItemData.getName());
        if (currCraftingMaterialRow != null)
        {
            if (currCraftingMaterialRow != currItem)
                currCraftingMaterialRow.DeselectItem();
        }
        currItem.SelectItem();
        currCraftingMaterialRow = currItem;
    }
    private void ClickCraftRow(CraftingRow currItem)
    {
        if (currItem == null)
        {
            return;
        }
        if (currCraftingRow != null)
        {
            if(currCraftingRow == currItem)
            {
                return;
            }
            else
            {
                currCraftingRow.UnselectItem();
            }
        }

        currCraftingRow = currItem;
        currCraftingRow.SelectItem();
    }

    private void Purge()
    {

        for (int i = 0; i < craftingRowList.Count; i++)
        {
            craftingRowList[i].PurgeData();
        }
    }
    private void Craft()
    {
        if(craftableItem != null)
        {
            Item item = craftableItem;
            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);
            PanelManager.self.get(Type.Craft).hide();
            craftableItem = null;
        }
    }
    private void SelectItem()
    {
        if (currCraftingMaterialRow == null)
            return;

        bool hasInjected = false;
        for(int  i = 0; i < craftingRowList.Count; i++)
        {
            if(!craftingRowList[i].HasData)
            {
                hasInjected = true;
                craftingRowList[i].InjectItem(currCraftingMaterialRow.ItemData);
                break;
            }
        }
        if(hasInjected == false)
        {
            craftingRowList[0].InjectItem(currCraftingMaterialRow.ItemData);
        }

        if(currCraftingRow)
        currCraftingRow.UnselectItem();
        currCraftingRow = null;
        if(currCraftingMaterialRow)
        currCraftingMaterialRow.DeselectItem();
        currCraftingMaterialRow = null;

        ComputeCombinations();
    }
    private void RemoveItem()
    {
        if (currCraftingRow == null)
            return;
        currCraftingRow.UnselectItem();
        currCraftingRow.PurgeData();
        currCraftingRow = null;
    }

    void ComputeCombinations()
    {
        int counter = 0;
        List<Item> rawIngredients = new List<Item>();
        for(int i = 0; i <craftingRowList.Count; i++)
        {
            if(craftingRowList[i].HasData)
            {
                counter++;
                rawIngredients.Add(craftingRowList[i].Item);
                DebugCustom.Print("my category is : " + craftingRowList[i].Item.category);
            }
        }



        bool foundTheOne = false;
        if(counter == 3)
        {
            DebugCustom.Print("it is complete");

            var dataList = combinationDataList.ComboDataList;
            for(int i = 0; i < dataList.Count; i++)
            {
                if(dataList[i].CheckIfRequirementsPass(rawIngredients))
                {
                    DebugCustom.Print("I found the one  " + dataList[i].ResultItem.getCastItem().getName());
                    resultImage.sprite = ImageManager.getSprite(dataList[i].ResultItem.getCastItem().getIconPath());
                    craftableItem = dataList[i].ResultItem.getCastItem();
                    break;
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }
}
