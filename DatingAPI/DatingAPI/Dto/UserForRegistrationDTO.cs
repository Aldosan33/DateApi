﻿using System;
using System.ComponentModel.DataAnnotations;

namespace DatingAPI.Dto
{
    public class UserForRegistrationDTO
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "You must specify a password between 4 and 8 characters")]
        public string Password { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string KnownAs { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string Country { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }
        public UserForRegistrationDTO()
        {
            Created = DateTime.Now;
            LastActive = DateTime.Now;
        }
    }
}