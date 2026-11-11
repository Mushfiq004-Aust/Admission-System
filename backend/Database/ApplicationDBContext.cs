using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Database
{
    // The DbContext is the "door" to the database. Repositories use it to read and write tables.
    // OnModelCreating describes how the C# models map to tables, columns, constraints and indexes.
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();
        public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AdmissionClass>(e =>
            {
                e.ToTable("AdmissionClasses", t =>
                    t.HasCheckConstraint("CK_AdmissionClasses_TotalSeats_Positive", "\"TotalSeats\" > 0"));
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<AdmissionHold>(e =>
            {
                e.ToTable("AdmissionHolds", t =>
                {
                    t.HasCheckConstraint("CK_AdmissionHolds_Status",
                        "\"Status\" IN ('HELD','CONFIRMED','EXPIRED','CANCELLED')");
                    t.HasCheckConstraint("CK_AdmissionHolds_StudentId_Format",
                        "\"StudentId\" ~ '^[0-9]{17}$'");
                });
                e.HasKey(x => x.Id);
                e.Property(x => x.StudentId).HasMaxLength(17).IsRequired();
                e.Property(x => x.Status).HasMaxLength(16).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
                e.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
                e.Property(x => x.ConfirmedAt).HasColumnType("timestamp with time zone");

                e.HasOne(x => x.Class)
                    .WithMany(c => c.Holds)
                    .HasForeignKey(x => x.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => x.ClassId).HasDatabaseName("IX_AdmissionHolds_ClassId");
                e.HasIndex(x => x.StudentId).HasDatabaseName("IX_AdmissionHolds_StudentId");
                e.HasIndex(x => x.Status).HasDatabaseName("IX_AdmissionHolds_Status");
                e.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_AdmissionHolds_ExpiresAt");
                e.HasIndex(x => new { x.ClassId, x.StudentId, x.Status })
                    .HasDatabaseName("IX_AdmissionHolds_ClassId_StudentId_Status");

                // Safety net at the database level: a student can have at most ONE
                // active (HELD or CONFIRMED) application per class.
                e.HasIndex(x => new { x.ClassId, x.StudentId })
                    .IsUnique()
                    .HasDatabaseName("UX_AdmissionHolds_ActivePerStudentPerClass")
                    .HasFilter("\"Status\" IN ('HELD','CONFIRMED')");
            });
        }
    }
}
