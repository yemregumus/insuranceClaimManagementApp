using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models; // Importing the models for database mapping
using Microsoft.EntityFrameworkCore; // Required for DbContext and Entity Framework Core functionality

namespace InsuranceClaimManagement.Data
{
    /// <summary>
    /// Represents the database context for the Insurance Claim Management application.
    /// Manages connections to the database and provides access to the application's data models.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext class with the specified options.
        /// </summary>
        /// <param name="options">
        /// A DbContextOptions object containing configuration information for the database connection.
        /// Typically provided through dependency injection.
        /// </param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            // Calls the base DbContext constructor with the given options
        }

        /// <summary>
        /// Gets or sets the Users table in the database.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the Claims table in the database.
        /// </summary>
        public DbSet<Claim> Claims { get; set; }
    }
}
