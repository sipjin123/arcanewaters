public enum TradeState : byte
{
   None = 0,
   RequestedTrade = 1,
   ChoosingItems = 2,
   ConfirmedChosenItems = 3,
   ReviewingTrade = 4,
   AcceptedTrade = 5
}
