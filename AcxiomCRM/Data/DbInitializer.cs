using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Apply any pending migrations or ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Default Users
            var adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    EmailConfirmed = true,
                    FullName = "Sales Operations Manager",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            var salesEmail = "sales@acxiomcrm.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    EmailConfirmed = true,
                    FullName = "Alex Sales Executive",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
                }
            }

            // 3. Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customers = new[]
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Acme Global Corporation",
                        Email = "contact@acmeglobal.com",
                        Phone = "9876543210",
                        CompanyName = "Acme Global Corp",
                        Address = "100 Innovation Blvd, Suite 400",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-60),
                        CreatedBy = salesEmail
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Apex Innovations Pvt Ltd",
                        Email = "info@apexinno.com",
                        Phone = "9876543211",
                        CompanyName = "Apex Innovations",
                        Address = "52 Cyber City, Phase 2",
                        City = "Hyderabad",
                        State = "Telangana",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-45),
                        CreatedBy = salesEmail
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Vertex Cloud Systems",
                        Email = "sales@vertexcloud.com",
                        Phone = "9876543212",
                        CompanyName = "Vertex Cloud Systems",
                        Address = "12 Tech Park Avenue",
                        City = "Pune",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        CreatedBy = managerEmail
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Nexus Global Logistics",
                        Email = "operations@nexuslog.com",
                        Phone = "9876543213",
                        CompanyName = "Nexus Logistics",
                        Address = "70 Portway Industrial Zone",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedBy = salesEmail
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads
            if (!await context.Leads.AnyAsync())
            {
                var leads = new[]
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "Quantum Technologies",
                        Email = "leads@quantumtech.io",
                        Phone = "9123456780",
                        CompanyName = "Quantum Technologies",
                        Source = "Website",
                        Status = "Qualified",
                        ExpectedValue = 75000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedTo = salesEmail
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "BlueSky Digital Media",
                        Email = "contact@blueskymedia.com",
                        Phone = "9123456781",
                        CompanyName = "BlueSky Media",
                        Source = "Referral",
                        Status = "New",
                        ExpectedValue = 25000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        AssignedTo = salesEmail
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Global FinServe Group",
                        Email = "inquiries@globalfinserve.com",
                        Phone = "9123456782",
                        CompanyName = "Global FinServe",
                        Source = "Campaign",
                        Status = "Contacted",
                        ExpectedValue = 120000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        AssignedTo = managerEmail
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Starlight Retail Ventures",
                        Email = "retail@starlight.com",
                        Phone = "9123456783",
                        CompanyName = "Starlight Retail",
                        Source = "Cold Call",
                        Status = "Lost",
                        ExpectedValue = 15000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-40),
                        AssignedTo = salesEmail
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities
            if (!await context.Opportunities.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
                var cust3 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1003");
                var cust4 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1004");

                var opps = new[]
                {
                    new Opportunity
                    {
                        OpportunityName = "Enterprise CRM Digital Transformation",
                        CustomerId = cust1?.CustomerId ?? 1,
                        Amount = 150000.00m,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedTo = salesEmail
                    },
                    new Opportunity
                    {
                        OpportunityName = "Cloud Infrastructure Modernization",
                        CustomerId = cust2?.CustomerId ?? 2,
                        Amount = 85000.00m,
                        Stage = "Proposal",
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-14),
                        AssignedTo = salesEmail
                    },
                    new Opportunity
                    {
                        OpportunityName = "Annual SLA & Premium Support Tier",
                        CustomerId = cust3?.CustomerId ?? 3,
                        Amount = 45000.00m,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddDays(-50),
                        AssignedTo = managerEmail
                    },
                    new Opportunity
                    {
                        OpportunityName = "AI Predictive Supply Chain Integration",
                        CustomerId = cust4?.CustomerId ?? 4,
                        Amount = 95000.00m,
                        Stage = "Qualification",
                        Probability = 30,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(60),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        AssignedTo = salesEmail
                    }
                };

                context.Opportunities.AddRange(opps);
                await context.SaveChangesAsync();
            }

            // 6. Seed FollowUps
            if (!await context.FollowUps.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
                var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-1001");

                var followUps = new[]
                {
                    new FollowUp
                    {
                        CustomerId = cust1?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(1),
                        FollowUpType = "Meeting",
                        Remarks = "Executive presentation on commercial terms and rollout timeline",
                        Status = "Planned",
                        AssignedTo = salesEmail,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        CustomerId = cust2?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-2),
                        FollowUpType = "Call",
                        Remarks = "Urgent follow-up on technical architecture sign-off",
                        Status = "Planned", // Overdue
                        AssignedTo = salesEmail,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new FollowUp
                    {
                        LeadId = lead1?.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(3),
                        FollowUpType = "Email",
                        Remarks = "Send proposal deck and customer references",
                        Status = "Planned",
                        AssignedTo = salesEmail,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!await context.Activities.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var activities = new[]
                {
                    new Activity
                    {
                        ActivityType = "Call",
                        Subject = "Initial Discovery Session",
                        Description = "Discussed current bottlenecks with legacy CRM and user adoption goals.",
                        ActivityDate = DateTime.UtcNow.AddDays(-15),
                        CustomerId = cust1?.CustomerId,
                        AssignedTo = salesEmail,
                        Status = "Completed"
                    },
                    new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "Architecture Review",
                        Description = "Reviewed security compliance, SSO requirements, and role matrix.",
                        ActivityDate = DateTime.UtcNow.AddDays(-7),
                        CustomerId = cust1?.CustomerId,
                        AssignedTo = salesEmail,
                        Status = "Completed"
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Initial Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                context.AuditLogs.AddRange(
                    new AuditLog
                    {
                        UserId = "system@acxiomcrm.com",
                        Action = "Create",
                        EntityName = "System",
                        RecordId = "INIT",
                        NewValue = "Database seeded with default roles, credentials, and demo records.",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1"
                    },
                    new AuditLog
                    {
                        UserId = adminEmail,
                        Action = "Login",
                        EntityName = "Auth",
                        RecordId = adminEmail,
                        NewValue = "Administrator authenticated successfully.",
                        CreatedDate = DateTime.UtcNow.AddHours(-3),
                        IpAddress = "127.0.0.1"
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
