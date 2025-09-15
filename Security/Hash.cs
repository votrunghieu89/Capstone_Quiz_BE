namespace Capstone.Security
{
    public static class Hash
    {
        // Mã hóa mật khẩu sử dụng BCrypt
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Xác minh mật khẩu so với mật khẩu đã mã hóa
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
