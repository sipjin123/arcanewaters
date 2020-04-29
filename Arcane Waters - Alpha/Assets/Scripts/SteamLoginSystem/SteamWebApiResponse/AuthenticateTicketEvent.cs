using System;
using UnityEngine.Events;

namespace SteamLoginSystem
{
   [Serializable]
   public class AuthenticateTicketEvent : UnityEvent<AuthenticateTicketResponse>
   {
   }
}