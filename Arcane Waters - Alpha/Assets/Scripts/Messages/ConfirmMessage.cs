using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ConfirmMessage : NetworkMessage
{
   #region Public Variables

   // The Type of confirmation
   public enum Type {
      SeaWarp = 1, BoughtCargo = 2, BugReport = 3, AddGold = 4, General = 5,
      ConfirmDeleteItem = 6, ContainerOpened = 7, StoreItemBought = 8, UsedHairDye = 9,
      DeletedUser = 10, UsedShipSkin = 11, UsedHaircut = 12, CreatedGuild = 13,
      ShipBought = 14, FriendshipInvitationSent = 15, FriendshipInvitationAccepted = 16,
      FriendshipDeleted = 17, MailSent = 18, MailDeleted = 19, ItemsAddedToInventory = 20,
      ModifiedOwnAuction = 21, BidOnAuction = 22, EditGuildRanks = 23, GuildActionLocal = 24,
      GuildActionGlobal = 25, GuildActionUpdate = 26, CorrectClientVersion = 27, SoldAuctionItem = 28,
      ReturnAuctionItem = 29, UsedConsumable = 30, UsedArmorDye = 31, UsedHatDye = 32, UsedWeaponDye = 33,
      ItemSoulBound = 34, GeneralPopup = 35, RestoredUser = 36, ModifiedAuction = 37
   }

   // The Type of confirmation
   public Type confirmType;

   // The time of the action on the server
   public long timestamp;

   // A custom confirmation message that can be provided
   public string customMessage;

   #endregion

   public ConfirmMessage () { }

   public ConfirmMessage (Type confirmType, long timestamp, string customMessage = "") {
      this.confirmType = confirmType;
      this.timestamp = timestamp;
      this.customMessage = customMessage;
   }
}