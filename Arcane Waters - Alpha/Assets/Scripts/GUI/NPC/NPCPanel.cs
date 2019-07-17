using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class NPCPanel : Panel
{
   #region Public Variables

   // The NPC associated with this panel
   public NPC npc;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Our head animation
   public SimpleAnimation headAnim;

   // Our Faction Icon
   public Image factionIcon;

   // Our Specialty Icon
   public Image specialtyIcon;

   // The Text that shows the NPC name
   public Text nameText;

   // The Text that shows our friendship number
   public Text friendshipText;

   // The container of our clickable rows
   public GameObject clickableRowContainer;

   // The prefab we use for creating NPC text rows
   public ClickableText clickableRowPrefab;

   // Self
   public static NPCPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      // Keep track of what our intro text is
      _greetingText = greetingText.text;
   }

   public void SetMessage (string text) {
      greetingText.text = text;
      _greetingText = text;
   }

   public override void show () {
      base.show();

      // Fill in the details for this NPC
      factionIcon.sprite = Faction.getIcon(npc.faction);
      specialtyIcon.sprite = Specialty.getIcon(npc.specialty);
      nameText.text = npc.npcName;

      // Show what our friendship rating is with this NPC
      friendshipText.text = "100";

      // Update the head image based on the type of NPC this is
      string path = "Faces/" + npc.GetComponent<SpriteSwap>().newTexture.name;
      Texture2D newTexture = ImageManager.getTexture(path);
      headAnim.GetComponent<SimpleAnimation>().setNewTexture(newTexture);

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);
   }

   public void setClickableRows (List<ClickableText.Type> options) {
      // Clear out any old stuff
      clickableRowContainer.DestroyChildren();

      // Create a clickable text row for each option in the list
      foreach (ClickableText.Type option in options) {
         ClickableText row = Instantiate(clickableRowPrefab);
         row.transform.SetParent(clickableRowContainer.transform);

         // Set up the click function
         row.clickedEvent.AddListener(() => rowClickedOn(row, this.npc));

         // Set the type
         row.textType = option;

         row.gameObject.SetActive(false);
      }
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      foreach (Transform child in clickableRowContainer.transform) {
         child.gameObject.SetActive(true);
      }

      List<Item> itemList = new List<Item>();
      foreach (Item item in itemArray) {
         if (item.category == Item.Category.CraftingIngredients) {
            var findItem = itemList.Find(_ => _.category == Item.Category.CraftingIngredients &&
            ((CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) item.itemTypeId));
            if (findItem != null) {
               int index = itemList.IndexOf(findItem);
               itemList[index].count++;
            } else {
               itemList.Add(item.getCastItem());
            }
         } else {
            itemList.Add(item.getCastItem());
         }
      }

      if (npc.dialogeTypes[0] == ClickableText.Type.TradeDeliveryComplete) {
         var getDeliveryList = npc.npcData.npcQuestList[0].deliveryQuests[0].deliveryList;

         var findingItemList = itemList.Find(_ => (CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) getDeliveryList[0].itemToDeliver.itemTypeId);
         if (findingItemList != null) {

            if (findingItemList.count >= getDeliveryList[0].quantity) {
               DebugCustom.Print("[QUANTITY] Current is : " + findingItemList.count + "  Requred: " + getDeliveryList[0].quantity);
               DebugCustom.Print("Found a requiremet");
            }
         }
      }

   }

   public void rowClickedOn (ClickableText row, NPC npc) {
      // Tell the server what we clicked
      //Global.player.rpc.Cmd_ClickedNPCRow(npc.npcId, row.textType);

      switch (row.textType) {
         case ClickableText.Type.TradeBluePrint:

            break;
         case ClickableText.Type.TradeDeliveryInit:
            NPCQuestData questData = npc.npcData.npcQuestList[0];
            QuestManager.self.RegisterQuest(questData);

            npc.UnlockDialogue(questData, true);
            break;
         case ClickableText.Type.TradeDeliveryComplete:


            NPCQuestData questData1 = npc.npcData.npcQuestList[0];

            DeliverDataClass deliverList = questData1.deliveryQuests[0].deliveryList[0];

            Debug.LogError("I wanna deliver : " + deliverList.itemToDeliver.getName() + " " + deliverList.quantity);

            QuestManager.self.ClearQuest(questData1);

            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Lizard_Scale, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);

            npc.NoDialogues();
            break;
         case ClickableText.Type.TradeGossip:

            break;
      }

      PanelManager.self.get(Type.NPC_Panel).hide();
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   #endregion
}
