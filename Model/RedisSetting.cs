namespace Capstone.Model
{
    public class RedisSetting
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 6379;
        public string Password { get; set; } = "";
        public int DefaultDatabase { get; set; } = 0;
    }
}
