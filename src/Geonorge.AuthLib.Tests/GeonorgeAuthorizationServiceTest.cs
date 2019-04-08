using FluentAssertions;
using Geonorge.AuthLib.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Geonorge.AuthLib.Tests
{
    public class GeonorgeAuthorizationServiceTest
    {
        private const string Username = "johndoe";

        [Fact]
        public async Task ShouldReturnClaimsForUser()
        {
            var baatMock = new Mock<IBaatAuthzApi>();
            var response = new BaatAuthzUserInfoResponse()
            {
                Email = "johndoe@example.com",
                Name = "John Doe",
                User = Username,
                AuthorizedFrom = "20090101",
                AuthorizedUntil = "20301231"
            };
            baatMock.Setup(b => b.Info(Username)).ReturnsAsync(response);

            var rolesResponse = new BaatAuthzUserRolesResponse
            {
                Services = new List<string>
                {
                    GeonorgeRoles.MetadataAdmin, GeonorgeRoles.MetadataEditor
                }
            };
            baatMock.Setup(b => b.GetRoles(Username)).ReturnsAsync(rolesResponse);

            var authorizationService = new GeonorgeAuthorizationService(baatMock.Object);
            ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>() {
                new Claim(GeonorgeAuthorizationService.ClaimIdentifierUsername, Username)
                });

            List<Claim> claims = await authorizationService.GetClaims(identity);
            GetValue(claims, GeonorgeClaims.Name).Should().Be("John Doe");
            GetValue(claims, GeonorgeClaims.Email).Should().Be("johndoe@example.com");
            GetValue(claims, GeonorgeClaims.AuthorizedFrom).Should().Be("20090101");
            GetValue(claims, GeonorgeClaims.AuthorizedUntil).Should().Be("20301231");

            List<Claim> roles = claims.FindAll(c => c.Type == GeonorgeAuthorizationService.ClaimIdentifierRole);
            roles.FirstOrDefault(r => r.Value == GeonorgeRoles.MetadataAdmin).Should().NotBeNull();
            roles.FirstOrDefault(r => r.Value == GeonorgeRoles.MetadataEditor).Should().NotBeNull();
        }

        [Fact]
        public async Task ShouldReturnUserWithoutRolesWhenNoRolesFound()
        {
            var baatMock = new Mock<IBaatAuthzApi>();
            var response = new BaatAuthzUserInfoResponse()
            {
                Email = "johndoe@example.com",
                Name = "John Doe",
                User = Username,
                AuthorizedFrom = "20090101",
                AuthorizedUntil = "20301231"
            };
            baatMock.Setup(b => b.Info(Username)).ReturnsAsync(response);

            var rolesResponse = new BaatAuthzUserRolesResponse
            {
                Services = new List<string>()
            };
            baatMock.Setup(b => b.GetRoles(Username)).ReturnsAsync(rolesResponse);

            var authorizationService = new GeonorgeAuthorizationService(baatMock.Object);
            ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>() {
                new Claim(GeonorgeAuthorizationService.ClaimIdentifierUsername, Username)
            });

            List<Claim> claims = await authorizationService.GetClaims(identity);
            GetValue(claims, GeonorgeClaims.Name).Should().Be("John Doe");
            GetValue(claims, GeonorgeClaims.Email).Should().Be("johndoe@example.com");
            GetValue(claims, GeonorgeClaims.AuthorizedFrom).Should().Be("20090101");
            GetValue(claims, GeonorgeClaims.AuthorizedUntil).Should().Be("20301231");

            List<Claim> roles = claims.FindAll(c => c.Type == GeonorgeAuthorizationService.ClaimIdentifierRole);
            roles.Any().Should().BeFalse();
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenServiceFails()
        {
            var baatMock = new Mock<IBaatAuthzApi>();

            baatMock.Setup(b => b.Info(Username)).ThrowsAsync(new Exception("http 500 error"));

            var authorizationService = new GeonorgeAuthorizationService(baatMock.Object);
            ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>() {
                new Claim(GeonorgeAuthorizationService.ClaimIdentifierUsername, Username)
            });

            var exception = await Assert.ThrowsAsync<Exception>(() => authorizationService.GetClaims(identity));
            exception.Message.Contains("Error while communicating with BaatAutzApi").Should().BeTrue();
        }

        private static string GetValue(List<Claim> claims, string typeName)
        {
            return claims.FirstOrDefault(c => c.Type == typeName)?.Value;
        }
    }
}