using System;
using UnityEngine.Events;

namespace SteamLoginSystem {
   [Serializable]
   public class AppOwnershipEvent : UnityEvent<AppOwnerShipResponse>
   {
   }
}