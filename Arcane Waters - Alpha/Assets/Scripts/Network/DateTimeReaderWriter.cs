using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;

public static class DateTimeReaderWriter {
   public static void writeDateTime (this NetworkWriter writer, DateTime dateTime) {
      writer.WriteInt64(dateTime.Ticks);
   }

   public static DateTime ReadDateTime (this NetworkReader reader) {
      return new DateTime(reader.ReadInt64());
   }
}
