using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CustomerCreateModel = InsuranceClaimManagement.Pages.Users.CreateModel;
using CustomerDeleteModel = InsuranceClaimManagement.Pages.Users.DeleteModel;
using CustomerEditModel = InsuranceClaimManagement.Pages.Users.EditModel;

namespace InsuranceClaimManagement.Tests.Customers
{
    public class CustomerManagementPageTests
    {
        [Fact]
        public async Task CreateRejectsMissingAndDuplicateCustomerDetails()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.Add(Customer(1, "existing@example.test", "REF-1"));
            await context.SaveChangesAsync();
            var page = new CustomerCreateModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                new RecordingAuditService());
            TestInfrastructure.InitializePage(page);

            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.ModelState.IsValid);

            page.ModelState.Clear();
            page.Input = new CustomerInputModel
            {
                Name = "Duplicate",
                Email = "existing@example.test",
                Username = "REF-1"
            };
            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.NotEmpty(page.ModelState["Input.Email"].Errors);
            Assert.NotEmpty(page.ModelState["Input.Username"].Errors);
        }

        [Fact]
        public async Task CreatePersistsTrimmedCustomerAndAudit()
        {
            await using var context = TestInfrastructure.CreateContext();
            var audit = new RecordingAuditService();
            var page = new CustomerCreateModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                audit)
            {
                Input = new CustomerInputModel
                {
                    Name = " Customer Name ",
                    Email = " customer@example.test ",
                    Username = " REF-22 "
                }
            };
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var customer = await context.Customers.SingleAsync();
            Assert.Equal("Customer Name", customer.Name);
            Assert.Equal("customer@example.test", customer.Email);
            Assert.Equal("REF-22", customer.Username);
            Assert.Equal(TestInfrastructure.OrganizationId, customer.OrganizationId);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.Created);
        }

        [Fact]
        public async Task EditLoadsCustomerAndRejectsDuplicateDetails()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.AddRange(
                Customer(1, "one@example.test", "REF-1"),
                Customer(2, "two@example.test", "REF-2"));
            await context.SaveChangesAsync();
            var page = new CustomerEditModel(context, new RecordingAuditService());

            Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
            Assert.IsType<PageResult>(await page.OnGetAsync(1));
            Assert.Equal("one@example.test", page.Input.Email);

            page.Input = new CustomerInputModel
            {
                Name = "One",
                Email = "two@example.test",
                Username = "REF-2"
            };
            Assert.IsType<PageResult>(await page.OnPostAsync(1));
            Assert.NotEmpty(page.ModelState["Input.Email"].Errors);
            Assert.NotEmpty(page.ModelState["Input.Username"].Errors);
        }

        [Fact]
        public async Task EditUpdatesCustomerAndRecordsAudit()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.Add(Customer(1, "one@example.test", "REF-1"));
            await context.SaveChangesAsync();
            var audit = new RecordingAuditService();
            var page = new CustomerEditModel(context, audit)
            {
                Input = new CustomerInputModel
                {
                    Name = " Updated ",
                    Email = " updated@example.test ",
                    Username = " UPDATED-1 "
                }
            };

            var result = await page.OnPostAsync(1);

            Assert.IsType<RedirectToPageResult>(result);
            var customer = await context.Customers.SingleAsync();
            Assert.Equal("Updated", customer.Name);
            Assert.Equal("updated@example.test", customer.Email);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.Updated);
        }

        [Fact]
        public async Task DeleteBlocksCustomersWithActiveOrArchivedClaims()
        {
            await using var context = TestInfrastructure.CreateContext();
            var customer = Customer(1, "customer@example.test", "REF-1");
            context.Customers.Add(customer);
            context.Claims.Add(new Claim
            {
                Id = 8,
                OrganizationId = TestInfrastructure.OrganizationId,
                UserId = customer.Id,
                ClaimType = "Health",
                Description = "Archived claim",
                PolicyNumber = "POL-8",
                Amount = 10,
                Status = ClaimStatuses.Closed,
                IsDeleted = true
            });
            await context.SaveChangesAsync();
            var page = new CustomerDeleteModel(context, new RecordingAuditService());

            Assert.IsType<PageResult>(await page.OnGetAsync(customer.Id));
            Assert.Equal(1, page.RelatedClaimCount);
            var result = await page.OnPostAsync(customer.Id);

            Assert.IsType<PageResult>(result);
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage.Contains("claims are associated"));
            Assert.Single(await context.Customers.ToListAsync());
        }

        [Fact]
        public async Task DeleteRemovesCustomerWithoutClaimsAndRecordsAudit()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.Add(Customer(1, "customer@example.test", "REF-1"));
            await context.SaveChangesAsync();
            var audit = new RecordingAuditService();
            var page = new CustomerDeleteModel(context, audit);

            Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
            Assert.IsType<NotFoundResult>(await page.OnPostAsync(999));
            Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(1));
            Assert.Empty(await context.Customers.ToListAsync());
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.Archived);
        }

        private static User Customer(int id, string email, string reference)
        {
            return new User
            {
                Id = id,
                OrganizationId = TestInfrastructure.OrganizationId,
                Name = $"Customer {id}",
                Email = email,
                Username = reference
            };
        }
    }
}
