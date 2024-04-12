

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System.Linq;

#nullable disable

namespace OCPP.Core.Database
{
    public partial class OCPPCoreContext : DbContext
    {
        public OCPPCoreContext(DbContextOptions<OCPPCoreContext> options)
            : base(options)
        {
            
        }

        public virtual DbSet<ChargePoint> ChargePoints { get; set; }
        public virtual DbSet<ChargeTag> ChargeTags { get; set; }
        public virtual DbSet<ConnectorStatus> ConnectorStatuses { get; set; }
        public virtual DbSet<ConnectorStatusView> ConnectorStatusViews { get; set; }
        public virtual DbSet<MessageLog> MessageLogs { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Company> Companies { get; set; }

        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Companies");

                entity.Property(e => e.CompanyId)
                    .HasColumnName("CompanyId")
                    .IsRequired();

                entity.Property(e => e.Name)
                    .HasColumnName("Name")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Address)
                    .HasColumnName("Address")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Phone)
                    .HasColumnName("Phone")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.AdministratorId)
                    .HasColumnName("AdministratorId")
                    .IsRequired();

                entity.HasKey(e => e.CompanyId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.UserId)
                    .HasColumnName("UserId") 
                    .IsRequired(); 

                entity.Property(e => e.Username)
                    .HasColumnName("Username") 
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Password)
                    .HasColumnName("Password") 
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Role)
                    .HasColumnName("Role") 
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasKey(e => e.UserId); 
            });
        

            modelBuilder.Entity<ChargePoint>(entity =>
            {
                entity.ToTable("ChargePoint");

                entity.HasIndex(e => e.ChargePointId, "ChargePoint_Identifier")
                    .IsUnique();

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.Comment).HasMaxLength(200);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Username).HasMaxLength(50);

                entity.Property(e => e.Password).HasMaxLength(50);

                entity.Property(e => e.ClientCertThumb).HasMaxLength(100);

                entity.Property(e => e.CompanyId)
                    .HasColumnName("CompanyId")
                    .IsRequired();

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ChargePoint_Companies");
            });

            modelBuilder.Entity<ChargeTag>(entity =>
            {
                entity.HasKey(e => e.TagId)
                    .HasName("PK_ChargeKeys");

                entity.Property(e => e.TagId).HasMaxLength(50);

                entity.Property(e => e.ParentTagId).HasMaxLength(50);

                entity.Property(e => e.TagName).HasMaxLength(200);

                entity.Property(e => e.CompanyId)
                    .HasColumnName("CompanyId")
                    .IsRequired();

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.ChargeTags)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ChargeTag_Companies");
            });

            modelBuilder.Entity<ConnectorStatus>(entity =>
            {
                entity.HasKey(e => new { e.ChargePointId, e.ConnectorId });

                entity.ToTable("ConnectorStatus");

                entity.Property(e => e.ChargePointId).HasMaxLength(100);

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastStatus).HasMaxLength(100);

                entity.Property(e => e.CompanyId)
                    .HasColumnName("CompanyId")
                    .IsRequired();

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.ConnectorStatus)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConnectorStatus_Companies");
            });

            modelBuilder.Entity<ConnectorStatusView>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("ConnectorStatusView");

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastStatus).HasMaxLength(100);

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(50);

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(50);
            });

            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.ToTable("MessageLog");

                entity.HasIndex(e => e.LogTime, "IX_MessageLog_ChargePointId");

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ErrorCode).HasMaxLength(100);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Uid).HasMaxLength(50);

                entity.Property(e => e.ChargePointId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(50);

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(50);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_ChargePoint");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
