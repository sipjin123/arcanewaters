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
   public Item.Category categoryType = Item.Category.CraftingIngredients;
   public string itemType;
   public string outputItem;
   public string itemData;

   public List<int> allSkills = new List<int>();
   public List<int> equppedSkills = new List<int>();

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
            EquippedAbilitiesSQL newSQL = new EquippedAbilitiesSQL();
            AllAbilitiesSQL allSQL = new AllAbilitiesSQL();
            allSkills = new List<int>();
            equppedSkills = new List<int>();

            for (int i = 0; i < 50; i++) {
               int randVal = Random.Range(1, 300);
               allSkills.Add(randVal);
               if (equppedSkills.Count < 5)
                  equppedSkills.Add(randVal);
            }
            allSQL.allAbilities = allSkills.ToArray();
            newSQL.equippedAbilities = equppedSkills.ToArray();
            DB_Main.updateAbilitiesData(Global.player.userId, newSQL, allSQL);
         }

         if (GUILayout.Button("Load All Skill List")) {
            List<int> newID = DB_Main.getAllAbilities(Global.player.userId);

            foreach (int fetchedID in newID) {
               Debug.LogError("ID: " + fetchedID);
            }
         }

         if (GUILayout.Button("Load All Equipped Skill List")) {
            List<int> newID = DB_Main.getEquipedAbilities(Global.player.userId);

            foreach (int fetchedID in newID) {
               Debug.LogError("ID: " + fetchedID);
            }
         }
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

#pragma warning restore

public class EquippedAbilitiesSQL
{
   // Array for equipped abilities
   public int[] equippedAbilities;
}

public class AllAbilitiesSQL
{
   // Array for all other abilities
   public int[] allAbilities;
}
