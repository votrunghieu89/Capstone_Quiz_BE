using Capstone.RabbitMQ;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RabbitMQTestController : ControllerBase
    {
        private readonly RabbitMQProducer _producer;

        public RabbitMQTestController(RabbitMQProducer producer)
        {
            _producer = producer;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            try
            {
                await _producer.SendMessageAsync(message);
                return Ok(new { success = true, message = "Message sent to RabbitMQ successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
