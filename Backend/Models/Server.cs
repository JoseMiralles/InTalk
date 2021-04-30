using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Intalk.Models
{
    public class Server
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string Title { get; set; }

        public ICollection<ApplicationUser> Users { get; set; }
        public List<UserServer> UserServers { get; set; }
    }

    public class UserServer
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public Server Server { get; set; }
        public long ServerId { get; set; }

        public long Role { get; set; }

        public enum Roles
        {
            Member = 0,
            Owner = 1
        }
    }
}