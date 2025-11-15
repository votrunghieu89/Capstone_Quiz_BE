using Capstone.Database;
using Capstone.Model;
using Capstone.Repositories;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class TopicService : ITopicRepository
    {
        private readonly ILogger<TopicService> _logger;
        private readonly AppDbContext _appDbContext;
        
        public TopicService(ILogger<TopicService> logger, AppDbContext appDbContext)
        {   
            _logger = logger;
            _appDbContext = appDbContext;
        }
        public async Task<List<TopicModel>> getAllTopic()
        {
            try
            {
                var lists = await _appDbContext.topics.ToListAsync();
                if(lists != null)
                {
                    return lists;
                }
                else
                {
                    return new List<TopicModel>();
                }
            }
            catch (Exception ex) {
                return new List<TopicModel>();
            }
        }

        public async Task<string> GetTopicName(int topicId)
        {
            try
            {
                var TopicName = await _appDbContext.topics.Where(t => t.TopicId == topicId).Select(t => t.TopicName).FirstOrDefaultAsync();
                if(TopicName == null)
                {
                    return null;
                }
                return TopicName;
            }
            catch (Exception ex) {
                return null;
            
            }
        }

       
    }
}
