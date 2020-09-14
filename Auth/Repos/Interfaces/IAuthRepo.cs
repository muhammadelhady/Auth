using Auth.Dtos;
using Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Auth.Repos
{
    public interface IAuthRepo
    {
        Task<Users> Register(Users user, string password);
        Task<Users> Login(string email, string password);
        Task<bool> ChangePassword(string token , ChangeUserPasswordDto changeUserPasswordDto);
        Task<bool> UserExists(string Email);
        Task<bool> ResetPassword(ResetPasswordDto resetPasswordDto);
    }
}
