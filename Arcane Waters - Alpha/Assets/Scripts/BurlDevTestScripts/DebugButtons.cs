using UnityEngine;

public class DebugButtons : MonoBehaviour
{
   private void Update () {

      if (Input.GetKeyDown(KeyCode.Alpha8)) {
         var itemToDelete = InventoryCacheManager.self.itemList.Find(_ => _.category == Item.Category.CraftingIngredients && (CraftingIngredients.Type) _.itemTypeId == CraftingIngredients.Type.Lizard_Scale);
         Global.player.rpc.Cmd_DeleteItem(itemToDelete.id);
      }
      if (Input.GetKey(KeyCode.Alpha0)) {
         if (Input.GetKeyDown(KeyCode.O)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Ore, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);
         }
         if (Input.GetKeyDown(KeyCode.P)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lumber, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Scale, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);
         }
      }
   }
}

public static class DebugCustom
{
   public static string B = "[B0NTA] :: ";

   public static void Print (string wat) {
      Debug.LogError(B + wat);
   }
}