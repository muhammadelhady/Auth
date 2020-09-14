using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Auth.Dtos
{
    public class ChangeUserPasswordDto
    {
        public string OldPassword { get; set; }
        public string newPassword { get; set; }
    }
}
