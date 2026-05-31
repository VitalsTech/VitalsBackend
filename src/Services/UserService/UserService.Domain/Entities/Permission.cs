using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.Entities
{
    public class Permission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Resource { get; set; }

        public string? Action { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}