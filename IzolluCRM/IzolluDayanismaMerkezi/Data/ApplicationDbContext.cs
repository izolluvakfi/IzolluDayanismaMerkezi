using Microsoft.EntityFrameworkCore;
using IzolluVakfi.Data.Entities;

namespace IzolluVakfi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Donor> Donors { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<TranscriptRecord> TranscriptRecords { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<StudentMeetingAttendance> StudentMeetingAttendances { get; set; }
    public DbSet<MemberMeetingAttendance> MemberMeetingAttendances { get; set; }
    public DbSet<MemberScholarshipCommitment> MemberScholarshipCommitments { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }
    public DbSet<ScholarshipPayment> ScholarshipPayments { get; set; }
    public DbSet<Village> Villages { get; set; }
    public DbSet<Aid> Aids { get; set; }
    public DbSet<Term> Terms { get; set; }
    public DbSet<TermScholarshipConfig> TermScholarshipConfigs { get; set; }
    public DbSet<StudentScholarshipStatus> StudentScholarshipStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AylikTutar).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ToplamAlinanBurs).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.SicilNumarasi).IsUnique();
            entity.HasMany(e => e.Transcripts)
                  .WithOne(e => e.Student)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ScholarshipPayments)
                  .WithOne(e => e.Student)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Donor entity
        modelBuilder.Entity<Donor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BirimBursTutari).HasColumnType("decimal(18,2)");
        });

        // Configure Member entity
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SicilNumarasi).IsUnique();
        });

        // Configure TranscriptRecord entity
        modelBuilder.Entity<TranscriptRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Baslik).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ToplantiTuru).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Konum).HasMaxLength(500);
            entity.Property(e => e.BitisTarihi).IsRequired();
        });

        modelBuilder.Entity<StudentMeetingAttendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.MeetingId }).IsUnique();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.MeetingAttendances)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Meeting)
                  .WithMany(m => m.Attendances)
                  .HasForeignKey(e => e.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ActivityLog entity
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Configure Settings entity
        modelBuilder.Entity<Settings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Configure MemberScholarshipCommitment entity
        modelBuilder.Entity<MemberScholarshipCommitment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.YearlyAmountPerScholarship).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.PledgedCount).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Configure foreign key relationships
            entity.HasOne(e => e.Member)
                  .WithMany(m => m.ScholarshipCommitments)
                  .HasForeignKey(e => e.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SystemSettings entity
        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AppVersion).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
        });

        // Configure ScholarshipPayment entity
        modelBuilder.Entity<ScholarshipPayment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.PaymentType).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Configure foreign key relationships
            entity.HasOne(e => e.Commitment)
                  .WithMany()
                  .HasForeignKey(e => e.CommitmentId)
                  .OnDelete(DeleteBehavior.Restrict); // Preserve payment history

            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // CRITICAL INDEXES FOR PERFORMANCE
            // Index for commitment-based queries
            entity.HasIndex(e => e.CommitmentId)
                  .HasDatabaseName("IX_ScholarshipPayments_CommitmentId");

            // Index for student payment history
            entity.HasIndex(e => e.StudentId)
                  .HasDatabaseName("IX_ScholarshipPayments_StudentId");

            // Index for payment date queries
            entity.HasIndex(e => e.PaymentDate)
                  .HasDatabaseName("IX_ScholarshipPayments_PaymentDate");
        });

        // Configure StudentScholarshipStatus entity
        modelBuilder.Entity<StudentScholarshipStatus>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CutReason).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            // Unique constraint: one status per student per term per month
            entity.HasIndex(e => new { e.StudentId, e.TermId, e.Month, e.Year })
                  .IsUnique()
                  .HasDatabaseName("IX_StudentScholarshipStatus_Unique");

            // Foreign key relationships
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Term)
                  .WithMany()
                  .HasForeignKey(e => e.TermId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
