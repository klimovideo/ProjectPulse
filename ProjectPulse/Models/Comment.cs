using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectPulse.Models;

public class Comment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProjectTaskId { get; set; }
        public int ProjectId     { get; set; }   // 0 если комментарий к задаче
        public int UserId        { get; set; }

        public string Content        { get; set; }
        public string AttachmentPath { get; set; }
        public DateTime CreatedAt    { get; set; } = DateTime.Now;

        public User        User        { get; set; }
        public ProjectTask ProjectTask { get; set; }
    }
