﻿using Boc.Commands;
using System;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain.Events;
using AccountState = Boc.Chapter13.Domain.AccountState;
using Boc.Chapter13.Domain;
using Microsoft.AspNetCore.Mvc;
using Boc.Domain;

namespace Boc.Chapter13
{
   namespace Unsafe
   {
      public class Chapter10_Transfers_NoValidation : ControllerBase
      {
         Func<Guid, AccountState> getAccount;
         Action<Event> saveAndPublish;

         public IActionResult MakeTransfer([FromBody] MakeTransfer cmd)
         {
            var account = getAccount(cmd.DebitedAccountId);

            // performs the transfer
            var (evt, newState) = account.Debit(cmd);

            saveAndPublish(evt);

            // returns information to the user about the new state
            return Ok(new { Balance = newState.Balance });
         }
      }

      // unsafe version
      public static class Account
      {
         // handle commands

         public static (Event Event, AccountState NewState) Debit
            (this AccountState @this, MakeTransfer transfer)
         {
            var evt = transfer.ToEvent();
            var newState = @this.Apply(evt);

            return (evt, newState);
         }

         // apply events

         public static AccountState Create(CreatedAccount evt)
            => new AccountState
               (
                  Currency: evt.Currency,
                  Status: AccountStatus.Active
               );

         public static AccountState Apply(this AccountState acc, Event evt)
            => evt switch
            {
               (DepositedCash e) => acc with { Balance = acc.Balance + e.Amount },
               (DebitedTransfer e) => acc with { Balance = acc.Balance - e.DebitedAmount },
               (FrozeAccount e) => acc with { Status = AccountStatus.Frozen },
               _ => throw new InvalidOperationException()
            };
      }
   }

   namespace WithValidation
   {
      public class Chapter10_Transfers_WithValidation : ControllerBase
      {
         Func<MakeTransfer, Validation<MakeTransfer>> validate;
         Func<Guid, AccountState> getAccount;
         Action<Event> saveAndPublish;

         public IActionResult MakeTransfer([FromBody] MakeTransfer cmd)
            => validate(cmd)
               .Bind(t => getAccount(t.DebitedAccountId).Debit(t))
               .Do(result => saveAndPublish(result.Item1))
               .Match<IActionResult>(
                  Invalid: errs => BadRequest(new { Errors = errs }),
                  Valid: result => Ok(new { Balance = result.Item2.Balance }));         
      }
   }
}
