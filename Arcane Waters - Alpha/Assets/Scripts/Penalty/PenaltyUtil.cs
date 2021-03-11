using System.Collections.Generic;

public class PenaltyUtil {

   public static List<int> getPenaltiesList (PenaltyType penaltyType) {
      List<int> penaltyTypes = new List<int>();

      if (penaltyType == PenaltyType.Ban) {
         penaltyTypes.Add((int) penaltyType);
      } else if (penaltyType == PenaltyType.Mute || penaltyType == PenaltyType.StealthMute) {
         penaltyTypes.Add((int) PenaltyType.Mute);
         penaltyTypes.Add((int) PenaltyType.StealthMute);
      }

      return penaltyTypes;
   }

}
