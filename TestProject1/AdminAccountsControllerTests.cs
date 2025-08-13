using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using Xunit;

namespace WebBanThucPham.Tests.Controllers
{
    public class AdminAccountsControllerTests
    {
        private DbBanThucPhamContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<DbBanThucPhamContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;

            var context = new DbBanThucPhamContext(options);

            // Thêm dữ liệu mẫu
            var salt = "xyz";
            var hashedPassword = Analyzer.Utilities.GetHash("abc" + salt);

            context.Accounts.Add(new Account
            {
                Email = "a@a.com",
                Password = hashedPassword,
                Salt = salt,
                Role = 1
            });

            context.SaveChanges();
            return context;
        }

        private ISession MockSession()
        {
            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, val) => { });
            sessionMock.Setup(s => s.Get(It.IsAny<string>()))
                .Returns((string key) => Encoding.UTF8.GetBytes("admin"));

            return sessionMock.Object;
        }

        [Fact]
        public void Login_ValidAccount_RedirectsToAdminHome()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var notyfMock = new Mock<INotyfService>();

            var controller = new AdminAccountsController(notyfMock.Object, context);

            // Gán session giả lập
            var httpContext = new DefaultHttpContext
            {
                Session = MockSession()
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Login("a@a.com", "abc");

            // Assert
            var redirect = Xunit.Assert.IsType<RedirectResult>(result);
            Xunit.Assert.Equal("/Admin/Home/Index", redirect.Url);
        }

        [Fact]
        public void Login_InvalidAccount_ReturnsLoginView()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var notyfMock = new Mock<INotyfService>();

            var controller = new AdminAccountsController(notyfMock.Object, context);

            var httpContext = new DefaultHttpContext
            {
                Session = MockSession()
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Login("wrong@a.com", "wrongpassword");

            // Assert
            var viewResult = Xunit.Assert.IsType<ViewResult>(result);
            Xunit.Assert.Equal("Login", viewResult.ViewName);
        }
    }
}
