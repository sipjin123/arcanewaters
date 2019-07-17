using UnityEngine;

public class DebugButtons : MonoBehaviour
{
   private void Update () {
     
      if (Input.GetKey(KeyCode.M)) {
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
         if (Input.GetKeyDown(KeyCode.I)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Claw, ColorType.DarkGreen, ColorType.DarkPurple, "");
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