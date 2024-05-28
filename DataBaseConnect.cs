using System;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

namespace rm
{
    public class DataBaseConnect
    {
        private static string ConnString = "server=188.187.190.68;user=root;password=root;database=recmusic;";        
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            using (var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, 10000))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public bool VerifyLogin(string login, string password)
        {
            using (var connection = new MySqlConnection(ConnString))
            {
                connection.Open();
                string sql = "SELECT Password FROM recmusic WHERE Login = @login";
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    var result = cmd.ExecuteScalar()?.ToString();
                    if (result != null)
                    {
                        return VerifyPassword(password, result);
                    }
                }
            }
            return false;
        }

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);
                var passwordHash = Convert.ToBase64String(hashBytes);
                return passwordHash;
            }
        }

        public void RegisterUser(string login, string password)
        {
            using (var connection = new MySqlConnection(ConnString))
            {
                string hashedPassword = HashPassword(password);
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO recmusic (Login, Password) VALUES (@login, @password)";
                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Передайте исключение наверх, чтобы обработать его в пользовательском интерфейсе
                    throw new Exception("Database error: " + ex.Message);
                }
            }
        }

    }
}
