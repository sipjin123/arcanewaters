using UnityEngine;
#if UNITY_EDITOR
public class DebugButtons : MonoBehaviour
{
   private void processItem(Item item) {

      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(item);
      PanelManager.self.pushPanel(Panel.Type.Reward);

      Global.player.rpc.Cmd_DirectAddItem(item);
   }

   private void Update () {

      if (Input.GetKeyDown(KeyCode.Alpha8)) {
         var itemToDelete = InventoryCacheManager.self.itemList.Find(_ => _.category == Item.Category.CraftingIngredients && (CraftingIngredients.Type) _.itemTypeId == CraftingIngredients.Type.Lizard_Scale);
         Global.player.rpc.Cmd_DeleteItem(itemToDelete.id);
      }
      if (Input.GetKey(KeyCode.Q)) {
         if(Input.GetKeyDown(KeyCode.Alpha1)) {

         }

         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            Debug.LogError("REward me a blueprint");
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_1, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            Debug.LogError("REward me a blueprint");
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_2, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            Debug.LogError("REward me a blueprint");
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Sword_3, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            Debug.LogError("REward me a blueprint");
            Blueprint craftingIngredients = new Blueprint(0, (int) Blueprint.Type.Lizard_Sword, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }

         if (Input.GetKeyDown(KeyCode.O)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Ore, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.P)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lumber, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            processItem(craftingIngredients);
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Scale, ColorType.DarkGreen, ColorType.DarkPurple, "");
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