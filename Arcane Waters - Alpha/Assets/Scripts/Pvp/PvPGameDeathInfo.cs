using System.Collections.Generic;
using System.Linq;

public class PvPGameDeathInfo
{
   #region Public Variables

   // The set of death times
   public List<float> deathTimes = new List<float>();

   #endregion

   public int getLastDeaths(float now, float secondsBefore) {
      if (deathTimes.Count == 0) {
         return 0;
      }

      return deathTimes.Count(t => now - t < secondsBefore);
   }
}
