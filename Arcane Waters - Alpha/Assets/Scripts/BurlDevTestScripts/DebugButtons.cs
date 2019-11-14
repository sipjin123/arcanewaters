#pragma warning disable

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
   public string itemData;

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

      if (GUILayout.Button("Trigger Loot Gain")) {
         Global.player.rpc.Cmd_RegisterAchievement(Global.player.userId, AchievementData.ActionType.LootGainTotal, 1, 0, 0);
      }

      if (GUILayout.Button("Trigger Gather Wood")) {
         Global.player.rpc.Cmd_RegisterAchievement(Global.player.userId, AchievementData.ActionType.GatherItem, 1, 6, 4);
      }

      if (GUILayout.Button("Create Achievement")) {
         AchievementData newData = new AchievementData {
            achievementName = "AName",
            achievementDescription = "ADesc",
            achievementUniqueID = "UniqueID",
            achievementType = AchievementData.ActionType.ArmorBuy,
            iconPath = "",
            itemCategory = 0,
            itemType = 0,
            count = 1
         };

         DB_Main.createAchievementData(newData, Global.player.userId);
      }
      if (GUILayout.Button("Get Achievement")) {
         List<AchievementData> newData = DB_Main.getAchievementDataList(Global.player.userId);

         foreach (AchievementData achieveData in newData) {
            Debug.LogError("----------------");
            Debug.LogError(achieveData.achievementName);
            Debug.LogError(achieveData.achievementUniqueID);
            Debug.LogError(achieveData.achievementType);
         }
      }

      if (GUILayout.Button("Update Achievement")) {
         AchievementData newData = new AchievementData {
            achievementName = "AName",
            achievementDescription = "ADesc",
            achievementUniqueID = "UniqueID",
            achievementType = AchievementData.ActionType.ArmorBuy,
            iconPath = "",
            itemCategory = 0,
            itemType = 0,
            count = 15
         };

         //DB_Main.updateAchievementData(newData, Global.player.userId);
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
               Item newItem = new Item(0, categoryType, int.Parse(itemType), int.Parse(quantity), ColorType.Black, ColorType.BlackEyes, itemData);
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
            GUILayout.BeginHorizontal();
            GUILayout.Box("ItemData: ", GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
            itemData = GUILayout.TextField(itemData, GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
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
                     Item.Category newCategory = Blueprint.getEquipmentType(itemTypeID);
                     if (newCategory == Item.Category.Weapon) {
                        itemTypeName = "Item Type: " + Blueprint.getName(itemTypeID);
                     } else if (newCategory == Item.Category.Armor) {
                        itemTypeName = "Item Type: " + Blueprint.getName(itemTypeID);
                     }
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
