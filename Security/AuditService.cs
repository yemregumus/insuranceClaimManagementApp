using System;
using System.Security.Claims;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Http;

namespace InsuranceClaimManagement.Security
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            ApplicationDbContext context,
            ICurrentOrganization currentOrganization,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _currentOrganization = currentOrganization;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Record(string eventType, string entityType, string entityId)
        {
            var actorUserId = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                throw new InvalidOperationException("An authenticated staff account is required to record an audit event.");
            }

            _context.AuditEvents.Add(new AuditEvent
            {
                OrganizationId = _currentOrganization.GetRequiredOrganizationId(),
                ActorUserId = actorUserId,
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                OccurredUtc = DateTime.UtcNow
            });
        }
    }
}
