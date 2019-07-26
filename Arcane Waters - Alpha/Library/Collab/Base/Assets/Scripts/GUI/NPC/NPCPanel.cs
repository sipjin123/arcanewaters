using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
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
      }
   }

   public void rowClickedOn (ClickableText row, NPC npc) {
      // Tell the server what we clicked
      Global.player.rpc.Cmd_ClickedNPCRow(npc.npcId, row.textType);
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   #endregion
}
