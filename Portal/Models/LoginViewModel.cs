using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "введите имя пользователя")]
        public string login { get; set; }

        [Required(ErrorMessage = "введите пароль")]
        [DataType(DataType.Password)]
        public string password { get; set; }
        public string userIp { get; set; }
    }
}
