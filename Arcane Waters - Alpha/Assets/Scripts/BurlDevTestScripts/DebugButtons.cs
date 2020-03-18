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
   public Item.Category categoryType = Item.Category.CraftingIngredients;
   public string itemType;
   public string outputItem;
   public string itemData;
   public string abilityIDData;

   #endregion

   private void OnGUI () {
      if (!enableDebug) {
         return;
      }

      simulateAbilityData();

      simulateItemReceive();
   }

   private void simulateAbilityData () {
      GUILayout.BeginHorizontal("box");
      {
         if (GUILayout.Button("Create Skill List")) {
            List<AbilitySQLData> sqlList = new List<AbilitySQLData>();

            for (int i = 0; i < 5; i++) {
               AbilitySQLData newSQL = new AbilitySQLData();
               newSQL.abilityID = i;
               newSQL.name = "Name: " + Random.Range(0, 100);
               newSQL.description = "test desc";
               newSQL.equipSlotIndex = i;
               newSQL.abilityLevel = 1;

               sqlList.Add(newSQL);
            }
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               foreach (AbilitySQLData sqlDat in sqlList) {
                  DB_Main.updateAbilitiesData(Global.player.userId, sqlDat);
               }
            });
         }
         
         if (GUILayout.Button("Load All Skill List")) {
            List<AbilitySQLData> newID = DB_Main.getAllAbilities(Global.player.userId);

            foreach (AbilitySQLData fetchedID in newID) {
               Debug.LogError("ID: " + fetchedID.abilityID+" - "+fetchedID.name);
            }
         }

         if (GUILayout.Button("Open Crafting")) {
            // Get the crafting panel
            PanelManager.self.selectedPanel = Panel.Type.Craft;
            CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

            // If the panel is not showing, send a request to the server to get the crafting data
            if (!panel.isShowing()) {
               panel.clearSelectedBlueprint();
               panel.refreshBlueprintList();
            }
         }

         if (GUILayout.Button("Open Recruitment")) {
            Global.player.rpc.Cmd_RequestCompanionData();
         }

         if (GUILayout.Button("Update Skill")) {
            AbilitySQLData newSQL = new AbilitySQLData();
            newSQL.abilityID = int.Parse( abilityIDData );
            newSQL.name = "new name";
            newSQL.description = "new desc";
            newSQL.abilityLevel = 2;
            newSQL.equipSlotIndex = 99;

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               DB_Main.updateAbilitiesData(Global.player.userId, newSQL);
            });
         }
         GUILayout.BeginHorizontal();
         GUILayout.Box("ItemSkill: ", GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
         abilityIDData = GUILayout.TextField(abilityIDData, GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
         GUILayout.EndHorizontal();
      }
      GUILayout.EndHorizontal();
   }

   private void simulateItemReceive () {
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
            GUILayout.Box("Quantity: ", GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
            quantity = GUILayout.TextField(quantity, GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Box("ItemTypeID: ", GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
            itemType = GUILayout.TextField(itemType, GUILayout.Width(buttonSizeX / 2), GUILayout.Height(buttonSizeY));
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
                     itemTypeName = "Item Type: " + (CraftingIngredients.Type) itemTypeID;
                     break;
                  case Item.Category.Armor:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, EquipmentXMLManager.self.armorStatList.Count - 1);
                     itemTypeName = "Item Type: " + itemTypeID;
                     break;
                  case Item.Category.Weapon:
                     itemTypeID = Mathf.Clamp(itemTypeID, 0, EquipmentXMLManager.self.weaponStatList.Count - 1);
                     itemTypeName = "Item Type: " + itemTypeID;
                     break;

               }
               outputItem = "Category: " + categoryType.ToString() + "\nQuantity: " + quantity + "\n" + itemTypeName;
            }
            GUILayout.Label("Final output is: \n" + outputItem, GUILayout.Width(buttonSizeX), GUILayout.Height(buttonSizeY * 2));
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