using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderService.Model;
using OrderService.Repository.Interface;
using System.Data;
using System.Threading.Tasks;

namespace OrderService.Repository.Service
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly IJwtService _jwtService; 

        public UserRepository(IConfiguration configuration, IJwtService jwtService) // Inject IJwtService
        {
            _connectionString = configuration.GetConnectionString("ConnStringDb");
            _jwtService = jwtService; 
        }

        public async Task<int> AddUser(UserModel user)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Generate JWT token
                    var tokenResult = await _jwtService.GenerateToken(user.loginame);
                    string jwtToken = tokenResult.Item1;
                    DateTime jwtTokenExpiry = tokenResult.Item2;

                    using (var command = new SqlCommand("usp_AddUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@loginame", user.loginame);
                        command.Parameters.AddWithValue("@phone", user.phone);
                        command.Parameters.AddWithValue("@Password", user.Password);
                        command.Parameters.AddWithValue("@Name", user.Name ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedBy", DBNull.Value);
                        command.Parameters.AddWithValue("@JwtToken", jwtToken);
                        command.Parameters.AddWithValue("@JwtTokenExpiryDate", jwtTokenExpiry);

                        await connection.OpenAsync();

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding user: " + ex.Message);
                return -1; // Indicate failure
            }
        }

        public async Task<UserModel?> LoginAsync(string loginame, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("sp_LoginUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@loginame", loginame);
                command.Parameters.AddWithValue("@Password", password);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new UserModel
                        {
                            loginame = reader["loginame"].ToString(),
                            Password = reader["Password"].ToString(),
                            Address = reader["Address"] == DBNull.Value ? "null" : reader["Address"].ToString(),
                            Name = reader["Name"].ToString(),
                            CreatedDate = reader["CreatedDate"] as DateTime?,
                            IsActive = reader["IsActive"] as bool?
                        };
                    }
                }
            }
            return null;
        }

        //public Task<bool> ResetPassword(string phone, string newPassword)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
  