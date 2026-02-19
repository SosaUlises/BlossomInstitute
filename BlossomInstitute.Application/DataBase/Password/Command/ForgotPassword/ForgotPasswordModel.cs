using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Password.Command.ForgotPassword
{
    public class ForgotPasswordModel
    {
        public string Email { get; set; } = default!;
        public string? FrontendResetUrl { get; set; }
    }
}
