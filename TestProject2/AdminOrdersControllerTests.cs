using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Hosting;
using MockQueryable.Moq;
using X.PagedList;

namespace webBanThucPham.Tests.Controllers
{
    public class AdminOrdersControllerTests
    {
        private readonly Mock<INotyfService> _notyfMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<DbSet<Order>> _orderSetMock;
        private readonly Mock<DbSet<Transactstatuss>> _transactStatusSetMock;  // Mock thêm TransactStatus
        private readonly Mock<DbBanThucPhamContext> _contextMock;

        public AdminOrdersControllerTests()
        {
            _notyfMock = new Mock<INotyfService>();
            _envMock = new Mock<IWebHostEnvironment>();

            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    Customer = new Customer { Email = "a@gmail.com", FullName = "A" },
                    OrderDate = new DateTime(2024, 1, 10),
                    Orderdetails = new List<Orderdetail>
                    {
                        new Orderdetail { Quantity = 1, Total = 100000, Product = new Product { ProductName = "Cam", Price = 100000 } }
                    },
                    Deleted = false
                }
            };

            var transactStatuses = new List<Transactstatuss>
            {
                new Transactstatuss { TracsactStatusId = 1, Status = "Đã giao" },
                new Transactstatuss { TracsactStatusId = 2, Status = "Chờ xử lý" }
            };

            // Mock DbSet<Order> với hỗ trợ async
            _orderSetMock = orders.AsQueryable().BuildMockDbSet();
            _transactStatusSetMock = transactStatuses.AsQueryable().BuildMockDbSet();

            _contextMock = new Mock<DbBanThucPhamContext>();
            _contextMock.Setup(c => c.Orders).Returns(_orderSetMock.Object);
            _contextMock.Setup(c => c.Transactstatusses).Returns(_transactStatusSetMock.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewWithOrders()
        {
            // Arrange
            var controller = new AdminOrdersController(_notyfMock.Object, _contextMock.Object, _envMock.Object, null);

            // Act
            var result = await controller.Index(null, null, null, null, null, null, null, false, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IPagedList<Order>>(viewResult.Model);
            Assert.Single(model); // Chỉ có 1 đơn hàng
        }

        [Fact]
        public async Task Index_FilterByEmail_ReturnsFilteredOrders()
        {
            // Arrange
            var controller = new AdminOrdersController(_notyfMock.Object, _contextMock.Object, _envMock.Object, null);

            // Act
            var result = await controller.Index(null, "a@gmail.com", null, null, null, null, null, false, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IPagedList<Order>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("a@gmail.com", model.First().Customer.Email);
        }
    }
}
