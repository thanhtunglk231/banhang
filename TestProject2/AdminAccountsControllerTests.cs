using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using webBanThucPham.Models.ViewModel;
using Xunit;
using webBanThucPham.ExtensionCode;
using webBanThucPham.Tests.Helpers;
using MockQueryable.Moq;
using Newtonsoft.Json;
namespace webBanThucPham.Tests.Controllers
{
    public class AdminAccountsControllerTests
    {
        private readonly Mock<INotyfService> _notyfMock;
        private readonly Mock<DbBanThucPhamContext> _contextMock;
        private readonly Mock<DbSet<Account>> _accountDbSetMock;
        private readonly Mock<DbSet<Role>> _roleDbSetMock;
        private readonly Mock<ISession> _sessionMock;
        private readonly DefaultHttpContext _httpContext;
        private readonly AdminAccountsController _controller;

        public AdminAccountsControllerTests()
        {
            _notyfMock = new Mock<INotyfService>();
            _contextMock = new Mock<DbBanThucPhamContext>();
            _accountDbSetMock = new Mock<DbSet<Account>>();
            _roleDbSetMock = new Mock<DbSet<Role>>();
            _sessionMock = new Mock<ISession>();
            _httpContext = new DefaultHttpContext { Session = _sessionMock.Object };

            _contextMock.Setup(c => c.Accounts).Returns(_accountDbSetMock.Object);
            _contextMock.Setup(c => c.Roles).Returns(_roleDbSetMock.Object);

            _controller = new AdminAccountsController(_notyfMock.Object, _contextMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
        }

        private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
        {
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        }

        private void SetupSessionMock()
        {
            _sessionMock.Setup(s => s.SetInt32(It.IsAny<string>(), It.IsAny<int>())).Verifiable();
            _sessionMock.Setup(s => s.SetString(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
            _sessionMock.Setup(s => s.GetString(It.IsAny<string>())).Returns<string>(key => null);
        }
        [Fact]
        public async Task Index_ReturnsViewWithAccounts()
        {
            // Arrange
            var roles = new List<Role>
        {
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "User" }
        };

            var accounts = new List<Account>
        {
            new Account { AccountId = 1, FullName = "User 1", RoleId = 1, Role = roles[0] },
            new Account { AccountId = 2, FullName = "User 2", RoleId = 2, Role = roles[1] }
        }.AsQueryable();

            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Roles).Returns(roles.AsQueryable().BuildMockDbSet().Object);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Account>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Quản lí tài khoản", viewResult.ViewData["CurrentPage"]);
        }
        [Fact]
        public async Task Login_WithCorrectCredentials_RedirectsToAdminHome()
        {
            var salt = Guid.NewGuid().ToString();
            var password = "123";
            var hashedPassword = (password + salt).ToMD5();

            var account = new Account
            {
                AccountId = 1,
                Email = "admin@example.com",
                Password = hashedPassword,
                Salt = salt,
                RoleId = 1,
                FullName = "Admin",
                Active = true
            };

            var accounts = new List<Account> { account }.AsQueryable();
            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Update(It.IsAny<Account>()));
            _contextMock.Setup(c => c.SaveChanges()).Returns(1);

            // Act
            var result = await _controller.Login("admin@example.com", "123");

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Admin/Home/Index", redirect.Url);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsViewWithError()
        {
            var accounts = new List<Account>().AsQueryable();
            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);

            var result = await _controller.Login("email@test.com", "wrongpass");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Tài khoản không tồn tại hoặc không có quyền truy cập.", viewResult.ViewData["Message"]);
            Assert.Equal("danger", viewResult.ViewData["Type"]);
        }

        [Fact]
        public async Task Login_InactiveAccount_ReturnsViewWithError()
        {
            var salt = Guid.NewGuid().ToString();
            var password = "123";
            var hashedPassword = (password + salt).ToMD5();

            var account = new Account
            {
                AccountId = 1,
                Email = "admin@example.com",
                Password = hashedPassword,
                Salt = salt,
                RoleId = 1,
                FullName = "Admin",
                Active = false
            };

            var accounts = new List<Account> { account }.AsQueryable();
            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);

            var result = await _controller.Login("admin@example.com", "123");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Tài khoản đang bị vô hiệu hóa.", viewResult.ViewData["Message"]);
            Assert.Equal("warning", viewResult.ViewData["Type"]);
        }


        [Fact]
        public async Task SendOtp_ValidEmail_ReturnsSuccess()
        {
            // Arrange
            var accounts = new List<Account>
    {
        new Account { Email = "email@test.com", RoleId = 1, Active = true }
    }.AsQueryable();

            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);

            _sessionMock.Setup(s => s.Remove(It.IsAny<string>())).Verifiable();
            // Không cần Setup cho SetString vì là extension method

            // Act
            var result = await _controller.SendOtp(new EmailViewModel { Email = "email@test.com" });

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var jsonStr = JsonConvert.SerializeObject(json.Value);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);

            Assert.Equal("True", data["success"].ToString());
            Assert.Equal("Mã OTP đã được gửi.", data["message"].ToString());
            Assert.Equal("300", data["expiresIn"].ToString());

            _sessionMock.Verify(s => s.Remove("OtpCode_Admin"), Times.Once());
            _sessionMock.Verify(s => s.Remove("OtpEmail_Admin"), Times.Once());
            _sessionMock.Verify(s => s.Remove("OtpTime_Admin"), Times.Once());
        }
        [Fact]
        public async Task SendOtp_InvalidEmail_ReturnsError()
        {
            // Arrange
            var accounts = new List<Account>().AsQueryable();
            _contextMock.Setup(c => c.Accounts).Returns(accounts.BuildMockDbSet().Object);

            // Act
            var result = await _controller.SendOtp(new EmailViewModel { Email = "nonexist@test.com" });

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var jsonStr = JsonConvert.SerializeObject(json.Value);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);

            Assert.Equal("False", data["success"].ToString());
            Assert.Equal("Tài khoản không tồn tại hoặc chưa được kích hoạt.", data["message"].ToString());
        }


        [Fact]
        public void VerifyOtp_ValidOtp_ReturnsSuccess()
        {
            // Arrange
            var account = new Account
            {
                Email = "email@test.com",
                RoleId = 1,
                FullName = "Test User",
                AccountId = 1
            };
            var accounts = new List<Account> { account }.AsQueryable();
            SetupMockDbSet(_accountDbSetMock, accounts);

            _sessionMock.Setup(s => s.GetString("OtpCode_Admin")).Returns("123456");
            _sessionMock.Setup(s => s.GetString("OtpEmail_Admin")).Returns("email@test.com");
            _sessionMock.Setup(s => s.GetString("OtpTime_Admin")).Returns(DateTime.UtcNow.ToString("o"));

            SetupSessionMock();

            _contextMock.Setup(c => c.Accounts).Returns(_accountDbSetMock.Object);
            _contextMock.Setup(c => c.Update(It.IsAny<Account>())).Verifiable();
            _contextMock.Setup(c => c.SaveChanges()).Verifiable();

            var otpViewModel = new OtpViewModel { Email = "email@test.com", Otp = "123456" };

            // Act
            var result = _controller.VerifyOtp(otpViewModel);

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(json.Value));

            Assert.True((bool)data["success"]);
            Assert.Equal("Đăng nhập thành công!", data["message"]);

            _sessionMock.Verify(s => s.SetInt32("AdminId", account.AccountId), Times.Once);
            _sessionMock.Verify(s => s.SetString("AdminEmail", account.Email), Times.Once);
            _sessionMock.Verify(s => s.SetString("AdminName", account.FullName), Times.Once);
            _sessionMock.Verify(s => s.SetInt32("RoleId", (int)account.RoleId), Times.Once);
            _sessionMock.Verify(s => s.Remove("OtpCode_Admin"), Times.Once);
            _sessionMock.Verify(s => s.Remove("OtpEmail_Admin"), Times.Once);
            _sessionMock.Verify(s => s.Remove("OtpTime_Admin"), Times.Once);

            _contextMock.Verify(c => c.Update(It.IsAny<Account>()), Times.Once);
            _contextMock.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void VerifyOtp_Expired_ReturnsError()
        {
            // Arrange
            var expiredTime = DateTime.UtcNow.AddMinutes(-6); // giả sử OTP hết hạn sau 5 phút

            _sessionMock.Setup(s => s.GetString("OtpCode_Admin")).Returns("123456");
            _sessionMock.Setup(s => s.GetString("OtpEmail_Admin")).Returns("email@test.com");
            _sessionMock.Setup(s => s.GetString("OtpTime_Admin")).Returns(expiredTime.ToString("o"));

            SetupSessionMock();

            var otpViewModel = new OtpViewModel { Email = "email@test.com", Otp = "123456" };

            // Act
            var result = _controller.VerifyOtp(otpViewModel);

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(json.Value));

            Assert.False((bool)data["success"]);
            Assert.Equal("Mã OTP đã hết hạn.", data["message"]);
        }
      
        [Fact]
        public void VerifyOtp_InvalidOtp_ReturnsError()
        {
            // Arrange
            var validTime = DateTime.UtcNow;

            _sessionMock.Setup(s => s.GetString("OtpCode_Admin")).Returns("123456");
            _sessionMock.Setup(s => s.GetString("OtpEmail_Admin")).Returns("email@test.com");
            _sessionMock.Setup(s => s.GetString("OtpTime_Admin")).Returns(validTime.ToString("o"));

            SetupSessionMock();

            var otpViewModel = new OtpViewModel { Email = "email@test.com", Otp = "000000" };

            // Act
            var result = _controller.VerifyOtp(otpViewModel);

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(json.Value));

            Assert.False((bool)data["success"]);
            Assert.Equal("Mã OTP không đúng.", data["message"]);
        }
        [Fact]
        public void Logout_ClearsSessionAndRedirects()
        {
            _sessionMock.Setup(s => s.Clear()).Verifiable();

            var result = _controller.Logout();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            _sessionMock.Verify(s => s.Clear(), Times.Once());
        }

        [Fact]
        public async Task Create_ValidAccount_ReturnsRedirect()
        {
            var account = new Account
            {
                AccountId = 1,
                Email = "test@example.com",
                RoleId = 1,
                Active = true
            };

            _contextMock.Setup(c => c.Add(It.IsAny<Account>())).Verifiable();
        
            var result = await _controller.Create(account);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _contextMock.Verify(c => c.Add(It.IsAny<Account>()), Times.Once());
   
        }

        [Fact]
        public async Task Edit_ValidAccount_ReturnsRedirect()
        {
            var account = new Account
            {
                AccountId = 1,
                Email = "test@example.com",
                RoleId = 1,
                Active = true
            };

            _contextMock.Setup(c => c.Update(It.IsAny<Account>())).Verifiable();
          

            var result = await _controller.Edit(1, account);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _contextMock.Verify(c => c.Update(It.IsAny<Account>()), Times.Once());
   
        }

        [Fact]
        public async Task Delete_ValidId_ReturnsRedirect()
        {
            var account = new Account { AccountId = 1 };
            _contextMock.Setup(c => c.Accounts.FindAsync(1)).ReturnsAsync(account);
            _contextMock.Setup(c => c.Accounts.Remove(It.IsAny<Account>())).Verifiable();


            var result = await _controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _contextMock.Verify(c => c.Accounts.Remove(It.IsAny<Account>()), Times.Once());
        
        }
    }
}