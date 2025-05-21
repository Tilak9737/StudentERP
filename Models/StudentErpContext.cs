using Microsoft.EntityFrameworkCore;

namespace StudentERP.Models;

public partial class StudentErpContext : DbContext
{
    public StudentErpContext()
    {
    }

    public StudentErpContext(DbContextOptions<StudentErpContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdmissionDetail> AdmissionDetails { get; set; }

    public virtual DbSet<ContactDetail> ContactDetails { get; set; }

    public virtual DbSet<DegreeName> DegreeNames { get; set; }

    public virtual DbSet<FieldName> FieldNames { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<ParentsDetail> ParentsDetails { get; set; }

    public virtual DbSet<PersonalDetail> PersonalDetails { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentBatch> StudentBatches { get; set; }

    public virtual DbSet<StudentLogin> StudentLogins { get; set; }

    public virtual DbSet<StudentProfile> StudentProfiles { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TILAK; Database=StudentERP; Integrated Security=True; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Admissio__32C52B996C32C1AD");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.AdmissionNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Course)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithOne(p => p.AdmissionDetail)
                .HasForeignKey<AdmissionDetail>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Admission__Stude__5DCAEF64");
        });

        modelBuilder.Entity<ContactDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__ContactD__32C52B993F7AB32E");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ZipCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithOne(p => p.ContactDetail)
                .HasForeignKey<ContactDetail>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContactDe__Stude__60A75C0F");
        });

        modelBuilder.Entity<DegreeName>(entity =>
        {
            entity.HasKey(e => e.Did).HasName("PK__DegreeNa__C036563074E883FE");

            entity.ToTable("DegreeName");

            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("DID");
            entity.Property(e => e.Dname)
                .HasMaxLength(100)
                .HasColumnName("DName");
        });

        modelBuilder.Entity<FieldName>(entity =>
        {
            entity.HasKey(e => e.Fid).HasName("PK__FieldNam__C1BEA5A2D08FA9CE");

            entity.ToTable("FieldName");

            entity.Property(e => e.Fid)
                .HasMaxLength(20)
                .HasColumnName("FID");
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("DID");
            entity.Property(e => e.Fname)
                .HasMaxLength(100)
                .HasColumnName("FName");

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.FieldNames)
                .HasForeignKey(d => d.Did)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FieldName__DID__71D1E811");
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Otps__3214EC07620556A6");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.OtpCode).HasMaxLength(6);
            entity.Property(e => e.StudentMail).HasMaxLength(255);

            entity.HasOne(d => d.StudentMailNavigation).WithMany(p => p.Otps)
                .HasPrincipalKey(p => p.StudentMail)
                .HasForeignKey(d => d.StudentMail)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Otps_StudentLogins");
        });

        modelBuilder.Entity<ParentsDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__ParentsD__32C52B99E26507FB");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.FatherName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MotherName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentPhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithOne(p => p.ParentsDetail)
                .HasForeignKey<ParentsDetail>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ParentsDe__Stude__6383C8BA");
        });

        modelBuilder.Entity<PersonalDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Personal__32C52B99C7532DD1");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithOne(p => p.PersonalDetail)
                .HasForeignKey<PersonalDetail>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PersonalD__Stude__5AEE82B9");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemId).HasName("PK__Semester__16D6C78A5C8E2525");

            entity.ToTable("Semester");

            entity.Property(e => e.SemId)
                .HasMaxLength(30)
                .HasColumnName("SemID");
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("DID");
            entity.Property(e => e.Fid)
                .HasMaxLength(20)
                .HasColumnName("FID");

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.Semesters)
                .HasForeignKey(d => d.Did)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Semester__DID__74AE54BC");

            entity.HasOne(d => d.FidNavigation).WithMany(p => p.Semesters)
                .HasForeignKey(d => d.Fid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Semester__FID__75A278F5");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A7974549BE8");

            entity.HasIndex(e => e.Email, "UQ__Students__A9D1053470C2FBF7").IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
        });

        modelBuilder.Entity<StudentBatch>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__StudentB__32C52A790AC17FB0");

            entity.ToTable("StudentBatch");

            entity.Property(e => e.StudentId)
                .ValueGeneratedNever()
                .HasColumnName("StudentID");
            entity.Property(e => e.CurrentSem).HasMaxLength(30);
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("DID");
            entity.Property(e => e.Fid)
                .HasMaxLength(20)
                .HasColumnName("FID");

            entity.HasOne(d => d.CurrentSemNavigation).WithMany(p => p.StudentBatches)
                .HasForeignKey(d => d.CurrentSem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentBa__Curre__00200768");

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.StudentBatches)
                .HasForeignKey(d => d.Did)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentBatc__DID__7E37BEF6");

            entity.HasOne(d => d.FidNavigation).WithMany(p => p.StudentBatches)
                .HasForeignKey(d => d.Fid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentBatc__FID__7F2BE32F");

            entity.HasOne(d => d.Student).WithOne(p => p.StudentBatch)
                .HasForeignKey<StudentBatch>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentBa__Stude__7D439ABD");
        });

        modelBuilder.Entity<StudentLogin>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__StudentL__32C52A790441BE22");

            entity.ToTable("StudentLogin");

            entity.HasIndex(e => e.StudentMail, "UQ__StudentL__71B0EA10FD5A75AF").IsUnique();

            entity.Property(e => e.StudentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("StudentID");
            entity.Property(e => e.HashPassword).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StudentMail).HasMaxLength(255);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__StudentP__32C52B9937B76268");

            entity.ToTable("StudentProfile");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.ProfilePictureName).HasMaxLength(255);

            entity.HasOne(d => d.Student).WithOne(p => p.StudentProfile)
                .HasForeignKey<StudentProfile>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentPr__Stude__5812160E");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA388603EDFD8");

            entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            entity.Property(e => e.Did)
                .HasMaxLength(10)
                .HasColumnName("DID");
            entity.Property(e => e.Fid)
                .HasMaxLength(20)
                .HasColumnName("FID");
            entity.Property(e => e.SemId)
                .HasMaxLength(30)
                .HasColumnName("SemID");
            entity.Property(e => e.SubjectCode).HasMaxLength(20);
            entity.Property(e => e.SubjectName).HasMaxLength(100);
            entity.Property(e => e.SyllabusFileName).HasMaxLength(255);

            entity.HasOne(d => d.DidNavigation).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.Did)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subjects__DID__787EE5A0");

            entity.HasOne(d => d.FidNavigation).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.Fid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subjects__FID__797309D9");

            entity.HasOne(d => d.Sem).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.SemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subjects__SemID__7A672E12");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACD97172F2");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E40DF41669").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Student).WithMany(p => p.Users)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_Students");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
