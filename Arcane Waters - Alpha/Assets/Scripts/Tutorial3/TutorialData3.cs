﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialData3 : MonoBehaviour
{
   #region Public Variables

   // The tutorial locations
   public enum Location
   {
      None = 0,
      TutorialTown = 1,
      Cemetery = 2,
      Farm = 3,
      House = 4,
      TutorialSeaMap = 5,
   }

   // The locations and the corresponding area key
   public static Dictionary<Location, string> locationToAreaKey = new Dictionary<Location, string>() {
      { Location.TutorialTown, "Tutorial Town" },
      { Location.Cemetery, "Tutorial Town Cemetery v2" },
      { Location.Farm, "customfarm" },
      { Location.House, "customhouse" },
      { Location.TutorialSeaMap, "Tutorial Sea Map" },
   };

   // The tutorials and, for each step, the npc speech and completion triggers
   public static List<Tutorial3> tutorials = new List<Tutorial3>() {
      new Tutorial3("Introduction", "Introduction",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.Manual, "Welcome to Arcane Waters! These tutorials will help you get started. Click on the right arrow to continue."),
         new TutorialStep3(TutorialTrigger.ExpandTutorialPanel, "You can choose a subject by expanding this panel. Click on the arrow pointing up."),
         new TutorialStep3(TutorialTrigger.Manual, "Either click on a subject that you wish to learn, or start the next topic by clicking on the right arrow."),
      }),

      //new Tutorial3("BuyWeapon", "Buy a weapon at the weapon shop",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.TalkShopOwner, "First, let's get equipped. Enter an item shop and talk to the owner. There is a shop in the starting town, at the north west corner."),
      //   new TutorialStep3(TutorialTrigger.BuyWeapon, "Pick any weapon and buy it."),
      //   new TutorialStep3(TutorialTrigger.OpenInventory, "Great! Let's have a look at your new weapon. Open the inventory panel by clicking the inventory button in the bottom bar"),
      //   new TutorialStep3(TutorialTrigger.EquipWeapon, "Equip your new weapon by double clicking it, dragging and dropping it over your character or right clicking it and selecting the corresponding action."),
      //   new TutorialStep3(TutorialTrigger.Manual, "Well done! You can now try your weapon in a test battle! Click on the right arrow to start the next tutorial."),
      //}),

      new Tutorial3("TestMeleeBattle", "Do a test melee battle",
      new List<TutorialStep3>() {
         new TutorialStep3(Location.TutorialTown, "Let's have a look at melee battles. Go to the starting town to begin!"),
         new TutorialStep3(TutorialTrigger.EquipWeapon, "You should have a sword in your fourth shortcut. Press <#FF8000>4</color> to equip it. You can also search for one in your inventory by pressing <#FF8000>I</color>."),
         new TutorialStep3(Location.Cemetery, "There is an old cemetery to the east of the starting town. Head there now!"),
         new TutorialStep3(TutorialTrigger.EnterBattle, "Many skeletons keep coming back to life in this place. Help the village out by reminding them they should be dead. Move close to one to enter battle!"),
         new TutorialStep3(TutorialTrigger.AttackBattleTarget, "He may look scary, but the skeleton is old and weak. At the top of the screen are your skills. Pick an attack skill and cast it!"),
         new TutorialStep3(TutorialTrigger.EndBattle, "Well done! Now give the skeleton no quarter until it is defeated!"),
         new TutorialStep3(Location.TutorialTown, "Congratulations on your battle victory! The first of many I'm sure. Leave the cemetery to start the next tutorial!"),
      }),

      new Tutorial3("PickFarmLayout", "Pick your farm layout",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.OpenFarmLayoutSelectionPanel, Location.Farm, "Your farm lies north of the starting town. Follow the road to the north to head there now."),
         new TutorialStep3(Location.Farm, "The first time you visit your farm, you will be asked to choose which layout you would like. Take your pick!"),
         new TutorialStep3(TutorialTrigger.OpenHouseLayoutSelectionPanel, Location.House, "Welcome to your farm. Have a look around! When you are ready to continue, enter your house."),
         new TutorialStep3(Location.House, "Time to choose your house layout!"),
         new TutorialStep3(Location.Farm, "Welcome home! Have a look around! Leave the house when you are ready to continue."),
      }),

      new Tutorial3("PlaceFurniture", "Customize your property",
      new List<TutorialStep3>{
         new TutorialStep3(Location.Farm, "Head to your farm. I'll explain everything when you reach it!"),
         new TutorialStep3(Weapon.ActionType.CustomizeMap, "Let me show you how to customize your farm and house. You should have a hammer in your inventory. Equip the hammer to enter the building mode."),
         new TutorialStep3(TutorialTrigger.SelectObject, "We gave you a few items to get you started! Select one you like in the panel on the right."),
         new TutorialStep3(TutorialTrigger.PlaceObject, "Click anywhere in the world to place it. You can also change it's variation with the <#FF8000>Mouse Wheel</color>."),
         new TutorialStep3(TutorialTrigger.MoveObject, "To adjust it's position, click on the object you placed, drag it to a new position and release."),
         new TutorialStep3(TutorialTrigger.DeleteObject, "Now click on the object you placed to select it and press the <#FF8000>Delete</color> key to delete it."),
         new TutorialStep3(TutorialTrigger.UnequipHammer, "Good job! To wrap it up, unequip the hammer in your inventory to exit the building mode."),
      }),

      new Tutorial3("PlantCrops", "Plant, water, and harvest some crops",
      new List<TutorialStep3>() {
         new TutorialStep3(Location.Farm, "Head to your farm. I'll explain everything when you reach it!"),
         new TutorialStep3(Weapon.ActionType.PlantCrop, "Let me show you how to grow crops. You should have a seed bag set to your first shortcut. Press <#FF8000>1</color> to equip it, or find it in your inventory by pressing <#FF8000>I</color>."),
         new TutorialStep3(TutorialTrigger.PlantCrop, "Look around your farm for some patches of dirt. They have holes ready for seeds. Stand next to the holes and press <#FF8000>Right-Click</color> to plant your seeds."),
         new TutorialStep3(TutorialTrigger.PlantCrop, "You're a farmer already! Now, let's plant a few more.", 4),
         new TutorialStep3(TutorialTrigger.CropGrewToMaxLevel, "Press <#FF8000>2</color> to equip the watering pot and <#FF8000>Right-Click</color> to use it on a plant. Your crops will need to be watered a few times before they are ready for harvest.", 5),
         new TutorialStep3(TutorialTrigger.HarvestCrop, "The crops are ready to be harvested! Press <#FF8000>3</color> to equip the pitchfork and collect your vegetables.", 5),
         new TutorialStep3(Location.TutorialTown, "Now that's good sweaty work! All your harvests can be sold at the crop merchant. You will find merchants in most villages."),
      }),

      new Tutorial3("SailShip", "Sail your ship",
      new List<TutorialStep3>() {
         new TutorialStep3(Location.TutorialSeaMap, "Time to sail! In the starting town, head to the docks located south."),
         new TutorialStep3(TutorialTrigger.TurnShipLeft, "Alright! First, let's move your ship around. Press <#FF8000>[primary]</color> or <#FF8000>[secondary]</color> to turn left."),
         new TutorialStep3(TutorialTrigger.TurnShipRight, "Press <#FF8000>[primary]</color> or <#FF8000>[secondary]</color> to turn right."),
         new TutorialStep3(TutorialTrigger.MoveShipForward, "Finally, press <#FF8000>[primary]</color> or <#FF8000>[secondary]</color> to move forward."),
         new TutorialStep3(TutorialTrigger.ShipSpeedUp, "You can boost your movement by holding the <#FF8000>Shift</color> key. Try it now!"),
         new TutorialStep3(TutorialTrigger.SelectPirateShip, "Let's have a look at your combat controls. First, sail west and look for a pirate ship. Careful, don't get too close! When you see one, click on it or press <#FF8000>Tab</color> to auto-target."),
         new TutorialStep3(TutorialTrigger.FireShipCannon, "Try firing your cannons by pressing <#FF8000>Space</color>. You will automatically aim towards the selected ship. Fire in the hole!"),
         new TutorialStep3(TutorialTrigger.DefeatPirateShip, "You are ready to challenge the pirates. Get in range and show them what you got!"),
         new TutorialStep3(Location.TutorialTown, "Congratulations on your first sea battle victory! When you are ready to continue, return to the town."),
      }),

      //new Tutorial3("EnterTreasureSite", "Enter a treasure site",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),
      
      new Tutorial3("JoinVoyage", "Join a voyage",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.OpenVoyagePanel, "Great adventures await you at sea! Near most docks in a town, you will find a voyage signboard. Look for one and click on it!"),
         new TutorialStep3(TutorialTrigger.SpawnInVoyage, "Voyages are set in distant, unexplored seas. By selecting a map, you will be automatically matched with other players. Choose a PvE map and join the expedition!"),
         new TutorialStep3(TutorialTrigger.Manual, "The group composition is displayed on the left of your screen. You can interact with the members of your group through the chat."),
         new TutorialStep3(TutorialTrigger.Manual, "You can leave your group and the map at any time by pressing the X button above the group members. But avast! Don't do it yet!"),
         new TutorialStep3(TutorialTrigger.Manual, "The goal of this voyage is to find and capture treasure sites before rival groups do."),
         new TutorialStep3(TutorialTrigger.EnterTreasureSiteRange, "Treasure sites show up as chests in the minimap. Search for one now! Beware the sea monsters and pirates, though."),
         new TutorialStep3(TutorialTrigger.Manual, "There, you found one! We won't go further in this tutorial, but I can tell you that once captured, a treasure site can be explored in search of riches!"),
         new TutorialStep3(TutorialTrigger.LeaveVoyageGroup, "This is all for voyages. To continue with other tutorials, leave the voyage by clicking on the X button above the group members."),
      }),



      // ----------------------------------------
      // This must always be the last tutorial!
      // ----------------------------------------
      new Tutorial3("EndNotice", "Explore the world",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.None, "Congratulations! You have completed all the tutorials. Now it is time to find your sea legs, unfurl your sails and head out into the world!"),
      }),
   };

   #endregion


   #region Private Variables

   #endregion
}

public enum TutorialTrigger
{
   None = 0,
   Manual = 1,
   TalkShopOwner = 2,
   BuyWeapon = 3,
   ExpandTutorialPanel = 5,
   OpenInventory = 6,
   EquipWeapon = 7,
   ShipSpeedUp = 8,
   FireShipCannon = 9,
   DefeatPirateShip = 10,
   PlantCrop = 12,
   CropGrewToMaxLevel = 13,
   HarvestCrop = 14,
   OpenFarmLayoutSelectionPanel = 15,
   OpenHouseLayoutSelectionPanel = 17,
   OpenVoyagePanel = 19,
   SpawnInVoyage = 20,
   EnterBattle = 22,
   AttackBattleTarget = 24,
   EndBattle = 25,
   EnterTreasureSiteRange = 26,
   PlaceObject = 28,
   DeleteObject = 29,
   UnequipHammer = 30,
   MoveShipForward = 32,
   TurnShipLeft = 33,
   TurnShipRight = 34,
   LeaveVoyageGroup = 35,
   SelectPirateShip = 36,
   MoveObject = 37,
   SelectObject = 38
};