using Microsoft.AspNetCore.Mvc;
using Palantiri.Shared.Amazon.S3;

namespace Palantiri.Rest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3Controller(ILoggerFactory factory, IWriteRepository publisher, IReadRepository consumer) : ControllerBase
    {
        private readonly ILogger _logger = factory.CreateLogger<S3Controller>();
        private readonly IWriteRepository _publisher = publisher;
        private readonly IReadRepository _consumer = consumer;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFileAsync([FromRoute] string id)
        {
            try
            {

                _logger.LogInformation("Init get...");
                var result = await _consumer.ReadAsync(id);
                return File(result.Item1, result.Item2, result.Item3);

            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

        public record Form(IFormFile File);

        [HttpPost]
        public async Task<ActionResult<string>> Publish([FromForm] Form command)
        {
            try
            {

                _logger.LogInformation("Init post...");
                

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await command.File.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                var stream = new MemoryStream(fileBytes);
                string fileName = Guid.NewGuid().ToString() + ".txt";

                //string destination = Path.Combine( "trace", fileName);

                await _publisher.WriteAsync(stream, command.File.ContentType, fileName);

                return Ok(fileName);
            }
            catch (Exception)
            {
                return BadRequest();
            }

        }

        [HttpDelete]
        public async Task<ActionResult<string>> Delete(string path)
        {
            try
            {

                _logger.LogInformation("Init delete...");
                await _publisher.DeleteAsync(path);
                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest();
            }

        }
    }
}