﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalRadio.Database.EF;

#nullable disable

namespace SignalRadio.Database.EF.Migrations
{
    [DbContext(typeof(SignalRadioDbContext))]
    [Migration("20220601210310_InitialDeploy")]
    partial class InitialDeploy
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.MountPoint", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Host")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Port")
                        .HasColumnType("bigint");

                    b.Property<long>("StreamId")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("MountPoints", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioCall", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("CallIdentifier")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("CallRecordState")
                        .HasColumnType("int");

                    b.Property<long>("CallSerialNumber")
                        .HasColumnType("bigint");

                    b.Property<int>("CallState")
                        .HasColumnType("int");

                    b.Property<string>("CallWavPath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Elapsed")
                        .HasColumnType("bigint");

                    b.Property<long>("Frequency")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsAnalog")
                        .HasColumnType("bit");

                    b.Property<bool>("IsConventional")
                        .HasColumnType("bit");

                    b.Property<bool>("IsEmergency")
                        .HasColumnType("bit");

                    b.Property<bool>("IsEncrypted")
                        .HasColumnType("bit");

                    b.Property<bool>("IsPhase2")
                        .HasColumnType("bit");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("StopTime")
                        .HasColumnType("datetime2");

                    b.Property<long>("TalkGroupId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TalkGroupId");

                    b.ToTable("RadioCalls", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioFrequency", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<bool>("ControlData")
                        .HasColumnType("bit");

                    b.Property<decimal>("FrequencyHz")
                        .HasColumnType("decimal(20,0)");

                    b.Property<long>("RadioSystemId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("RadioSystemId");

                    b.ToTable("RadioFrequencies", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioGroup", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("RadioSystemId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("RadioSystemId");

                    b.ToTable("RadioGroups", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioRecorder", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<long>("Count")
                        .HasColumnType("bigint");

                    b.Property<float>("Duration")
                        .HasColumnType("real");

                    b.Property<string>("RecorderIdentifier")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RecorderNumber")
                        .HasColumnType("int");

                    b.Property<int>("SourceNumber")
                        .HasColumnType("int");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<int>("StatusError")
                        .HasColumnType("int");

                    b.Property<long>("StatusLength")
                        .HasColumnType("bigint");

                    b.Property<int>("StatusSpike")
                        .HasColumnType("int");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.ToTable("RadioRecorders", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioSystem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("City")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("County")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastUpdatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("NAC")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ShortName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("State")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SystemNumber")
                        .HasColumnType("int");

                    b.Property<byte>("SystemType")
                        .HasColumnType("tinyint");

                    b.Property<int>("SystemVoice")
                        .HasColumnType("int");

                    b.Property<int>("WANC")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("RadioSystems", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.Stream", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Genra")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastCallTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<long?>("MountPointId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("OwnerUserId")
                        .HasColumnType("bigint");

                    b.Property<string>("StreamIdentifier")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MountPointId")
                        .IsUnique()
                        .HasFilter("[MountPointId] IS NOT NULL");

                    b.HasIndex("OwnerUserId");

                    b.ToTable("Streams", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.TalkGroup", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("AlphaTag")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Identifier")
                        .HasColumnType("int");

                    b.Property<byte>("Mode")
                        .HasColumnType("tinyint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("RadioGroupId")
                        .HasColumnType("bigint");

                    b.Property<long?>("RadioSystemId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Tag")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.HasIndex("RadioGroupId");

                    b.HasIndex("RadioSystemId");

                    b.ToTable("TalkGroups", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.TalkGroupStream", b =>
                {
                    b.Property<long>("TalkGroupId")
                        .HasColumnType("bigint");

                    b.Property<long>("StreamId")
                        .HasColumnType("bigint");

                    b.HasKey("TalkGroupId", "StreamId");

                    b.HasIndex("StreamId");

                    b.ToTable("TalkGroupStreams", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("EmailAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users", "dbo");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.MountPoint", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.User", "User")
                        .WithMany("MountPoints")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioCall", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.TalkGroup", "TalkGroup")
                        .WithMany("RadioCalls")
                        .HasForeignKey("TalkGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TalkGroup");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioFrequency", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.RadioSystem", "RadioSystem")
                        .WithMany("ControlFrequencies")
                        .HasForeignKey("RadioSystemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RadioSystem");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioGroup", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.RadioSystem", "RadioSystem")
                        .WithMany("RadioGroups")
                        .HasForeignKey("RadioSystemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RadioSystem");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.Stream", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.MountPoint", "Mount")
                        .WithOne("Stream")
                        .HasForeignKey("SignalRadio.Public.Lib.Models.Stream", "MountPointId");

                    b.HasOne("SignalRadio.Public.Lib.Models.User", "Owner")
                        .WithMany("Streams")
                        .HasForeignKey("OwnerUserId");

                    b.Navigation("Mount");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.TalkGroup", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.RadioGroup", "RadioGroup")
                        .WithMany("TalkGroups")
                        .HasForeignKey("RadioGroupId");

                    b.HasOne("SignalRadio.Public.Lib.Models.RadioSystem", "RadioSystem")
                        .WithMany("TalkGroups")
                        .HasForeignKey("RadioSystemId");

                    b.Navigation("RadioGroup");

                    b.Navigation("RadioSystem");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.TalkGroupStream", b =>
                {
                    b.HasOne("SignalRadio.Public.Lib.Models.Stream", "Stream")
                        .WithMany("StreamTalkGroups")
                        .HasForeignKey("StreamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SignalRadio.Public.Lib.Models.TalkGroup", "TalkGroup")
                        .WithMany("TalkGroupStreams")
                        .HasForeignKey("TalkGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Stream");

                    b.Navigation("TalkGroup");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.MountPoint", b =>
                {
                    b.Navigation("Stream");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioGroup", b =>
                {
                    b.Navigation("TalkGroups");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.RadioSystem", b =>
                {
                    b.Navigation("ControlFrequencies");

                    b.Navigation("RadioGroups");

                    b.Navigation("TalkGroups");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.Stream", b =>
                {
                    b.Navigation("StreamTalkGroups");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.TalkGroup", b =>
                {
                    b.Navigation("RadioCalls");

                    b.Navigation("TalkGroupStreams");
                });

            modelBuilder.Entity("SignalRadio.Public.Lib.Models.User", b =>
                {
                    b.Navigation("MountPoints");

                    b.Navigation("Streams");
                });
#pragma warning restore 612, 618
        }
    }
}
