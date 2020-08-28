using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialData3 : MonoBehaviour
{
   #region Public Variables

   // The area key of the tutorial cemetery
   public static string tutorialCemeteryAreaKey = "Tutorial Town Cemetery v2";

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
         new TutorialStep3(TutorialTrigger.EquipWeapon, "You should have a sword in your fourth shortcut. Press 4 to equip it. You can also search for one in your inventory by pressing I."),
         new TutorialStep3(TutorialTrigger.SpawnInTutorialTownCemetery, "There is an old cemetery to the east of the starting town. Head there now!"),
         new TutorialStep3(TutorialTrigger.EnterBattle, "Many skeletons keep coming back to life in this place. Help the villagers out by reminding them they should be dead. Move close to one to enter battle!"),
         new TutorialStep3(TutorialTrigger.SelectBattleEnemy, "He may look scary, but the skeleton is old and weak. Select it by clicking on it."),
         new TutorialStep3(TutorialTrigger.AttackBattleTarget, "At the top of the screen are your skills. Pick an attack skill and cast it!"),
         new TutorialStep3(TutorialTrigger.EndBattle, "Well done! Now give the skeleton no quarter until it is defeated!"),
         new TutorialStep3(TutorialTrigger.Manual, "Congratulations on your battle victory! The first of many I'm sure. To start the next tutorial, click on the right arrow."),
      }),

      new Tutorial3("PickFarmLayout", "Pick your farm layout",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.OpenFarmLayoutSelectionPanel, "Your farm lies north of the starting town. Follow the road to the north to head there now."),
         new TutorialStep3(TutorialTrigger.SpawnInFarm, "The first time you visit your farm, you will be asked to choose which layout you would like. Take your pick!"),
         new TutorialStep3(TutorialTrigger.Manual, "Welcome to your farm. Have a look around!"),
      }),

      new Tutorial3("PickHouseLayout", "Pick your house layout",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.OpenHouseLayoutSelectionPanel, "To choose the layout of your house, enter the house located on your farm."),
         new TutorialStep3(TutorialTrigger.SpawnInHouse, "Time to choose your house layout!"),
         new TutorialStep3(TutorialTrigger.Manual, "Welcome home! Have a look around!"),
      }),

      new Tutorial3("PlantCrops", "Plant, water, and harvest some crops",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.EquipSeedBag, "Let me show you how to grow crops. You should have a seed bag set to your first shortcut. Press 1 to equip it, or find it in your inventory by pressing I."),
         new TutorialStep3(TutorialTrigger.PlantCrop, "Now, look around your farm for some patches of dirt. They have holes ready for seeds. Stand next to the holes and press right-click to plant your seeds."),
         new TutorialStep3(TutorialTrigger.PlantCrop, "You're a farmer already! Now, let's plant a few more.", 4),
         new TutorialStep3(TutorialTrigger.CropGrewToMaxLevel, "Press 2 to equip the watering pot and right-click to use it on a plant. Your crops will need to be watered a few times before they are ready for harvest.", 5),
         new TutorialStep3(TutorialTrigger.HarvestCrop, "The crops are ready to be harvested! Press 3 to equip the pitchfork and collect your vegetables.", 5),
         new TutorialStep3(TutorialTrigger.Manual, "Now that's good sweaty work! All your harvests can be sold at the crop merchant. You will find merchants in most villages."),
         new TutorialStep3(TutorialTrigger.Manual, "That's all there is to growing crops! To start the next tutorial, click on the right arrow."),
      }),

      new Tutorial3("SailShip", "Sail your ship",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.SpawnInSeaArea, "To sail your ship, go to the docks. In the starting town, they are located south."),
         new TutorialStep3(TutorialTrigger.Manual, "Use W to move forward and A or D to turn. But beware pirates!"),
         new TutorialStep3(TutorialTrigger.ShipSpeedUp, "You can boost your movement by holding the SHIFT key. Try it now!"),
         new TutorialStep3(TutorialTrigger.Manual, "The speed-up only lasts a few seconds before needing to recharge."),
         new TutorialStep3(TutorialTrigger.Manual, "Well done, Captain! You are now ready to fight some pirates! Click on the right arrow to start the next tutorial."),
      }),

      new Tutorial3("FightPirateShip", "Fight a pirate ship",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.Manual, "Pirates are plenty in Arcane Waters. Before fighting them, let's have a look at your ship combat controls."),
         new TutorialStep3(TutorialTrigger.FireShipCannon, "You can fire your ship cannons by holding RIGHT-CLICK and releasing over your target. Fire in the hole!"),
         new TutorialStep3(TutorialTrigger.Manual, "The closer your target, the more damage you will do. This is represented by the color of the trajectory."),
         new TutorialStep3(TutorialTrigger.DefeatPirateShip, "You can usually find a pirate ship to the east of the starting town. Time to challenge it!"),
         new TutorialStep3(TutorialTrigger.Manual, "Congratulations on your first sea battle victory! But, there is more to know about sea maps. Click on the right arrow to start the next tutorial."),
      }),

      //new Tutorial3("EnterTreasureSite", "Enter a treasure site",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),
      
      new Tutorial3("JoinVoyage", "Join a voyage",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.OpenVoyagePanel, "Great adventures await you at sea! Near most docks in a town, you will find a voyage signboard. Look for one and click on it!"),
         new TutorialStep3(TutorialTrigger.Manual, "Voyages are set in distant, unexplored seas. These destinations regularly change."),
         new TutorialStep3(TutorialTrigger.SpawnInVoyage, "By selecting a map, you will be automatically matched with other players. Choose a PvE map and join the expedition!"),
         new TutorialStep3(TutorialTrigger.Manual, "The group composition is displayed on the left of your screen. You can interact with the members of your group through the chat."),
         new TutorialStep3(TutorialTrigger.Manual, "You can leave your group and the map at any time by pressing the X button above the group members. But avast! Don't do it yet!"),
         new TutorialStep3(TutorialTrigger.Manual, "The goal of this voyage is to find and capture treasure sites before rival groups do."),
         new TutorialStep3(TutorialTrigger.EnterTreasureSiteRange, "Treasure sites show up as chests in the minimap. Search for one now! Beware the sea monsters and pirates, though."),
         new TutorialStep3(TutorialTrigger.Manual, "There, you found one! We won't go further in this tutorial, but I can tell you that once captured, a treasure site can be explored in search of riches!"),
         new TutorialStep3(TutorialTrigger.Manual, "This is all for voyages. To continue with other tutorials, click on the right arrow."),
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
   SpawnInSeaArea = 4,
   ExpandTutorialPanel = 5,
   OpenInventory = 6,
   EquipWeapon = 7,
   ShipSpeedUp = 8,
   FireShipCannon = 9,
   DefeatPirateShip = 10,
   EquipSeedBag = 11,
   PlantCrop = 12,
   CropGrewToMaxLevel = 13,
   HarvestCrop = 14,
   OpenFarmLayoutSelectionPanel = 15,
   SpawnInFarm = 16,
   OpenHouseLayoutSelectionPanel = 17,
   SpawnInHouse = 18,
   OpenVoyagePanel = 19,
   SpawnInVoyage = 20,
   SpawnInTutorialTownCemetery = 21,
   EnterBattle = 22,
   SelectBattleEnemy = 23,
   AttackBattleTarget = 24,
   EndBattle = 25,
   EnterTreasureSiteRange = 26,
};