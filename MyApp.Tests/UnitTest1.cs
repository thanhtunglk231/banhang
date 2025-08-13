using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using webBanThucPham.ExtensionCode;
using System;

namespace webBanThucPham.Tests.Controllers
{
    public class AdminAccountsControllerTests
    {
        private AdminAccountsController GetControllerWithMockDb(List<Account> fakeAccounts)
        {
            var options = new DbContextOptionsBuilder<DbBanThucPhamContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isolate each test
                .Options;

            var context = new DbBanThucPhamContext(options);
            context.Accounts.AddRange(fakeAccounts);
            context.SaveChanges();

            var notyfMock = new Mock<AspNetCoreHero.ToastNotification.Abstractions.INotyfService>();
            var controller = new AdminAccountsController(notyfMock.Object, context);

            // Fake Session
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext = httpContext;

            return controller;
        }

        [Fact]
        public void Login_WithInvalidEmail_ReturnsViewWithError()
        {
            // Arrange
            var accounts = new List<Account>(); // Empty list = no users
            var controller = GetControllerWithMockDb(accounts);

            // Act
            var result = controller.Login("invalid@example.com", "123456") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("danger", result?.ViewData["Type"]);
            Assert.Equal("Tài kho?n không t?n t?i ho?c không có quy?n truy c?p.", result?.ViewData["Message"]);
        }

        [Fact]
        public void Login_WithWrongPassword_ReturnsError()
        {
            // Arrange
            var salt = "abc123";
            var correctPassword = "password123";
            var wrongPassword = "wrongpass";

            var user = new Account
            {
                Email = "admin@example.com",
                Password = (correctPassword + salt).ToMD5(),
                Salt = salt,
                Active = true,
                RoleId = 1
            };

            var controller = GetControllerWithMockDb(new List<Account> { user });

            // Act
            var result = controller.Login(user.Email, wrongPassword) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("danger", result?.ViewData["Type"]);
            Assert.Equal("Sai m?t kh?u.", result?.ViewData["Message"]);
        }

        [Fact]
        public void Login_WithCorrectCredentials_RedirectsToAdminHome()
        {
            // Arrange
            var salt = "salt";
            var password = "mypassword";
            var hashedPassword = (password + salt).ToMD5();

            var user = new Account
            {
                AccountId = 1,
                Email = "admin@example.com",
                Password = hashedPassword,
                Salt = salt,
                Active = true,
                FullName = "Admin",
                RoleId = 1
            };

            var controller = GetControllerWithMockDb(new List<Account> { user });

            // Act
            var result = controller.Login(user.Email, password) as RedirectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/Admin/Home/Index", result.Url);
        }
    }
}
