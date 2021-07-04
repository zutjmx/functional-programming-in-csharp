﻿using System;
using static System.Console;
using System.Net.Http;
using System.Text.Json;

using LaYumba.Functional;
using static LaYumba.Functional.F;

using Rates = System.Collections.Immutable.ImmutableDictionary<string, decimal>;
using CurrencyCode = Boc.Domain.CurrencyCode;

namespace Examples.Chapter15
{

   public static class CurrencyLookup_Stateless
   {
      public static void Run()
      {
         WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
         for (string input; (input = ReadLine().ToUpper()) != "Q";)
            WriteLine(FxApi.GetRate(input));
      }
   }

   public class CurrencyLookup_StatefulUnsafe
   {
      public static void Run()
      {
         WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
         MainRec(Rates.Empty);
      }

      static void MainRec(Rates cache)
      {
         var input = ReadLine().ToUpper();
         if (input == "Q") return;

         var (rate, newState) = GetRate(input, cache);
         WriteLine(rate);
         MainRec(newState); // recursively calls itself with the new state
      }

      // non-recursive version
      public static void MainNonRec()
      {
         WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
         var state = Rates.Empty;

         for (string input; (input = ReadLine().ToUpper()) != "Q";)
         {
            var (rate, newState) = GetRate(input, state);
            state = newState;
            WriteLine(rate);
         }
      }

      static (decimal, Rates) GetRate(string ccyPair, Rates cache)
      {
         if (cache.ContainsKey(ccyPair))
            return (cache[ccyPair], cache);

         var rate = FxApi.GetRate(ccyPair);
         return (rate, cache.Add(ccyPair, rate));
      }
   }

   public class CurrencyLookup_Testable
   {
      public static void _main()
      {
         WriteLine("Enter a currency pair like 'EURUSD', or 'q' to quit");
         MainRec(Rates.Empty);
      }

      static void MainRec(Rates cache)
      {
         var input = ReadLine().ToUpper();
         if (input == "Q") return;

         var (rate, newState) = GetRate(FxApi.GetRate, input, cache);
         WriteLine(rate);
         MainRec(newState); // recursively calls itself with the new state
      }

      static (decimal, Rates) GetRate
         (Func<string, decimal> getRate, string ccyPair, Rates cache)
      {
         if (cache.ContainsKey(ccyPair))
            return (cache[ccyPair], cache);

         var rate = getRate(ccyPair);
         return (rate, cache.Add(ccyPair, rate));
      }
   }

   public class CurrencyLookup_StatefulSafe
   {
      public static void Run()
         => MainRec("Enter a currency pair like 'EURUSD', or 'q' to quit"
            , Rates.Empty);

      static void MainRec(string message, Rates cache)
      {
         WriteLine(message);
         var input = ReadLine().ToUpper();
         if (input == "Q") return;

         GetRate(pair => () => FxApi.GetRate(pair), input, cache).Run().Match(
            ex => MainRec($"Error: {ex.Message}", cache),
            result => MainRec(result.Quote.ToString(), result.NewState));
      }

      static Try<(decimal Quote, Rates NewState)> GetRate
        (Func<string, Try<decimal>> getRate, string ccyPair, Rates cache)
      {
         if (cache.ContainsKey(ccyPair))
            return Try(() => (cache[ccyPair], cache));

         else return from rate in getRate(ccyPair)
            select (rate, cache.Add(ccyPair, rate));
      }
   }

   static class FxApi
   {
      record Response
      (
         CurrencyCode CurrencyCode,
         Rates Rates
      );

      public static decimal GetRate(string ccyPair)
      {
         WriteLine($"fetching rate...");

         var (baseCcy, quoteCcy) = ccyPair.SplitAt(3);
         var uri = $"https://api.ratesapi.io/api/latest?base={baseCcy}";
         var request = new HttpClient().GetStringAsync(uri);

         var opts = new JsonSerializerOptions
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
         var response = JsonSerializer.Deserialize<Response>(request.Result, opts);

         return response.Rates[quoteCcy];
      }

      public static Try<decimal> TryGetRate(string ccyPair)
         => () => GetRate(ccyPair);
   }
}
