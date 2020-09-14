using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Auth.Dtos;
using Auth.Models;
using Auth.Repos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepo _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepo repo, IConfiguration config)
        {
            _config = config;
            _repo = repo;

        }


        [HttpGet]
        public ActionResult GetValues()
        {
            var Values = "hello";
            return Ok(Values);
        }


        [HttpPost("ResetPassword")]
        public async Task< ActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {

            if (!await _repo.UserExists(resetPasswordDto.Email))
                return BadRequest("User Is Not Exists");

            if (await _repo.ResetPassword(resetPasswordDto))
                return Ok("please check your email");
         
            return BadRequest("something wrong please try again!");
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            //validate the request 
            userForRegisterDto.UserName = userForRegisterDto.UserName.ToLower();
            userForRegisterDto.Email = userForRegisterDto.Email.ToLower();

            if (await _repo.UserExists(userForRegisterDto.UserName)) return BadRequest();

            var userToCreat = new Users
            {
                UserName = userForRegisterDto.UserName,
                Phone=userForRegisterDto.Phone,
                Email=userForRegisterDto.Email
            };

            var createdUser = await _repo.Register(userToCreat, userForRegisterDto.Password);
            return StatusCode(201);

        }
        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task < IActionResult> ChangePassword(ChangeUserPasswordDto changeUserPasswordDto)
        {
            var jwt = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            if (!await _repo.ChangePassword(jwt, changeUserPasswordDto))
                return BadRequest("something worng try again");

            return Ok();
        }



      

        [HttpPost("login")]
        public async Task<IActionResult> Login( UserForLoginDto  userForLoginDot)
        {
            var userFromRepo = await _repo.Login(userForLoginDot.Email.ToLower(), userForLoginDot.Password);

            if (userFromRepo == null)
                return Unauthorized();

            var claims = new[]{
           new Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
           new Claim(ClaimTypes.Name,userFromRepo.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds

            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new
            {
                token = tokenHandler.WriteToken(token)

            });
        }
    }
}
