using System;
using UnityEngine.Events;

namespace SteamLoginSystem
{
   [Serializable]
   public class GetAuthTicketEvent : UnityEvent<GetAuthTicketResponse> {
   }

   [Serializable]
   public class GetAuthTicketResponse {
      // Auth Ticket Data
      public byte[] m_Ticket;

      // Auth Ticket Size
      public uint m_pcbTicket;
   }
}