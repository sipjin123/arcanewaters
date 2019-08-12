using System.Collections.Generic;
using UnityEngine;
#if IS_SERVER_BUILD
using Mirror;
public class DebugButtons : NetworkBehaviour
{
   public GenericLootData tempDrop;

   private void processItem(Item item) {
      RewardManager.self.showItemInRewardPanel(item);
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

   Random.State seedGenerator;
   int seedGeneratorSeed = 1337;
   private void Update () {
      if (Global.player.isLocalPlayer) {
         if (Input.GetKeyDown(KeyCode.T)) {

            //Global.player.requestAnimationPlay();
            Global.player.rpc.Cmd_InteractAnimation(Anim.Type.Mining);
         }
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

      if(Input.GetKey(KeyCode.U)) {
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            seedGeneratorSeed = 1137;
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            seedGeneratorSeed = 1037;
         }

         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            bool seedGeneratorInitialized = false;
            // remember old seed
            var temp = Random.state;
 
            // initialize generator state if needed
            if (!seedGeneratorInitialized)
            {
                  Random.InitState(seedGeneratorSeed);
                  seedGenerator = Random.state;
                  seedGeneratorInitialized = true;
            }
 
            // set our generator state to the seed generator
            Random.state = seedGenerator;
            // generate our new seed
            var generatedSeed = Random.Range(int.MinValue, int.MaxValue);
            // remember the new generator state
            seedGenerator = Random.state;
            // set the original state back so that normal random generation can continue where it left off
            Random.state = temp;
            Debug.LogError("SEED GEB : "+generatedSeed);
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