using Capstone.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopicController : ControllerBase
    {
        private readonly ITopicRepository _topicRepository;
        private readonly ILogger<TopicController> _logger;

        public TopicController(ITopicRepository topicRepository, ILogger<TopicController> logger)
        {
            _topicRepository = topicRepository;
            _logger = logger;
        }

        [HttpGet("getAllTopic")]
    
        public async Task<IActionResult> getAllTopic()
        {
            try
            {
                var lists = await _topicRepository.getAllTopic();
                return Ok(lists);
            }
            catch (Exception ex) { 
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("getTopicName")]
      
        public async Task<IActionResult> getTopicname(int topicId)
        {
            try
            {
                var Name = await _topicRepository.GetTopicName(topicId);
                return Ok(Name);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
