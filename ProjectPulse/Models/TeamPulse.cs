using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPulse.Models;

public class TeamPulse
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId    { get; set; }
        public int ProjectId { get; set; }

        /// <summary>Оценка настроения 1-5</summary>
        public int    MoodRating { get; set; }
        public string Comment    { get; set; }
        public DateTime CreatedAt{ get; set; } = DateTime.Now;

        public User    User    { get; set; }
        public Project Project { get; set; }
    }