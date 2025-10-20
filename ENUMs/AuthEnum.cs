namespace Capstone.ENUMs
{
    public class AuthEnum
    {
        public enum Role
        {
            Student,
            Teacher,
            Admin
        }
        public enum Login
        {
            WrongEmailOrPassword,
            AccountHasBanned,
            Success,
            Error
        }

    }
}
