﻿#pragma warning disable

using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if IS_SERVER_BUILD
using Mirror;
public class DebugButtons : NetworkBehaviour
{
   #region Public Variables

   public GenericLootData tempDrop;

   public bool enableDebug;

   public string quantity;
   public Item.Category categoryType;
   public string itemType;
   public string outputItem;

   #endregion

   private void processItem(Item item) {
      RewardManager.self.showItemInRewardPanel(item);
   }

   private void Start () {
      categoryType = Item.Category.CraftingIngredients;
   }

   private void OnGUI () {
      if (!enableDebug) {
         return;
      }

      GUILayout.BeginHorizontal("box");
      {
         GUILayout.BeginVertical("box");
         {
            foreach (Item.Category category in System.Enum.GetValues(typeof(Item.Category))) {
               if (category == Item.Category.CraftingIngredients || category == Item.Category.Blueprint || category == Item.Category.Weapon || category == Item.Category.Armor) {
                  if (GUILayout.Button("Select CraftingItems:: " + category, GUILayout.Width(buttonSizeX), GUILayout.Height(buttonSizeY))) {
                     categoryType = (Item.Category) System.Enum.Parse(typeof(Item.Category), category.ToString(), true);
                  }
               }
            }
         }
         GUILayout.EndVertical();

         GUILayout.BeginVertical("box");
         {
            if (GUILayout.Button("GENERATE ITEM: " + categoryType.ToString(), GUILayout.Width(buttonSizeX), GUILayout.Height(buttonSizeY * 2))) {
               Item newItem = new Item(0, categoryType, int.Parse(itemType), int.Parse(quantity), ColorType.Black, ColorType.BlackEyes, "");
               int newQuantity = int.Parse(quantity);
               newQuantity = Mathf.Clamp(newQuantity, 0, 100);
               newItem.count = newQuantity;

               processRewardItems(newItem);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box("Quantity: ", GUILayout.Width(buttonSizeX/2), GUILayout.Height(buttonSizeY));
            quantity = GUILayout.TextField(quantity, GUILayout.Width(buttonSizeX/2), GUILayout.Height(buttonSizeY));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Box("ItemTypeID: ", GUILayout.Width(buttonSizeX/2), GUILayout.Height(buttonSizeY));
            itemType = GUILayout.TextField(itemType, GUILayout.Width(buttonSizeX/2), GUILayout.Height(buttonSizeY));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Check final output: ", GUILayout.Width(buttonSizeX), GUILayout.Height(buttonSizeY))) {
               string itemTypeName = "";
               int itemTypeID = int.Parse(itemType);
               switch (categoryType) {
                  case Item.Category.CraftingIngredients:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, System.Enum.GetNames(typeof(CraftingIngredients.Type)).Length - 1);
                     itemTypeName = "Item Type: "+(CraftingIngredients.Type) itemTypeID;
                     break;
                  case Item.Category.Armor:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, System.Enum.GetNames(typeof(Armor.Type)).Length - 1);
                     itemTypeName = "Item Type: " + (Armor.Type) itemTypeID;
                     break;
                  case Item.Category.Blueprint:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, System.Enum.GetNames(typeof(Blueprint.Type)).Length - 1);
                     itemTypeName = "Item Type: " + (Blueprint.Type) itemTypeID;
                     break;
                  case Item.Category.Weapon:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, System.Enum.GetNames(typeof(Weapon.Type)).Length - 1);
                     itemTypeName = "Item Type: " + (Weapon.Type) itemTypeID;
                     break;

               }
               outputItem = "Category: "+categoryType.ToString() + "\nQuantity: " + quantity + "\n" + itemTypeName;
            }
            GUILayout.Label("Final output is: \n"+outputItem, GUILayout.Width(buttonSizeX), GUILayout.Height(buttonSizeY * 2));
         }
         GUILayout.EndVertical();
      }
      GUILayout.EndHorizontal();
   }

   [Server]
   public void processRewardItems (Item item) {
      // Editor debug purposes, direct adding of item to player
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         item = DB_Main.createNewItem(Global.player.userId, item);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Tells the user to update their inventory cache to retrieve the updated items
            Global.player.rpc.Target_UpdateInventory(Global.player.connectionToClient);
         });
      });
   }

   private void Update () {

      if (Input.GetKeyDown(KeyCode.Q)) {
         enableDebug = !enableDebug;
      }

      if(Input.GetKeyDown(KeyCode.Tilde)) {
         var temp = tempDrop.requestLootList();
         List<Item> itemList = new List<Item>();
         for(int i = 0; i < temp.Count; i++) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) temp[i].lootType, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;
            itemList.Add(item);
         }

         RewardManager.self.showItemsInRewardPanel(itemList);
      }
   }

   #region Private Variables

   private float buttonSizeX = 250;

   private float buttonSizeY = 50;

   private Random.State seedGenerator;

   private int seedGeneratorSeed = 1337;

   #endregion
}

#endif

public static class DebugCustom
{
   public static string B = "[B0NTA] :: ";

   public static void Print (string wat) {
      Debug.LogError(B + wat);
   }
}

#pragma warning restore
