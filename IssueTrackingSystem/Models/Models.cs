﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTS.Models
{
    public class User
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string Name { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string Surname { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string Nickname { get; set; }


        // + date mask annotation

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime BirthDate { get; set; }

        [Required]
        [MaxLength(30)]
        public string Email { get; set; }
        public byte[] Avatar { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(5)]
        public string Password { get; set; }

        [Required]
        [MaxLength(15)]
        [Display(Name = "Apply status")]
        public string Status { get; set; }
    }

    public class Project
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }
        public byte[] Logo { get; set; }
        public string Updates { get; set; }
    }

    public class Bug
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [MinLength(5)]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }
        public byte[] Image { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; }

        public int? Estimate { get; set; }
    }

    public class ProjectBugs
    {
        public Project proj { get; set; }

        public List<Bug> bugs { get; set; }
    }

    public class ProjectUser
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(15)]
        public string Status { get; set; }
    }

    public class Notification
    {
        [Required]
        [MaxLength(15)]
        public string Sender { get; set; }

        [Required]
        [MaxLength(15)]
        public string Receiver { get; set; }

        [Required]
        public string Message { get; set; }
    }

    public class Attachment
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }
    }

    public class BugAttachment
    {
        [Required]
        public int BugId { get; set; }

        [Required]
        public int AttachmentId { get; set; }
    }

    public class Category
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Title { get; set; }
    }

    public class ProjectCategory
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}
