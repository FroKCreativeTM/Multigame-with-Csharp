using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace SharedDB
{
	public class SharedDbContext : DbContext
	{
		public DbSet<TokenDb> Tokens { get; set; }
		public DbSet<ServerDb> Servers { get; set; }

		// GameServer
		public SharedDbContext()
		{

		}

		// ASP.NET
		public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
		{

		}

		// GameServer
		public static string ConnectionString { get; set; } = @"server=localhost;database=shareddb;uid=root;password=keAarwrm76*;";
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			if (options.IsConfigured == false)
			{
				options
					.UseMySql(connectionString: ConnectionString,
					new MySqlServerVersion(new Version(10, 4, 17)));
			}
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.Entity<TokenDb>()
				.HasIndex(t => t.AccountDbId)
				.IsUnique();

			builder.Entity<ServerDb>()
				.HasIndex(s => s.Name)
				.IsUnique();
		}
	}
}
