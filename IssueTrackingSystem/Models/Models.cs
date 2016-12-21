using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace BTS.Models
{
    public class User
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name must contain at least 3 characters")]
        [MaxLength(20, ErrorMessage = "Name field must contain at most 20 characters")]
        [MinLength(3,ErrorMessage ="Name field must contain at least 3 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Surname must contain at least 3 characters")]
        [MaxLength(20, ErrorMessage = "Surname field must contain at most 20 characters")]
        [MinLength(3, ErrorMessage = "Surname field must contain at least 3 characters")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Nickname must contain at least 3 characters")]
        [MaxLength(20, ErrorMessage = "Nickname field must contain at most 20 characters")]
        [MinLength(3, ErrorMessage = "Nickname field must contain at least 3 characters")]
        public string Nickname { get; set; }

        [Required(ErrorMessage = "Date of birth must be filled")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = false)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Email must be filled")]
        [MaxLength(30, ErrorMessage="Email field must contain at most 30 characters")]
        //[RegularExpression("/^(([^<>()[\\]\\.,;:\\s@\"]+(\\.[^<>()[\\]\\.,;:\\s@\"]+)*)|(\".+\"))@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\])|(([a-zA-Z\\-0-9]+\\.)+[a-zA-Z]{2,}))$/", ErrorMessage ="Email is invalid")]
        public string Email { get; set; }
        public byte[] Avatar { get; set; }

        [Required(ErrorMessage = "Password must contain at least 5 characters")]
        [MaxLength(20, ErrorMessage = "Password field must contain at most 20 characters")]
        [MinLength(5, ErrorMessage = "Password field must contain at least 5 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage ="Status field must be filled")]
        [MaxLength(15, ErrorMessage = "Status field must contain at most 15 characters")]
        [Display(Name = "Apply status")]
        public string Status { get; set; }

        public bool Confirmed { get; set; }
    }

    public class Project
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage ="Field Name must be filled")]
        [MaxLength(20, ErrorMessage ="Field Name must contain at most 20 characters")]
        [MinLength(3, ErrorMessage = "Field Name must contain at least 3 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage ="Field Description must be filled")]
        public string Description { get; set; }
        public byte[] Logo { get; set; }

        public int PmId { get; set; }
    }



    public class Bug
    {

        public int Id { get; set; }
        
        [Required(ErrorMessage ="Subject field cannot be empty")]
        [MaxLength(30, ErrorMessage = "Subject field must contain at most 30 characters")]
        [MinLength(5, ErrorMessage = "Subject field must contain at least 5 characters")]
        public string Subject { get; set; }

        [Required(ErrorMessage ="Description field must be filled")]
        public string Description { get; set; }

        public byte[] Image { get; set; }

        public int ProjectId { get; set; }


        [MaxLength(30, ErrorMessage ="Status field must contain at most 30 characters")]
        public string Status { get; set; }

        public int? Estimate { get; set; }

        public string TopicStarter { get; set; }

        public int DeveloperId { get; set; }

        public string Solution { get; set; }

        public DateTime AddingTime { get; set; }

        public DateTime StatusChangeDate { get; set; }
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

        public IEnumerable<HttpPostedFileBase> Attachments { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }
        public string MessageText { get; set; }
        public DateTime AddingTime { get; set;}
        public string SenderNick { get; set; }
        public int BugId { get; set; }
        public bool Correct { get; set; }

        public string UserToReply { get; set; }

        public int? MessageToReplyId { get; set; }
    }


    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(15)]
        public string Sender { get; set; }

        [Required]
        [MaxLength(15)]
        public string Receiver { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime SendDate { get; set; }
    }

    [DataContract]
    public class Attachment
    {
        [DataMember]
        [Required]
        public int Id { get; set; }

        [DataMember]
        public int BugId { get; set; }

        [DataMember]
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        [Required]
        [DataMember]
        public byte[] Data { get; set; }
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
