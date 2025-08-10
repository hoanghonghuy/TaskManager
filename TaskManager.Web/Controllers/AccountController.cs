using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Data;
using TaskManager.Data.Models;
using TaskManager.Web.ViewModels;

namespace TaskManager.Web.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng lặp Email và Username
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng.");
                    return View(model);
                }
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                // 2. Băm mật khẩu (Hash Password)
                // Chúng ta sẽ sử dụng thư viện BCrypt.Net cho việc này
                // Em sẽ hướng dẫn cài đặt ở bước tiếp theo
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // 3. Tạo User mới và lưu vào database
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 4. Chuyển hướng người dùng sau khi đăng ký thành công
                return RedirectToAction("Login", "Account"); // Sẽ tạo chức năng này sau
            }

            // Nếu dữ liệu không hợp lệ, trả về View với lỗi
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // 1. Tìm người dùng theo tên đăng nhập hoặc email
                var user = await _context.Users.SingleOrDefaultAsync(u =>
                    u.Username == model.UsernameOrEmail ||
                    u.Email == model.UsernameOrEmail);

                if (user != null)
                {
                    // 2. Kiểm tra mật khẩu
                    if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    {
                        // Đăng nhập thành công, tạo Identity và Session
                        // Tạo các Claims (thông tin xác thực)
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email),
                            // Có thể thêm Claim cho Role (quyền) sau này
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            // Các thuộc tính của cookie, ví dụ: thời gian hết hạn
                            IsPersistent = true, // Giữ đăng nhập sau khi đóng trình duyệt
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        };

                        // Đăng nhập người dùng bằng cách tạo cookie
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // 3. Chuyển hướng người dùng
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home"); // Chuyển hướng về trang chủ
                        }
                    }
                }

                // Nếu xác thực thất bại
                ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}