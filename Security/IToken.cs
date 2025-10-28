namespace Capstone.Security
{
    public interface IToken
    {
        public string generateAccessToken(int accountId, string role, string email);
        public string generateRefreshToken();
    }
}
