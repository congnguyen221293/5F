﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCDev.APIGenerator.Model
{
    public class Tenant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid TenantId { get; set; } = new Guid();
        public string TenantName { get; set; } = string.Empty;
        public string TenantDomain { get; set; } = string.Empty;

        public AppUser Owner { get; set; }
        public Guid OwnerId { get; set; }

        public virtual IEnumerable<AppUser> Users { get; set; }
    }
}
