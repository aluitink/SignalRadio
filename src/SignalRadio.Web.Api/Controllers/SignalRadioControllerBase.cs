using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRadio.Database.EF;

namespace SignalRadio.Web.Api.Controllers
{
    public class SignalRadioControllerBase: ControllerBase
    {
        protected ILogger Logger { get; set; }
        protected ISignalRadioDbContext DbContext { get; set; }

        public SignalRadioControllerBase(SignalRadioDbContext dbContext, ILogger logger)
        {
            if(dbContext.Database.EnsureCreated())
                logger.LogInformation("Database exists.");

            DbContext = dbContext;
            Logger = logger;
        }
    }

}
