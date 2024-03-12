using Microsoft.AspNetCore.Mvc;
using Palantiri.Rest.Transport;
using Palantiri.Shared.Amazon.SQS;

namespace Palantiri.Rest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SQSController(ILoggerFactory factory, IMessagePublisher publisher, IMessageConsumer consumer) : ControllerBase
    {
        private readonly ILogger _logger = factory.CreateLogger<SQSController>();
        private readonly IMessagePublisher _publisher = publisher;
        private readonly IMessageConsumer _consumer = consumer;

        [HttpPost]
        public async Task<ActionResult<string>> Publish()
        {
            try
            {

                _logger.LogInformation("Init post...");
                var formurlencoded = new Dictionary<string, string>
                {
                    { "Message", "They're Taking the Hobbits to Isengard" },
                    { "echo", "-gard -ard -rd -d" }
                };

                await _publisher.PublishAsync(formurlencoded);

                return Ok(formurlencoded);
            }
            catch (Exception)
            {
                return BadRequest();
            }

        }

        [HttpGet]
        public async Task<ActionResult<string>> Consume()
        {
            try
            {

                _logger.LogInformation("Init consume...");
                var result = await _consumer.ConsumeAsync<SQSMessage>();
                return Ok(result);

            }
            catch (Exception)
            {
                return BadRequest();
            }

        }
    }
}