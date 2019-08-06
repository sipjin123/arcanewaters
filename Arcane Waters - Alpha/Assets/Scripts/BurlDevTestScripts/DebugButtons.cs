﻿using System.Collections.Generic;
using UnityEngine;
#if IS_SERVER_BUILD
using Mirror;
public class DebugButtons : NetworkBehaviour
{
   public GenericLootData tempDrop;

   private void processItem(Item item) {
      RewardManager.self.processLoot(item);
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
      if(Input.GetKeyDown(KeyCode.T)) {
         var temp = tempDrop.requestLootList();
         List<Item> itemList = new List<Item>();
         for(int i = 0; i < temp.Count; i++) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) temp[i].lootType, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;
            itemList.Add(item);
         }

         RewardManager.self.processLoots(itemList);
         return;
         /*
         var newLootlist = tempDrop.requestLootList();
         Debug.LogError("-------------------- I received this list : " + newLootlist.Count);
         List<Item> itemList = new List<Item>();
         for (int i = 0; i < newLootlist.Count; i++) {
            Debug.LogError(newLootlist[i].lootType);

            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) newLootlist[i].lootType, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;
            itemList.Add(item);
         }

         //----------------------------------------

         RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
         rewardPanel.setItemDataGroup(itemList);
         PanelManager.self.pushPanel(Panel.Type.Reward);*/


      }
      if(Input.GetKey(KeyCode.K)) {
         Anim.Type animationType = Anim.Type.Battle_East;
         /*
         foreach (SimpleAnimation anim in _anims) {
            anim.playAnimation(animationType);
         }*/
      }

      if (Input.GetKeyDown(KeyCode.Alpha9)) {
         //var itemToDelete = InventoryCacheManager.self.itemList.Find(_ => _.category == Item.Category.CraftingIngredients && (CraftingIngredients.Type) _.itemTypeId == CraftingIngredients.Type.Lizard_Scale);
         //Global.player.rpc.Cmd_DeleteItem(itemToDelete.id);
      }

      if(Input.GetKey(KeyCode.U)) {
         if (Input.GetKeyDown(KeyCode.Alpha1)) {
         }

         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            var areas = AreaManager.self.getAreas();
            Debug.LogError("The list of area is : " + areas.Count);
         }

         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            DB_Main.getNPCRelationInfo(Global.player.userId, 2);
         }
         if (Input.GetKeyDown(KeyCode.Alpha9)) {
            Debug.LogError("Requesting from server as -1");
            Global.player.rpc.Cmd_RequestItemsFromServer(-1, 15);
         }
         if (Input.GetKeyDown(KeyCode.Alpha8)) {
            Debug.LogError("Requesting from server as 1");
            Global.player.rpc.Cmd_RequestItemsFromServer(1, 15);
         }
      }

      if (Input.GetKey(KeyCode.Q)) {
         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_1, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_2, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_3, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_4, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Claw, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            //processItem(craftingIngredients);
            processRewardItems(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha6)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Scale, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            //processItem(craftingIngredients);
            processRewardItems(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha7)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lumber, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            //processItem(craftingIngredients);
            processRewardItems(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha8)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Gold_Ore, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
      }
   }
}

#endif

public static class DebugCustom
{
   public static string B = "[B0NTA] :: ";

   public static void Print (string wat) {
      Debug.LogError(B + wat);
   }
}