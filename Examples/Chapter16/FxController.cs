﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LaYumba.Functional;
using Boc.Domain;

namespace Examples.Chapter16
{
   public class FxController : ControllerBase
   {
      //$ curl http://localhost:5000/convert/1000/USD/to/EUR -s
      //896.9000

      //$ curl http://localhost:5000/convert/1000/USD/to/JPY -s
      //103089.0000

      //$ curl http://localhost:5000/convert/1000/XXX/to/XXX -s
      //{"message":"An unexpected error has occurred"}

      [HttpGet("convert/{amount}/{from}/to/{to}")]
      public Task<IActionResult> Convert(decimal amount, string from, string to)
         => RatesApi.GetRateAsync(from + to)
            .OrElse(() => CurrencyLayer.GetRateAsync(from + to))
            .Map(rate => amount * rate)
            .Map(
               Faulted: ex => StatusCode(500, Errors.UnexpectedError),
               Completed: result => Ok(result) as IActionResult);
   }
}
