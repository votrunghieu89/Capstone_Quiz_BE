using Capstone.Model;

namespace Capstone.Repositories
{
    public interface ITopicRepository
    {
        public Task<List<TopicModel>> getAllTopic();
    }
}
