using System;
using System.Collections;
using System.Collections.Generic;

public class WeightedItem<T> {
   public double Proportion { get; set; }
   public T Value { get; set; }
}

public static class WeightedItem {
   public static WeightedItem<T> Create<T> (double proportion, T value) {
      return new WeightedItem<T> { Proportion = proportion, Value = value };
   }

   static Random random = new Random();

   public static T ChooseByRandom<T> (
       this IEnumerable<WeightedItem<T>> collection) {
      var rnd = random.NextDouble();
      foreach (var item in collection) {
         if (rnd < item.Proportion)
            return item.Value;
         rnd -= item.Proportion;
      }
      throw new InvalidOperationException(
          "The proportions in the collection do not add up to 1.");
   }
}
