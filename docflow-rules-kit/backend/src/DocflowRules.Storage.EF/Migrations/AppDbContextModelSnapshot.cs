using System;
using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DocflowRules.Storage.EF.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("DocflowRules.Storage.EF.RuleFunction", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd();
                    b.Property<string>("Code").IsRequired();
                    b.Property<string>("CodeHash").IsRequired().HasMaxLength(128);
                    b.Property<string>("Description");
                    b.Property<bool>("Enabled");
                    b.Property<bool>("IsBuiltin");
                    b.Property<string>("Name").IsRequired();
                    b.Property<string>("Owner");
                    b.Property<string>("ReadsCsv");
                    b.Property<DateTimeOffset>("UpdatedAt");
                    b.Property<string>("Version").IsRequired();
                    b.Property<string>("WritesCsv");
                    b.HasKey("Id");
                    b.HasIndex("Name").IsUnique();
                    b.ToTable("RuleFunctions");
                });

            modelBuilder.Entity("DocflowRules.Storage.EF.RuleTestCase", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd();
                    b.Property<string>("ExpectJson").IsRequired();
                    b.Property<string>("InputJson").IsRequired();
                    b.Property<string>("Name").IsRequired();
                    b.Property<int>("Priority");
                    b.Property<Guid>("RuleFunctionId");
                    b.Property<string>("Suite");
                    b.Property<string>("TagsCsv");
                    b.Property<DateTimeOffset>("UpdatedAt");
                    b.HasKey("Id");
                    b.HasIndex("RuleFunctionId", "Name").IsUnique();
                    b.ToTable("RuleTestCases");
                });

            modelBuilder.Entity("DocflowRules.Storage.EF.TestSuite", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd();
                    b.Property<string>("Color");
                    b.Property<string>("Description");
                    b.Property<string>("Name").IsRequired();
                    b.Property<DateTimeOffset>("UpdatedAt");
                    b.HasKey("Id");
                    b.HasIndex("Name").IsUnique();
                    b.ToTable("TestSuites");
                });

            modelBuilder.Entity("DocflowRules.Storage.EF.TestTag", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd();
                    b.Property<string>("Color");
                    b.Property<string>("Description");
                    b.Property<string>("Name").IsRequired();
                    b.Property<DateTimeOffset>("UpdatedAt");
                    b.HasKey("Id");
                    b.HasIndex("Name").IsUnique();
                    b.ToTable("TestTags");
                });

            modelBuilder.Entity("DocflowRules.Storage.EF.RuleTestCaseTag", b =>
                {
                    b.Property<Guid>("RuleTestCaseId");
                    b.Property<Guid>("TestTagId");
                    b.HasKey("RuleTestCaseId", "TestTagId");
                    b.HasIndex("TestTagId");
                    b.ToTable("RuleTestCaseTags");
                });
#pragma warning restore 612, 618
        }
    }
}
