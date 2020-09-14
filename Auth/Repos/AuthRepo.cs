using Auth.Data;
using Auth.Dtos;
using Auth.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using MailKit.Security;


namespace Auth.Repos
{
    public class AuthRepo : IAuthRepo
    {

        private readonly DatabaseContext _context;

        public AuthRepo(DatabaseContext context)
        {
            _context = context;

        }
        public async Task<Users> Login(string email, string password)
        {
          
            var user = await _context.Users.FirstOrDefaultAsync(X => X.Email == email);
            if (user == null) return null;

            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt)) return null;

            return user;
        }

        private bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {

                var ComputeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                if (ComputeHash.Length == passwordHash.Length)
                {

                    for (int i = 0; i < ComputeHash.Length; i++)
                    {
                        if (ComputeHash[i] != passwordHash[i]) return false;

                    }

                }

            }
            return true;
        }

        public async Task<Users> Register(Users user, string password)
        {
            byte[] passwordHash;
            byte[] passwordSalt;
            CreatPasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;

        }

        private void CreatPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<bool> UserExists(string Email)
        {
            return await _context.Users.AnyAsync(X => X.Email == Email);

        }
      

        public async Task<bool> ChangePassword(string token, ChangeUserPasswordDto changeUserPasswordDto)
        {
            //get user id from token 
            int userId = ExtractUserIdFromToken(token);
            //retrive user using user id 
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return false;

           if( VerifyPassword(changeUserPasswordDto.OldPassword,user.PasswordHash,user.PasswordSalt))
                {  
                 user = await SetUserPassword(user, changeUserPasswordDto.newPassword);
                return true;
                }

         
            return false;
        }

        private async Task< Users> SetUserPassword(Users user , string password)
        {
            byte[] passwordHash;
            byte[] passwordSalt;
            CreatPasswordHash(password, out passwordHash, out passwordSalt);
            var dbUser =await _context.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
            dbUser.PasswordHash = passwordHash;
            dbUser.PasswordSalt = passwordSalt;
            if (await _context.SaveChangesAsync()>0)        
            return dbUser;
            return null;
        }

        private int ExtractUserIdFromToken(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var id = token.Claims.FirstOrDefault(x => x.Type == "nameid").Value;

            return int.Parse(id);
        }

        public async Task<bool> ResetPassword(ResetPasswordDto resetPasswordDto)
        {

            Random R = new Random();
           

            string newPassword = R.Next(12121212, 98989898)+"";
            Users user = await _context.Users.FirstOrDefaultAsync(x=>x.Email==resetPasswordDto.Email);

            if (user == null)
                return false;

            await   SetUserPassword(user, newPassword);
            SendMail(newPassword);
            return true;
        }

        private void SendMail(string newPassword)
        {
            // create email message
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse("mum.mido93@gmail.com");
            email.To.Add(MailboxAddress.Parse("muhammad.elhady@outlook.com"));
            email.Subject = "Recover Your Password";
            email.Body = new TextPart(TextFormat.Plain) { Text = "Your new password is "+newPassword+" you can use it login or change it with a new one " };

            // send email
            using var smtp = new SmtpClient();
         
            smtp.ServerCertificateValidationCallback= (s, c, h, e) => true;
            smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("mum.mido93@gmail.com", "kwaudcwfpphufsje");
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
