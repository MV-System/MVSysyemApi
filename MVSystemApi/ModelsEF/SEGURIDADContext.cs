using Microsoft.EntityFrameworkCore;

namespace MVSystemApi.ModelsEF
{
    public partial class SEGURIDADContext : DbContext
    {
        public SEGURIDADContext(DbContextOptions<SEGURIDADContext> options) : base(options)
        {
        }

        public virtual DbSet<Empresa> Empresas { get; set; }
        public virtual DbSet<Permiso> Permisos { get; set; }
        public virtual DbSet<PermisoUsuario> PermisoUsuarios { get; set; }
        public virtual DbSet<RolPermiso> RolPermisos { get; set; }
        public virtual DbSet<RolUsuario> RolUsuarios { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Estado)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('A')")
                    .IsFixedLength();

                entity.Property(e => e.IdEmpresa)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("ID_Empresa");

                entity.Property(e => e.NombreEmpresa)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("Nombre_Empresa");

                entity.Property(e => e.RegistroUsuario)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("(suser_sname())");

                entity.Property(e => e.Registrofecha)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.StringCon)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Permiso>(entity =>
            {
                entity.Property(e => e.PermissionName)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<PermisoUsuario>(entity =>
            {
                entity.ToTable("PermisoUsuario");

                entity.HasOne(d => d.IdPermisoNavigation)
                    .WithMany(p => p.PermisoUsuarios)
                    .HasForeignKey(d => d.IdPermiso)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PermisoUs__IdPer__1F98B2C1");

                entity.HasOne(d => d.IdUserNavigation)
                    .WithMany(p => p.PermisoUsuarios)
                    .HasForeignKey(d => d.IdUser)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PermisoUs__IdUse__2180FB33");
            });

            modelBuilder.Entity<RolPermiso>(entity =>
            {
                entity.ToTable("RolPermiso");

                entity.HasOne(d => d.IdPermisoNavigation)
                    .WithMany(p => p.RolPermisos)
                    .HasForeignKey(d => d.IdPermiso)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__RolPermis__IdPer__1DB06A4F");

                entity.HasOne(d => d.IdRoleNavigation)
                    .WithMany(p => p.RolPermisos)
                    .HasForeignKey(d => d.IdRole)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__RolPermis__IdRol__1EA48E88");
            });

            modelBuilder.Entity<RolUsuario>(entity =>
            {
                entity.ToTable("RolUsuario");

                entity.HasOne(d => d.IdRolNavigation)
                    .WithMany(p => p.RolUsuarios)
                    .HasForeignKey(d => d.IdRol)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__RolUsuari__IdRol__236943A5");

                entity.HasOne(d => d.IdUserNavigation)
                    .WithMany(p => p.RolUsuarios)
                    .HasForeignKey(d => d.IdUser)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__RolUsuari__IdUse__22751F6C");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.RoleName).HasMaxLength(100);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Codigo)
                    .HasName("PK_Usu_Codigo");

                entity.Property(e => e.Descripcion)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Estado).HasDefaultValueSql("((1))");

                entity.Property(e => e.FechaModificado)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FechaRegistro)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Login)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
