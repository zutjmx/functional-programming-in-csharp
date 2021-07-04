﻿using System;
namespace Examples.Chapter01
{
   record Product(string Name, decimal Price, bool IsFood);

   record Order(Product Product, int Quantity)
   {
      public decimal NetPrice => Product.Price * Quantity;
   }

   record Address(string Country);
   record UsAddress(string State) : Address("us");

   public static class VatStrategy
   {
      static decimal Vat(Address address, Order order)
         => address switch
         {
            UsAddress(var state) => Vat(RateByState(state), order),
            Address("de") => DeVat(order),
            Address(var country) => Vat(RateByCountry(country), order),
         };

      static decimal RateByCountry(string country)
         => country switch
         {
            "it" => 0.22m,
            "jp" => 0.08m,
            _ => throw new ArgumentException($"Missing rate for {country}")
         };

      static decimal Vat(decimal rate, Order order)
         => order.NetPrice * rate;

      static decimal RateByState(string state)
         => state switch
         {
            "ca" => 0.1m,
            "ma" => 0.0625m,
            "ny" => 0.085m,
            _ => throw new ArgumentException($"Missing rate for {state}")
         };

      static decimal DeVat(Order order)
         => order.NetPrice * (order.Product.IsFood ? 0.08m : 0.2m);
   }
}
