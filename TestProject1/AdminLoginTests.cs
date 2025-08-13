using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;
using System;

public class AdminLoginTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "https://localhost:7155";

    public AdminLoginTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless"); // Test chạy ngầm, không hiện trình duyệt
        _driver = new ChromeDriver(options);
    }

    [Fact]
    public void Login_WithValidCredentials_ShouldRedirectToDashboard()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/AdminAccounts/Login");

        _driver.FindElement(By.Name("email")).SendKeys("admin@example.com");
        _driver.FindElement(By.Name("password")).SendKeys("yourpassword");

        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        System.Threading.Thread.Sleep(2000); // Hoặc thay bằng WebDriverWait

        Xunit.Assert.Contains("/Admin/Home/Index", _driver.Url);
    }

    [Fact]
    public void Login_WithInvalidPassword_ShouldShowError()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/AdminAccounts/Login");

        _driver.FindElement(By.Name("email")).SendKeys("admin@example.com");
        _driver.FindElement(By.Name("password")).SendKeys("sai-mat-khau");
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        System.Threading.Thread.Sleep(2000);

        var alert = _driver.FindElement(By.ClassName("alert"));
        Xunit.Assert.Contains("alert", alert.GetAttribute("class"));
        Xunit.Assert.False(string.IsNullOrWhiteSpace(alert.Text));
    }

    public void Dispose()
    {
        _driver.Quit(); // Tắt browser sau mỗi test
    }
}
