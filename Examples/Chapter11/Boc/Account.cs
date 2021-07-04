﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using Boc.Domain;
using NUnit.Framework;

namespace Examples.Chapter11
{
   public record AccountState_Positional
   (
      CurrencyCode Currency,
      AccountStatus Status = AccountStatus.Requested,
      decimal AllowedOverdraft = 0m,
      IEnumerable<Transaction> TransactionHistory = null
   );

   public record Transaction
   (
      decimal Amount,
      string Description,
      DateTime Date
   );

   public record AccountState
   (
      CurrencyCode Currency,
      AccountStatus Status = AccountStatus.Requested,
      decimal AllowedOverdraft = 0m,
      IEnumerable<Transaction> TransactionHistory = null
   )
   {
      // use a read-only property to disallow "updating" the currency of an account
      public CurrencyCode Currency { get; } = Currency;

      // use a property initializer to use an empty list rather than null
      public IEnumerable<Transaction> TransactionHistory { get; init; }
         = TransactionHistory?.ToImmutableList()
            ?? Enumerable.Empty<Transaction>();
   }

   public static class Account
   {
      public static AccountState Create(CurrencyCode ccy)
         => new(ccy);

      public static AccountState Add(this AccountState original, Transaction transaction)
         => original with
         {
            TransactionHistory = original.TransactionHistory.Prepend(transaction)
         };

      public static AccountState Activate(this AccountState original)
         => original with { Status = AccountStatus.Active };

      public static AccountState RedFlag(this AccountState original)
         => original with
         {
            Status = AccountStatus.Frozen,
            AllowedOverdraft = 0m
         };
   }

   public static class Usage
   {
      public static void AccountUsage()
      {
         var onDayOne = Account.Create("USD");
         var onDayTwo = Account.Activate(onDayOne);
         //var onDayTwo = onDayOne.Activate();
         var onDayThree = onDayTwo.Add(new(50, "", DateTime.Now));
      }

      static AccountState OpeningOffer()
         => Account.Create("USD")
            .Activate()
            .Add(new(50, "New customer offer", DateTime.Now));

      [Test]
      public static void WhenInitializedWithNull_ThenHasEmptyList()
         => Assert.AreEqual(0, new AccountState("EUR").TransactionHistory.Count());

      [Test]
      public static void WithOnlyChangesTheSpecifiedFields()
      {
         var original = new AccountState(Currency: "EUR");
         var activated = original.Activate();

         Assert.AreEqual(AccountStatus.Requested, original.Status);
         Assert.AreEqual(AccountStatus.Active, activated.Status);
         Assert.AreEqual(original.Currency, activated.Currency);
      }

      [Test]
      public static void GivenRecordImmutable_WhenMutateList_ThenRecordIsNotMutated()
      {
         var mutable = new List<Transaction>();
         var account = new AccountState
         (
            Currency: "EUR",
            TransactionHistory: mutable
         );
         mutable.Add(new(-1000, "Create trouble", DateTime.Now));

         Assert.AreEqual(1, mutable.Count);
         Assert.AreEqual(0, account.TransactionHistory.Count());
      }
   }
}