using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using TaskManager.Data;
using TaskManager.Data.Models;
using TaskManager.Web.ViewModels;
using System.Security.Cryptography;

namespace TaskManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp Email và Username
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

                // Băm mật khẩu (Hash Password)
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Tạo User mới và lưu vào database
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login", "Account");
            }

            return View(model);
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Tìm người dùng theo tên đăng nhập hoặc email
                var user = await _context.Users.SingleOrDefaultAsync(u =>
                    u.Username == model.UsernameOrEmail ||
                    u.Email == model.UsernameOrEmail);

                if (user != null)
                {
                    // Kiểm tra mật khẩu
                    if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    {
                        // Lấy vai trò của người dùng từ database
                        var userRoles = await _context.UserRoles
                            .Where(ur => ur.UserId == user.Id)
                            .Select(ur => ur.Role.Name) // Chỉ lấy tên của vai trò
                            .ToListAsync();

                        // Tạo các Claims (thông tin xác thực)
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email)
                        };
                        // Thêm các role claim
                        foreach (var roleName in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, roleName));
                        }
                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        };

                        // Đăng nhập người dùng bằng cách tạo cookie
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Chuyển hướng người dùng
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                // Nếu xác thực thất bại
                ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }
        #endregion

        #region Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        #endregion
        #region Change Password
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.FindAsync(int.Parse(userId!));

                if (user == null)
                {
                    return NotFound();
                }

                // 1. Xác thực mật khẩu cũ
                if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash!))
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác.");
                    return View(model);
                }

                // 2. Băm và lưu mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();
                TempData["success"] = "Mật khẩu của bạn đã được thay đổi thành công. Vui lòng đăng nhập lại.";

                // Sau khi đổi mật khẩu thành công, đăng xuất và chuyển hướng về trang đăng nhập
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            return View(model);
        }
        #endregion
        #region User Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(int.Parse(userId!));

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                // Chuyển thành chuỗi đã được định dạng 
                CreatedAtString = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userToUpdate = await _context.Users.FindAsync(int.Parse(userId!));

                if (userToUpdate == null)
                {
                    return NotFound();
                }

                userToUpdate.Email = model.Email!;
                userToUpdate.FullName = model.FullName;

                await _context.SaveChangesAsync();

                return RedirectToAction("Profile", "Account");
            }

            return View(model);
        }
        #endregion
        #region Forgot & Reset Password

        [HttpGet]
        [AllowAnonymous] // Cho phép người chưa đăng nhập truy cập
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    // Không thông báo cho người dùng biết email có tồn tại hay không vì lý do bảo mật
                    return View("ForgotPasswordConfirmation");
                }

                // Tạo token reset ngẫu nhiên
                var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

                user.PasswordResetToken = resetToken;
                user.ResetTokenExpires = DateTime.UtcNow.AddHours(1); // Token hết hạn sau 1 giờ
                await _context.SaveChangesAsync();

                // Tạo link reset
                var resetLink = Url.Action("ResetPassword", "Account", new { token = resetToken }, Request.Scheme);

                
                // GIẢ LẬP GỬI EMAIL
                
                
                TempData["ResetLink"] = resetLink;

                return View("ForgotPasswordConfirmation");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.SingleOrDefaultAsync(
                    u => u.PasswordResetToken == model.Token && u.ResetTokenExpires > DateTime.UtcNow);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Token không hợp lệ hoặc đã hết hạn.");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                user.PasswordResetAt = DateTime.UtcNow;
                // Vô hiệu hóa token đã sử dụng
                user.PasswordResetToken = null;
                user.ResetTokenExpires = null;

                await _context.SaveChangesAsync();

                // Gửi thông báo cho người dùng rằng mật khẩu của họ đã được thay đổi

                return View("ResetPasswordConfirmation");
            }

            return View(model);
        }

        #endregion
    }
}