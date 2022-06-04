using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using SignalRadio.Database.EF;

namespace SignalRadio.Web.Api.Controllers
{
    [ApiController]
    public class WebSocketsController : SignalRadioControllerBase
    {
        public WebSocketsController(SignalRadioDbContext dbContext, ILogger<WebSocketsController> logger)
            : base(dbContext, logger) { }

        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                Logger.LogDebug("WebSocket activated");
                using (var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
                {
                    using (Logger.BeginScope("TrunkRecorderStatusHandler"))
                    {
                        await (new TrunkRecorderStatusHandler(DbContext, Logger))
                            .StartStatusMessageHandlerAsync(HttpContext, webSocket);
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
