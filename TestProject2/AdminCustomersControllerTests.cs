using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Http;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using PagedList.Core;

namespace webBanThucPham.Tests.Controllers
{
    public class AdminCustomersControllerTests
    {
        private readonly Mock<INotyfService> _notyfMock;
        private readonly Mock<DbBanThucPhamContext> _contextMock;
        private readonly Mock<DbSet<Customer>> _customerDbSetMock;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly DefaultHttpContext _httpContext;
        private readonly AdminCustomersController _controller;

        public AdminCustomersControllerTests()
        {
            _notyfMock = new Mock<INotyfService>();
            _contextMock = new Mock<DbBanThucPhamContext>();
            _customerDbSetMock = new Mock<DbSet<Customer>>();
            _sessionMock = new Mock<ISession>();
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns("wwwroot");

            _httpContext = new DefaultHttpContext { Session = _sessionMock.Object };

            _contextMock.Setup(c => c.Customers).Returns(_customerDbSetMock.Object);

            _controller = new AdminCustomersController(_contextMock.Object, _notyfMock.Object, _envMock.Object)
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

            mockSet.As<IAsyncEnumerable<T>>()
                   .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                   .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _innerEnumerator;

            public TestAsyncEnumerator(IEnumerator<T> enumerator)
            {
                _innerEnumerator = enumerator;
            }

            public T Current => _innerEnumerator.Current;

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_innerEnumerator.MoveNext());
            }

            public ValueTask DisposeAsync()
            {
                _innerEnumerator.Dispose();
                return new ValueTask();
            }
        }

        [Fact]
        public async Task Index_ReturnsViewWithCustomers()
        {
            // Arrange
            var customers = new List<Customer>
            {
                new Customer { CustomerId = 1, FullName = "Customer 1", Email = "customer1@example.com" },
                new Customer { CustomerId = 2, FullName = "Customer 2", Email = "customer2@example.com" }
            }.AsQueryable();

            var customerDbSetMock = new Mock<DbSet<Customer>>();
            SetupMockDbSet(customerDbSetMock, customers);

            _contextMock.Setup(c => c.Customers).Returns(customerDbSetMock.Object);

            // Act
            var result = await _controller.Index(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IPagedList<Customer>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Quản lý khách hàng", viewResult.ViewData["CurrentPage"]);
        }

        [Fact]
        public async Task Create_ValidCustomer_ReturnsRedirect()
        {
            // Arrange
            var customer = new Customer
            {
                FullName = "New Customer",
                Email = "newcustomer@example.com"
            };

            _contextMock.Setup(c => c.Add(It.IsAny<Customer>())).Verifiable();
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(customer);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _contextMock.Verify(c => c.Add(It.IsAny<Customer>()), Times.Once());
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Edit_ValidCustomer_ReturnsRedirect()
        {
            // Arrange
            var customer = new Customer
            {
                CustomerId = 1,
                FullName = "Updated Customer",
                Email = "updatedcustomer@example.com"
            };

            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync(customer);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.Edit(1, customer);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task Delete_ValidId_ReturnsRedirect()
        {
            // Arrange
            var customer = new Customer { CustomerId = 1, FullName = "Test Customer" };

            _contextMock.Setup(c => c.Customers.FindAsync(It.IsAny<object[]>())).ReturnsAsync(customer);
            _contextMock.Setup(c => c.Customers.Remove(It.IsAny<Customer>())).Verifiable();
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Delete_CustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync((Customer)null);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_CustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync((Customer)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ValidCustomer_Redirects()
        {
            // Arrange
            var customer = new Customer { CustomerId = 1, FullName = "Test Customer" };

            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync(customer);
            _contextMock.Setup(c => c.Customers.Remove(It.IsAny<Customer>())).Verifiable();
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Index_ReturnsViewWithNoCustomers()
        {
            // Arrange
            var customers = new List<Customer>().AsQueryable();
            SetupMockDbSet(_customerDbSetMock, customers);

            // Act
            var result = await _controller.Index(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IPagedList<Customer>>(viewResult.Model);
            Assert.Empty(model);
        }
    }
}
