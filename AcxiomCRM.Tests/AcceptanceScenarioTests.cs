using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;

namespace AcxiomCRM.Tests
{
    public class AcceptanceScenarioTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Test_Customer_RejectsDuplicateEmail()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var customerService = new CustomerService(context, auditService);

            var cust1 = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "Test Corp A",
                Email = "duplicate@test.com",
                Phone = "9876543210",
                Status = "Active"
            };
            await customerService.CreateAsync(cust1, "tester");

            // Act: Attempt to create customer with same email
            var cust2 = new Customer
            {
                CustomerCode = "CUST-002",
                CustomerName = "Test Corp B",
                Email = "duplicate@test.com",
                Phone = "9876543211",
                Status = "Active"
            };
            var (success, errorMsg, _) = await customerService.CreateAsync(cust2, "tester");

            // Assert
            Assert.False(success);
            Assert.Contains("email", errorMsg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test_Customer_RejectsDuplicatePhone()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var customerService = new CustomerService(context, auditService);

            var cust1 = new Customer
            {
                CustomerCode = "CUST-001",
                CustomerName = "Alpha Corp",
                Email = "alpha@test.com",
                Phone = "9998887776",
                Status = "Active"
            };
            await customerService.CreateAsync(cust1, "tester");

            // Act: Attempt to create customer with same phone
            var cust2 = new Customer
            {
                CustomerCode = "CUST-002",
                CustomerName = "Beta Corp",
                Email = "beta@test.com",
                Phone = "9998887776",
                Status = "Active"
            };
            var (success, errorMsg, _) = await customerService.CreateAsync(cust2, "tester");

            // Assert
            Assert.False(success);
            Assert.Contains("phone", errorMsg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test_Opportunity_RejectsZeroOrNegativeAmount()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "Zero Value Deal",
                CustomerId = 1,
                Amount = 0.00m,
                Probability = 50,
                Stage = "Qualification",
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var (success, errorMsg, _) = await oppService.CreateAsync(opp, "tester");

            // Assert
            Assert.False(success);
            Assert.Equal("Opportunity Amount must be greater than 0.", errorMsg);
        }

        [Fact]
        public async Task Test_Opportunity_RejectsProbabilityOutOfRange()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "120% Prob Deal",
                CustomerId = 1,
                Amount = 50000.00m,
                Probability = 120, // Invalid!
                Stage = "Qualification",
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var (success, errorMsg, _) = await oppService.CreateAsync(opp, "tester");

            // Assert
            Assert.False(success);
            Assert.Equal("Probability must be between 0 and 100.", errorMsg);
        }

        [Fact]
        public async Task Test_Opportunity_RejectsPastCloseDateForActiveOpportunity()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "Past Date Deal",
                CustomerId = 1,
                Amount = 50000.00m,
                Probability = 50,
                Stage = "Qualification", // Active
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2) // Past!
            };

            // Act
            var (success, errorMsg, _) = await oppService.CreateAsync(opp, "tester");

            // Assert
            Assert.False(success);
            Assert.Equal("Expected Close Date cannot be in the past.", errorMsg);
        }

        [Fact]
        public async Task Test_FollowUp_RejectsDateEarlierThanTodayForPlanned()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var followUpService = new FollowUpService(context, auditService);

            var followUp = new FollowUp
            {
                CustomerId = 1,
                FollowUpType = "Meeting",
                Status = "Planned",
                FollowUpDate = DateTime.UtcNow.AddDays(-1), // Past date!
                Remarks = "Past meeting"
            };

            // Act
            var (success, errorMsg, _) = await followUpService.CreateAsync(followUp, "tester");

            // Assert
            Assert.False(success);
            Assert.Equal("Follow-up date cannot be earlier than today.", errorMsg);
        }

        [Fact]
        public async Task Test_Lead_ConversionWorkflow_CreatesCustomerAndOpportunity()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var customerService = new CustomerService(context, auditService);
            var leadService = new LeadService(context, auditService, customerService);

            var lead = new Lead
            {
                LeadCode = "LEAD-101",
                LeadName = "Promising Enterprise",
                Email = "promising@enterprise.com",
                Phone = "9811223344",
                CompanyName = "Enterprise Holdings",
                Status = "Qualified",
                ExpectedValue = 90000.00m,
                AssignedTo = "sales@acxiomcrm.com"
            };
            var createLeadResult = await leadService.CreateAsync(lead, "sales@acxiomcrm.com");
            Assert.True(createLeadResult.Success);

            // Act: Convert Lead
            var (convertSuccess, errorMsg, custId, oppId) = await leadService.ConvertToCustomerAsync(lead.LeadId, "sales@acxiomcrm.com");

            // Assert
            Assert.True(convertSuccess);
            Assert.NotNull(custId);
            Assert.NotNull(oppId);

            var createdCustomer = await context.Customers.FindAsync(custId.Value);
            Assert.NotNull(createdCustomer);
            Assert.Equal("Promising Enterprise", createdCustomer.CustomerName);

            var createdOpp = await context.Opportunities.FindAsync(oppId.Value);
            Assert.NotNull(createdOpp);
            Assert.Equal(90000.00m, createdOpp.Amount);
            Assert.Equal(custId.Value, createdOpp.CustomerId);

            var updatedLead = await context.Leads.FindAsync(lead.LeadId);
            Assert.Equal("Converted", updatedLead!.Status);

            // Verify Audit Log was generated
            var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "Convert" && a.RecordId == lead.LeadId.ToString());
            Assert.NotNull(audit);
        }

        [Fact]
        public async Task Test_Opportunity_WeightedValueCalculation()
        {
            var opp = new Opportunity
            {
                OpportunityName = "Sample Deal",
                Amount = 100000.00m,
                Probability = 75
            };

            // WeightedValue = Amount * (Probability / 100)
            Assert.Equal(75000.00m, opp.WeightedValue);
        }

        [Fact]
        public async Task Test_RoleScoping_SalesExecutiveOnlySeesAssignedRecords()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var auditService = new AuditService(context);
            var customerService = new CustomerService(context, auditService);

            var cust1 = new Customer { CustomerCode = "C-1", CustomerName = "Sales Rep Cust", Email = "c1@rep.com", Phone = "1111111111", CreatedBy = "rep1@crm.com" };
            var cust2 = new Customer { CustomerCode = "C-2", CustomerName = "Other Rep Cust", Email = "c2@rep.com", Phone = "2222222222", CreatedBy = "rep2@crm.com" };

            context.Customers.AddRange(cust1, cust2);
            await context.SaveChangesAsync();

            // Act: Query as SalesExecutive rep1
            var rep1Customers = await customerService.GetAllAsync(currentUserId: "rep1@crm.com", userRole: "SalesExecutive");
            // Act: Query as Manager
            var managerCustomers = await customerService.GetAllAsync(currentUserId: "manager@crm.com", userRole: "Manager");

            // Assert
            Assert.Single(rep1Customers);
            Assert.Equal("rep1@crm.com", rep1Customers.First().CreatedBy);

            Assert.Equal(2, managerCustomers.Count);
        }
    }
}
