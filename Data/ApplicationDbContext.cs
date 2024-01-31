using BeautyHubAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeautyHubAPI.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(300);
        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public virtual DbSet<BankDetail> BankDetail { get; set; } = null!;
        public virtual DbSet<Banner> Banner { get; set; } = null!;
        public virtual DbSet<CityMaster> CityMaster { get; set; } = null!;
        public virtual DbSet<CountryMaster> CountryMaster { get; set; } = null!;
        public virtual DbSet<MainCategory> MainCategory { get; set; } = null!;
        public virtual DbSet<MembershipPlan> MembershipPlan { get; set; } = null!;
        public virtual DbSet<MembershipRecord> MembershipRecord { get; set; } = null!;
        public virtual DbSet<PaymentReceipt> PaymentReceipt { get; set; } = null!;
        public virtual DbSet<SalonBanner> SalonBanner { get; set; } = null!;
        public virtual DbSet<SalonDetail> SalonDetail { get; set; } = null!;
        public virtual DbSet<StateMaster> StateMaster { get; set; } = null!;
        public virtual DbSet<SubCategory> SubCategory { get; set; } = null!;
        public virtual DbSet<Upidetail> Upidetail { get; set; } = null!;
        public virtual DbSet<UserDetail> UserDetail { get; set; } = null!;
        public virtual DbSet<CustomerSalon> CustomerSalon { get; set; } = null!;
        public virtual DbSet<CustomerAddress> CustomerAddress { get; set; }
        public virtual DbSet<VendorCategory> VendorCategory { get; set; } = null!;
        public virtual DbSet<SalonSchedule> SalonSchedule { get; set; } = null!;
        public virtual DbSet<SalonService> SalonService { get; set; } = null!;
        public virtual DbSet<TimeSlot> TimeSlot { get; set; } = null!;
        public virtual DbSet<NotificationSent> NotificationSent { get; set; } = null!;
        public virtual DbSet<Notification> Notification { get; set; } = null!;
        public virtual DbSet<FavouriteService> FavouriteService { get; set; } = null!;
        public virtual DbSet<Cart> Cart { get; set; } = null!;
        public virtual DbSet<FavouriteSalon> FavouriteSalon { get; set; } = null!;
        public virtual DbSet<Appointment> Appointment { get; set; } = null!;
        public virtual DbSet<BookedService> BookedService { get; set; } = null!;
        public virtual DbSet<ServicePackage> ServicePackage { get; set; } = null!;
        public virtual DbSet<CustomerSearchRecord> CustomerSearchRecord { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerSearchRecord>(entity =>
                                    {
                                        entity.HasKey(e => e.RecordId);

                                        entity.Property(e => e.CreateDate)
                                            .HasColumnType("datetime")
                                            .HasDefaultValueSql("(getdate())");

                                        entity.Property(e => e.CustomerSearchItem).HasMaxLength(500);

                                        entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                                        entity.HasOne(d => d.CustomerUser)
                                            .WithMany(p => p.CustomerSearchRecord)
                                            .HasForeignKey(d => d.CustomerUserId)
                                            .HasConstraintName("FK_CustomerSearchRecord_UserDetail");
                                    });

            modelBuilder.Entity<ServicePackage>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.PackageService)
                    .WithMany(p => p.ServicePackage)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ServicePackage_SalonService");
            });

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.Property(e => e.AppointmentStatus).HasMaxLength(100);

                entity.Property(e => e.CancelledBy).HasMaxLength(50);

                entity.Property(e => e.Cgst).HasColumnName("CGST");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerFirstName).HasMaxLength(100);

                entity.Property(e => e.CustomerLastName).HasMaxLength(100);

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.Property(e => e.Igst).HasColumnName("IGST");

                entity.Property(e => e.PaymentMethod).HasMaxLength(100);

                entity.Property(e => e.PaymentStatus).HasMaxLength(100);

                entity.Property(e => e.PhoneNumber).HasMaxLength(50);

                entity.Property(e => e.Sgst).HasColumnName("SGST");

                entity.Property(e => e.TransactionId).HasMaxLength(100);
            });

            modelBuilder.Entity<BookedService>(entity =>
            {
                entity.Property(e => e.AppointmentDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FromTime).HasMaxLength(50);

                entity.Property(e => e.ToTime).HasMaxLength(50);

                entity.Property(e => e.VendorId).HasMaxLength(450);
                entity.Property(e => e.AppointmentStatus).HasMaxLength(100);

                entity.HasOne(d => d.Appointment)
                    .WithMany(p => p.BookedService)
                    .HasForeignKey(d => d.AppointmentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BookedService_Appointment");
            });

            modelBuilder.Entity<FavouriteSalon>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.HasOne(d => d.CustomerUser)
                    .WithMany(p => p.FavouriteSalon)
                    .HasForeignKey(d => d.CustomerUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavouriteSalon_UserDetail");

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.FavouriteSalon)
                    .HasForeignKey(d => d.SalonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavouriteSalon_SalonDetail");
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.CustomerUser)
                    .WithMany(p => p.Cart)
                    .HasForeignKey(d => d.CustomerUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Cart_UserDetail");

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.Cart)
                    .HasForeignKey(d => d.SalonId)
                    .HasConstraintName("FK_Cart_SalonDetail");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.Cart)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Cart_SalonService");

                entity.HasOne(d => d.Slot)
                    .WithMany(p => p.Cart)
                    .HasForeignKey(d => d.SlotId)
                    .HasConstraintName("FK_Cart_TimeSlot");
            });

            modelBuilder.Entity<FavouriteService>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.HasOne(d => d.CustomerUser)
                    .WithMany(p => p.FavouriteService)
                    .HasForeignKey(d => d.CustomerUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavouriteService_UserDetail");

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.FavouriteService)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavouriteService_SalonService");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreatedBy).HasMaxLength(450);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Title).HasMaxLength(500);

                entity.Property(e => e.NotificationType)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.UserRole)
                .HasMaxLength(100)
                .IsUnicode(false);
            });

            modelBuilder.Entity<NotificationSent>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NotificationType)
                .HasMaxLength(100)
                .IsUnicode(false);

                entity.Property(e => e.Title).HasMaxLength(500);

                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.NotificationSent)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_NotificationSent_UserDetail");
            });

            modelBuilder.Entity<TimeSlot>(entity =>
            {
                entity.HasKey(e => e.SlotId);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FromTime).HasMaxLength(50);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.SlotDate).HasColumnType("datetime");

                entity.Property(e => e.ToTime).HasMaxLength(50);

                entity.HasOne(d => d.Service)
                    .WithMany(p => p.TimeSlot)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TimeSlot_SalonService");
            });

            modelBuilder.Entity<SalonService>(entity =>
            {
                entity.HasKey(e => e.ServiceId);

                entity.Property(e => e.AgeRestrictions)
                    .HasMaxLength(50)
                    .HasDefaultValueSql("(N'Adult')");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.GenderPreferences)
                    .HasMaxLength(50)
                    .HasDefaultValueSql("(N'Male')");

                entity.Property(e => e.ServiceType)
                .HasMaxLength(50)
                .HasDefaultValueSql("(N'Single')");

                entity.Property(e => e.LockTimeEnd).HasMaxLength(50);

                entity.Property(e => e.LockTimeStart).HasMaxLength(50);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ServiceName).HasMaxLength(1000);

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.SalonService)
                    .HasForeignKey(d => d.SalonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SalonService_SalonDetail");
            });

            modelBuilder.Entity<SalonSchedule>(entity =>
            {
                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeletedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.FromTime).HasMaxLength(100);

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.ToTime).HasMaxLength(100);

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.SalonSchedule)
                    .HasForeignKey(d => d.SalonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SalonSchedule_SalonDetail");
            });

            modelBuilder.Entity<VendorCategory>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Status).HasDefaultValueSql("((0))");

                entity.Property(e => e.VendorId).HasMaxLength(450);

                entity.HasOne(d => d.MainCategory)
                    .WithMany(p => p.VendorCategory)
                    .HasForeignKey(d => d.MainCategoryId)
                    .HasConstraintName("FK_VendorCategory_MainCategory");

                entity.HasOne(d => d.SubCategory)
                    .WithMany(p => p.VendorCategory)
                    .HasForeignKey(d => d.SubCategoryId)
                    .HasConstraintName("FK_VendorCategory_SubCategory");

                entity.HasOne(d => d.Vendor)
                    .WithMany(p => p.VendorCategory)
                    .HasForeignKey(d => d.VendorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VendorCategory_UserDetail");
            });

            modelBuilder.Entity<CustomerAddress>(entity =>
            {
                entity.Property(e => e.AddressLatitude).HasMaxLength(250);

                entity.Property(e => e.AddressLongitude).HasMaxLength(250);

                entity.Property(e => e.AddressType).HasMaxLength(50);

                entity.Property(e => e.AlternatePhoneNumber).HasMaxLength(50);

                entity.Property(e => e.City).HasMaxLength(100);

                entity.Property(e => e.CrateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.Property(e => e.FullName).HasMaxLength(500);

                entity.Property(e => e.HouseNoOrBuildingName).HasMaxLength(500);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PhoneNumber).HasMaxLength(50);

                entity.Property(e => e.Pincode).HasMaxLength(50);

                entity.Property(e => e.State).HasMaxLength(100);

                entity.HasOne(d => d.CustomerUser)
                    .WithMany(p => p.CustomerAddress)
                    .HasForeignKey(d => d.CustomerUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CustomerAddress_UserDetail");
            });

            modelBuilder.Entity<CustomerSalon>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CustomerUserId).HasMaxLength(450);

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.CustomerUser)
                    .WithMany(p => p.CustomerSalon)
                    .HasForeignKey(d => d.CustomerUserId)
                    .HasConstraintName("FK_Customer_UserDetail");

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.CustomerSalon)
                    .HasForeignKey(d => d.SalonId)
                    .HasConstraintName("FK_Customer_SalonDetail");
            });

            modelBuilder.Entity<BankDetail>(entity =>
            {
                entity.HasKey(e => e.BankId);

                entity.Property(e => e.BankAccountHolderName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.BankAccountNumber).HasMaxLength(100);

                entity.Property(e => e.BankName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.BranchName)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Ifsc)
                    .HasMaxLength(50)
                    .HasColumnName("IFSC");

                entity.Property(e => e.IsActive).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BankDetail)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_BankDetail_UserDetail");
            });

            modelBuilder.Entity<Banner>(entity =>
            {
                entity.Property(e => e.BannerType).HasMaxLength(50);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<CityMaster>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.HasOne(d => d.State)
                    .WithMany(p => p.CityMaster)
                    .HasForeignKey(d => d.StateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CityMaster_CityMaster");
            });

            modelBuilder.Entity<CountryMaster>(entity =>
            {
                entity.HasKey(e => e.CountryId);

                entity.Property(e => e.CountryCode).HasMaxLength(5);

                entity.Property(e => e.CountryName).HasMaxLength(50);

                entity.Property(e => e.Timezone).HasMaxLength(200);
            });

            modelBuilder.Entity<MainCategory>(entity =>
            {
                entity.Property(e => e.CategoryName).HasMaxLength(500);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreatedBy).HasMaxLength(450);

                entity.Property(e => e.ModifiedBy).HasMaxLength(450);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<MembershipPlan>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ExpiryDate).HasColumnType("datetime");

                entity.Property(e => e.GstinPercentage).HasColumnName("GSTInPercentage");

                entity.Property(e => e.Gsttax).HasColumnName("GSTTax");

                entity.Property(e => e.Gsttype)
                    .HasMaxLength(100)
                    .HasColumnName("GSTType");

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.PlanName).HasMaxLength(500);

                entity.Property(e => e.PlanType).HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<MembershipRecord>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreatedBy).HasMaxLength(450);

                entity.Property(e => e.ExpiryDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PaymentMethod).HasMaxLength(500);

                entity.Property(e => e.VendorId).HasMaxLength(450);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.MembershipRecord)
                    .HasForeignKey(d => d.CreatedBy)
                    .HasConstraintName("FK_MembershipRecord_UserDetail1");

                entity.HasOne(d => d.MembershipPlan)
                    .WithMany(p => p.MembershipRecord)
                    .HasForeignKey(d => d.MembershipPlanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MembershipRecord_UserDetail");

                entity.HasOne(d => d.PaymentReceipt)
                    .WithMany(p => p.MembershipRecord)
                    .HasForeignKey(d => d.PaymentReceiptId)
                    .HasConstraintName("FK_MembershipRecord_PaymentReceipt");
            });

            modelBuilder.Entity<PaymentReceipt>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.PaymentReceipt)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_PaymentReceipt_UserDetail");
            });

            modelBuilder.Entity<SalonBanner>(entity =>
            {
                entity.Property(e => e.BannerType).HasMaxLength(50);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.MainCategory)
                    .WithMany(p => p.SalonBanner)
                    .HasForeignKey(d => d.MainCategoryId)
                    .HasConstraintName("FK_ShopBanner_MainCategory");

                entity.HasOne(d => d.Salon)
                    .WithMany(p => p.SalonBanner)
                    .HasForeignKey(d => d.SalonId)
                    .HasConstraintName("FK_ShopBanner_SalonDetail");

                entity.HasOne(d => d.SubCategory)
                    .WithMany(p => p.SalonBanner)
                    .HasForeignKey(d => d.SubCategoryId)
                    .HasConstraintName("FK_ShopBanner_SubCategory");
            });

            modelBuilder.Entity<SalonDetail>(entity =>
            {
                entity.HasKey(e => e.SalonId)
                    .HasName("PK_salonDetails");

                entity.Property(e => e.AddressLatitude)
                    .HasMaxLength(250)
                    .HasDefaultValueSql("((30.696561151242133))");

                entity.Property(e => e.AddressLongitude)
                    .HasMaxLength(250)
                    .HasDefaultValueSql("((76.7870075375031))");

                entity.Property(e => e.BusinessPan)
                    .HasMaxLength(50)
                    .HasColumnName("BusinessPAN");

                entity.Property(e => e.City)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Gstnumber)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("GSTNumber");

                entity.Property(e => e.HomerServiceStatus).HasDefaultValueSql("((0))");

                entity.Property(e => e.InventoryAdded).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.Landmark).HasMaxLength(500);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PaymentQrcode).HasColumnName("PaymentQRCode");

                entity.Property(e => e.SalonName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.SalonType)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('Male')");

                entity.Property(e => e.Status).HasDefaultValueSql("((0))");

                entity.Property(e => e.UpidetailId).HasColumnName("UPIDetailId");

                entity.Property(e => e.Upiid).HasColumnName("UPIId");

                entity.Property(e => e.VendorId).HasMaxLength(450);

                entity.Property(e => e.Zip)
                    .HasMaxLength(50)
                    .HasColumnName("ZIP");

                entity.HasOne(d => d.Bank)
                    .WithMany(p => p.SalonDetail)
                    .HasForeignKey(d => d.BankId)
                    .HasConstraintName("FK_salonDetails_UserDetail");

                entity.HasOne(d => d.Upidetail)
                    .WithMany(p => p.SalonDetail)
                    .HasForeignKey(d => d.UpidetailId)
                    .HasConstraintName("FK_salonDetail_UPIDetail");
            });

            modelBuilder.Entity<StateMaster>(entity =>
            {
                entity.HasKey(e => e.StateId);

                entity.Property(e => e.StateName).HasMaxLength(50);
            });

            modelBuilder.Entity<SubCategory>(entity =>
            {
                entity.Property(e => e.CategoryName).HasMaxLength(500);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreatedBy).HasMaxLength(450);

                entity.Property(e => e.ModifiedBy).HasMaxLength(450);

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Upidetail>(entity =>
            {
                entity.ToTable("UPIDetail");

                entity.Property(e => e.UpidetailId).HasColumnName("UPIDetailId");

                entity.Property(e => e.AccountHolderName).HasMaxLength(500);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActive).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.Qrcode).HasColumnName("QRCode");

                entity.Property(e => e.Upiid).HasColumnName("UPIId");

                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Upidetail)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UPIDetail_UserDetail");
            });

            modelBuilder.Entity<UserDetail>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CreatedBy).HasMaxLength(450);

                entity.Property(e => e.DeviceType).HasMaxLength(100);

                entity.Property(e => e.DialCode).HasMaxLength(50);

                entity.Property(e => e.EmailOtp).HasColumnName("EmailOTP");

                entity.Property(e => e.Fcmtoken).HasColumnName("FCMToken");

                entity.Property(e => e.Gender)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.Pan)
                    .HasMaxLength(50)
                    .HasColumnName("PAN");
            });

            base.OnModelCreating(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }

}
