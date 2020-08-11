using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialData3 : MonoBehaviour
{
   #region Public Variables

   // The tutorials and, for each step, the npc speech and completion triggers
   public static List<Tutorial3> tutorials = new List<Tutorial3>() {
      new Tutorial3("Introduction", "Introduction",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.Manual, "Welcome to Arcane Waters! Here's a tutorial to help you get started. Click on the right arrow to continue."),
         new TutorialStep3(TutorialTrigger.ExpandTutorialPanel, "You can choose a subject by expanding this panel. Click on the arrow pointing up."),
         new TutorialStep3(TutorialTrigger.Manual, "Either click on a subject that you wish to learn, or start the next by clicking on the right arrow."),
      }),

      //new Tutorial3("BuyWeapon", "Buy a weapon at the weapon shop",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.TalkShopOwner, "First, let's get equipped. Enter an item shop and talk to the owner. There is a shop in the starting town, at the north west corner."),
      //   new TutorialStep3(TutorialTrigger.BuyWeapon, "Pick any weapon and buy it."),
      //   new TutorialStep3(TutorialTrigger.OpenInventory, "Great! Let's have a look at your new weapon. Open the inventory panel by clicking the inventory button in the bottom bar"),
      //   new TutorialStep3(TutorialTrigger.EquipWeapon, "Equip your new weapon by double clicking it, dragging and dropping it over your character or right clicking it and selecting the corresponding action."),
      //   new TutorialStep3(TutorialTrigger.Manual, "Well done! You can now try your weapon in a test battle! Click on the right arrow to start the next tutorial."),
      //}),

      //new Tutorial3("TestMeleeBattle", "Do a test melee battle against an NPC in town",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),

      //new Tutorial3("PlantCrops", "Plant, water, and harvest some crops",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),

      //new Tutorial3("PickHouseLayout", "Pick your house layout",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),

      new Tutorial3("SailShip", "Sail your ship",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.SpawnInSeaArea, "To sail your ship, go to the docks. In the starting town, they are located south."),
         new TutorialStep3(TutorialTrigger.Manual, "Use W to move forward and A or D to turn. Beware the pirates!"),
         new TutorialStep3(TutorialTrigger.ShipSpeedUp, "You can boost your movement by holding the SHIFT key. Try it now!"),
         new TutorialStep3(TutorialTrigger.Manual, "The speed-up only last a few seconds before needing to recharge."),
         new TutorialStep3(TutorialTrigger.Manual, "Well done! You are ready to fight some pirates! Click on the right arrow to start the next tutorial."),
      }),

      new Tutorial3("FightPirateShip", "Fight a pirate ship",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.Manual, "Pirates are plenty in Arcane Waters. Before fighting them, let's have a look at your ship combat controls."),
         new TutorialStep3(TutorialTrigger.FireShipCannon, "You can fire your ship cannons by holding RIGHT CLICK and releasing over your target. Fire now!"),
         new TutorialStep3(TutorialTrigger.Manual, "The closer your target, the more damage you will do. This is represented by the color of the trajectory."),
         new TutorialStep3(TutorialTrigger.DefeatPirateShip, "You can usually find a pirate ship east of the starting town. Time to challenge it!"),
         new TutorialStep3(TutorialTrigger.Manual, "Congratulations on your first sea battle victory! There is more to know about sea maps. Click on the right arrow to start the next tutorial."),
      }),

      //new Tutorial3("EnterTreasureSite", "Enter a treasure site",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),

      //new Tutorial3("JoinPvPVoyage", "Join a PvP voyage",
      //new List<TutorialStep3>() {
      //   new TutorialStep3(TutorialTrigger.Manual, "This tutorial will be available soon!"),
      //}),

      new Tutorial3("EndNotice", "Explore the world",
      new List<TutorialStep3>() {
         new TutorialStep3(TutorialTrigger.None, "Congratulations! You have completed all the tutorials. You are now ready to freely roam the world!"),
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
};