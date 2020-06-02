using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class TooltipManager : ClientMonoBehaviour {
   #region Public Variables

   // The tooltip object we manage
   public Tooltip tooltip;

   // Self
   public static TooltipManager self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   public void Update () {
      // Figure out where we want the tooltip to show up
      Vector2 pos = Input.mousePosition + new Vector3(0f, 8f);

      // Keep it within the screen bounds
      pos.x = Mathf.Clamp(pos.x, 0 + tooltip.rectTransform.sizeDelta.x, Screen.width);
      pos.y = Mathf.Clamp(pos.y, 0, Screen.height - tooltip.rectTransform.sizeDelta.y);
      
      // Keep the tooltip at the mouse hotspot
      Util.setXY(tooltip.transform, pos);

      // Check if there's any text to show based on where the mouse currently is
      string tooltipText = getRelevantTooltip();

      // Decide whether the tooltip should be visible
      bool shouldTooltipShow = !Util.isEmpty(tooltipText);

      // Toggle visibility
      tooltip.gameObject.SetActive(shouldTooltipShow);

      // Sets the text in the component
      tooltip.text.SetText(tooltipText);
   }

   public string getRelevantTooltip () {
      GameObject gameObjectUnderMouse = StandaloneInputModuleV2.self.getGameObjectUnderPointer();
      if (gameObjectUnderMouse == null) {
         return "";
      }

      // Check if there's a tooltipped gameobject at the mouse position
      Tooltipped tooltipped = gameObjectUnderMouse.GetComponent<Tooltipped>();

      // If there is a tooltipped gameObject, returns its tooltip
      if (tooltipped != null) {
         return tooltipped.text;
      }

      // Check if there's an image at the mouse position
      Image image = gameObjectUnderMouse.GetComponent<Image>();

      // If there was an image, look up the text that's associated with it
      if (image != null && image.sprite != null) {
               
         // Get a generic tooltip based on the image name
         string tooltip = getTooltip(image.sprite.name, image.gameObject);

         // If we found an image that has a tooltip defined, then we're done
         if (!Util.isEmpty(tooltip)) {
            return tooltip;
         }
      }

      return "";
   }

   public static string getTooltip (string imageName, GameObject gameObject) {
      if (imageName.StartsWith("ping-")) {
         return PingPanel.self.getPingText();
      } else if (imageName.StartsWith("icon_rank")) {
         return "The <color=red>level</color> the guild has reached.  This increases as the members of the guild gain experience.";
      } else if (imageName.StartsWith("guild_emblem")) {
         return "The <color=red>emblem</color> the guild has chosen.";
      } else if (imageName.StartsWith("coins-")) {
         return "Gold coins";
      } else if (imageName.StartsWith("gem")) {
         return "Gems";
      } else if (imageName.StartsWith("trash")) {
         return "Trashes the selected item.";
      } else if (imageName.StartsWith("gender_male")) {
         return "Male";
      } else if (imageName.StartsWith("gender_female")) {
         return "Female";
      }

      // Bottom buttons
      if (gameObject.name == "Character Button") {
         return "Character Info <color=green>[C]</color>";
      } else if (imageName.StartsWith("btn_abilities")) {
         return "Abilities <color=green>[U]</color>";
      } else if (imageName.StartsWith("btn_guild")) {
         return "Guild Info <color=green>[G]</color>";
      } else if (imageName.StartsWith("btn_inventory")) {
         return "Inventory <color=green>[I]</color>";
      } else if (imageName.StartsWith("btn_leader")) {
         return "Leader Boards <color=green>[B]</color>";
      } else if (imageName.StartsWith("btn_map")) {
         return "Map <color=green>[M]</color>";
      } else if (imageName.StartsWith("btn_options")) {
         return "Options <color=green>[O]</color>";
      } else if (imageName.StartsWith("btn_ship")) {
         return "Ship List <color=green>[L]</color>";
      } else if (imageName.StartsWith("btn_store")) {
         return "Gem Store <color=green>[E]</color>";
      } else if (imageName.StartsWith("btn_trade")) {
         return "Trade History <color=green>[T]</color>";
      } else if (imageName.StartsWith("btn_friends")) {
         return "Friend List <color=green>[F]</color>";
      } else if (imageName.StartsWith("btn_mail")) {
         return "Mail <color=green>[K]</color>";
      } else if (imageName.StartsWith("btn_team")) {
         return "Team Combat <color=green>[K]</color>";
      } else if (imageName.StartsWith("btn_customize_map")) {
         return "Customize Map <color=green>[NULL]</color>";
      }

      // Specialties
      if (imageName.StartsWith("specialty_")) {
         string[] split = imageName.Split('_');
         Specialty.Type specialty = (Specialty.Type) System.Enum.Parse(typeof(Specialty.Type), split[1], true);
         return Specialty.getDescription(specialty);
      }

      // Return an appropriate tooltip description text for the specified image name
      switch (imageName) {
         case "lvl_shield":
            return "The <color=red>level</color> this character has reached.";

         case "icon_found":
            return "The date that the guild was <color=red>created</color>.";

         case "icon_flagship":
            return "Assigns your <color=red>flagship</color>.  This is the default ship that will be used when you leave town.";

         case "class_fighter":
            return Class.getDescription(Class.Type.Fighter);
         case "class_healer":
            return Class.getDescription(Class.Type.Healer);
         case "class_mystic":
            return Class.getDescription(Class.Type.Mystic);
         case "class_marksman":
            return Class.getDescription(Class.Type.Marksman);

         case "ship_damage":
            return "How much <color=red>damage</color> is done by the cannons on this ship.";
         case "ship_range":
            return "The maximum <color=red>range</color> at which this ship can fire.";
         case "ship_health":
            return "How <color=red>durable</color> the ship's hull is. If this reaches 0, the ship sinks!";
         case "ship_supplies":
            return "How many <color=red>supplies</color> the ship can carry to feed the crew during long voyages.";
         case "ship_cargo":
            return "How much <color=red>cargo</color> the ship can carry to sell at the market.";
         case "ship_speed":
            return "The <color=red>speed</color> at which the ship moves while sailing.  The higher the number, the better.  Large warships tend to move slower.";
         case "ship_sailors":
            return "The number of <color=red>sailors</color> that it takes to run this ship.  The larger the crew, the more supplies it takes to feed them during voyages.  " +
               "Large warships generally require large crews, but smaller ships with small crews are better for sailing long voyages.";

         case "icon_strength":
            return "Your <color=red>strength</color> determines how much damage you do with melee weapons like swords.  Important for <color=red>Fighters</color>.";
         case "icon_precision":
            return "Your <color=red>precision</color> determines how much damage you do with ranged weapons like pistols and rifles.  Important for <color=red>Marksmen</color>.";
         case "icon_intelligence":
            return "Your <color=red>intelligence</color> allows you to do more damage with magic attacks.  Important for <color=red>Mystics</color>.";
         case "icon_spirit":
            return "Your <color=red>spirit</color> allows you to be more effective with healing and support abilities.  Important for <color=red>Healers</color>.";
         case "icon_vitality":
            return "Your <color=red>vitality</color> determines how much damage you can take before being knocked out.";
         case "icon_luck":
            return "Your <color=red>luck</color> increases the chances of finding valuable items after battles.  " +
               "The entire team's luck is added together, so everyone benefits equally.  " +
               "Some items only have a chance of appearing when the total combined luck is high enough!";

         case "faction_neutral":
            return "By remaining <color=red>neutral</color>, you are safe from attack by other players.  However, there are many benefits that are only available to players who have chosen a faction.";
         case "faction_builders":
            return "The <color=red>builders</color> are focused on the construction of new towns and shops.";
         case "faction_cartographers":
            return "The <color=red>cartographers</color> are attempting to map out the entire world, which requires long journeys to unexplored territories.";
         case "faction_merchants":
            return "The <color=red>merchants</color> are interested in making as much profit as quickly as they can.  Their focus is primarily on selling cargo to the highest bidder.";
         case "faction_naturalists":
            return "The <color=red>naturalists</color> are focused on preserving and cultivating the plants and wildlife that inhabit the land.";
         case "faction_pillagers":
            return "The <color=red>pillagers</color> endlessly hunt for undiscovered treasure sites, fighting anyone and anything that gets in their way.";
         case "faction_pirates":
            return "The <color=red>skull and bones</color> submit to no authority and will attack anyone if they can get away with it.  Ruthless and cut-throat, they are not to be trusted.";
         case "faction_privateers":
            return "The noble <color=red>privateers</color> hunt down the thieves and scoundrels who prey on the weak.";

         default:
            return "";
      }
   }

   #region Private Variables

   #endregion
}
