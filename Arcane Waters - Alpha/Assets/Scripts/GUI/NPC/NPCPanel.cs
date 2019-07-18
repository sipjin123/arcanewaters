using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class NPCPanel : Panel {
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

      // Sets the index of the dialogue
      int index = 1;
      // Create a clickable text row for each option in the list
      foreach (ClickableText.Type option in options) {
         ClickableText row = Instantiate(clickableRowPrefab);
         row.transform.SetParent(clickableRowContainer.transform);
         // Set the type
         row.textType = option;
         row.initData(index);
         index++;

         // Set up the click function
         row.clickedEvent.AddListener(() => rowClickedOn(row, this.npc));
      }
   }

   public void rowClickedOn (ClickableText row, NPC npc) {
      // Tell the server what we clicked
      Global.player.rpc.Cmd_ClickedNPCRow(npc.npcId, row.textType);

      switch (row.textType) {
         case ClickableText.Type.TradeDeliveryInit:
            // Initialized the quest
            NPCQuestData questData = npc.npcData.npcQuestList[0];
            QuestManager.self.registerQuest(questData);
            break;
         case ClickableText.Type.TradeDeliverySuccess:
            NPCQuestData questData1 = npc.npcData.npcQuestList[0];
            QuestManager.self.clearQuest(questData1);

            List<Item> rawList = InventoryCacheManager.self.rawItemList;
            DeliverQuest questPair = npc.npcData.npcQuestList[0].deliveryQuestList[0].deliveryQuest;
            int countToDelete = questPair.quantity;

            // Deletes items from the inventory equivalent to the Deliver Quest requirements
            int deleteCounter = 0;
            for (int i = 0; i < rawList.Count; i++) {
               if (rawList[i].category == questPair.itemToDeliver.category) {
                  if (deleteCounter >= countToDelete) {
                     break;
                  }
                  if (rawList[i].itemTypeId == questPair.itemToDeliver.itemTypeId) {
                     deleteCounter++;
                     Global.player.rpc.Cmd_DeleteItem(rawList[i].id);
                  }
               }
            }

            //Add Reward Item
            CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) CraftingIngredients.Type.Ore, ColorType.DarkGreen, ColorType.DarkPurple, "");
            craftingIngredients.itemTypeId = (int) craftingIngredients.type;
            Item item = craftingIngredients;

            PanelManager.self.rewardScreen.Show(item);
            Global.player.rpc.Cmd_DirectAddItem(item);

            npc.npcData.npcQuestList[0].deliveryQuestList[0].questState = QuestState.Completed;
            break;
      }
      PanelManager.self.popPanel();
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   #endregion
}
