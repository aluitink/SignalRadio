using Microsoft.AspNetCore.Mvc;

using SignalRadio.Database.EF;

namespace SignalRadio.Web.Api.Controllers
{
    public class SignalRadioControllerBase: ControllerBase
    {
        protected SignalRadioDbContext DbContext { get; set; }
        protected ILogger Logger { get; set; }

        public  SignalRadioControllerBase(SignalRadioDbContext dbContext, ILogger logger)
        {
            DbContext = dbContext;
            Logger = logger;
        }
    }
}