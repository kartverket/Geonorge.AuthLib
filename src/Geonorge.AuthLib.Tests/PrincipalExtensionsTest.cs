using FluentAssertions;
using Geonorge.AuthLib.Common;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Geonorge.AuthLib.Tests
{
    public class PrincipalExtensionsTest
    {

        [Fact]
        public void ShouldReturnUsername()
        {
            var username = "johndoe";
            var principal = CreatePrincipal(GeonorgeAuthorizationService.ClaimIdentifierUsername, username);

            principal.GetUsername().Should().Be(username);
        }

        [Fact]
        public void ShouldReturnEmail()
        {
            var email = "johndoe@example.com";
            var principal = CreatePrincipal(GeonorgeClaims.Email, email);

            principal.GetUserEmail().Should().Be(email);
        }

        [Fact]
        public void ShouldReturnFullName()
        {
            var name = "John Doe";
            var principal = CreatePrincipal(GeonorgeClaims.Name, name);

            principal.GetUserFullName().Should().Be(name);
        }

        [Fact]
        public void ShouldReturnOrganizationName()
        {
            var name = "My Company";
            var principal = CreatePrincipal(GeonorgeClaims.OrganizationName, name);

            principal.GetOrganizationName().Should().Be(name);
        }

        [Fact]
        public void ShouldReturnOrganizationOrgnr()
        {
            var name = "99887766";
            var principal = CreatePrincipal(GeonorgeClaims.OrganizationOrgnr, name);

            principal.GetOrganizationOrgnr().Should().Be(name);
        }

        private static ClaimsPrincipal CreatePrincipal(string claimType, string claimValue)
        {
            ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>()
            {
                new Claim(claimType, claimValue)
            });
            var principal = new ClaimsPrincipal(identity);
            return principal;
        }
    }
}
