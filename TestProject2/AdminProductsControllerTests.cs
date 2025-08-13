using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using webBanThucPham.Areas.Admin.Controllers;
using webBanThucPham.Models;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using X.PagedList;
using static webBanThucPham.Areas.Admin.Controllers.AdminProductsController;

namespace webBanThucPham.Tests
{
    public class AdminProductsControllerTests
    {
        private readonly Mock<INotyfService> _mockNotyfService;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly DbBanThucPhamContext _context;
        private readonly AdminProductsController _controller;

        public AdminProductsControllerTests()
        {
            _mockNotyfService = new Mock<INotyfService>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            // Sử dụng in-memory database thay vì mock DbContext
            var options = new DbContextOptionsBuilder<DbBanThucPhamContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_" + System.Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()  // Enable sensitive data logging for debugging
                .Options;

            _context = new DbBanThucPhamContext(options);

            _controller = new AdminProductsController(_mockNotyfService.Object, _context, _mockWebHostEnvironment.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithAListOfProducts()
        {
            // Arrange
            _context.Products.AddRange(new List<Product>
            {
                new Product { ProductId = 1, ProductName = "Product1", Active = true },  // Ensure Active is set
                new Product { ProductId = 2, ProductName = "Product2", Active = true }   // Ensure Active is set
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index(null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IPagedList<Product>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_Post_ReturnsRedirectToAction_WhenValidModel()
        {
            // Arrange
            var product = new Product
            {
                ProductName = "Test Product",
                Price = 100,
                Discount = 10,
                UnitsInStock = 50,
                Active = true,  // Ensure Active is set
                CatId = 1
            };

            _context.Categories.Add(new Category
            {
                CatId = 1,
                CatName = "Test Category",
                Published = true  // Ensure Published is set
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create(product, null, null, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_Post_ReturnsNotFound_WhenProductNotFound()
        {
            // Arrange
            var product = new Product { ProductId = 999, ProductName = "Non-existing Product" };

            // Act
            var result = await _controller.Edit(999, product, null, null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenProductIsDeleted()
        {
            // Arrange
            var product = new Product { ProductId = 1, ProductName = "Product to Delete", Active = true };  // Ensure Active is set
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.Equal("Sản phẩm đã được xóa thành công.", response.Message);

        }



        [Fact]
        public async Task Details_ReturnsNotFound_WhenProductNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }
    }
}
