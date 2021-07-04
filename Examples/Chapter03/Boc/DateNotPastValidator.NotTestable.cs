using Boc.Commands;
using Boc.Services;

using System;
using NUnit.Framework;

namespace Examples.Chapter03.Boc.NotTestable
{
   public class DateNotPastValidator : IValidator<MakeTransfer>
   {
      public bool IsValid(MakeTransfer request)
         => DateTime.UtcNow.Date <= request.Date.Date;
   }

   public class DateNotPastValidator_Impure_Test
   {
      [Test]
      [Ignore("Demonstrates a test that is not repeatable")]
      public void WhenTransferDateIsFuture_ThenValidatorPasses()
      {
         var sut = new DateNotPastValidator();
         var transfer = MakeTransfer.Dummy with
         {
            Date = new DateTime(2021, 3, 12)
         };

         var actual = sut.IsValid(transfer);
         Assert.AreEqual(true, actual);
      }
   }
}