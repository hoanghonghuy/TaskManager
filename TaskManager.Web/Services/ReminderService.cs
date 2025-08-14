using Microsoft.EntityFrameworkCore;
using TaskManager.Data;

namespace TaskManager.Web.Services
{
    public class ReminderService : IHostedService, IDisposable
    {
        private readonly ILogger<ReminderService> _logger;
        private Timer? _timer = null;
        private readonly IServiceProvider _serviceProvider;

        public ReminderService(ILogger<ReminderService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder Service is starting.");

            // Thiết lập Timer để chạy phương thức DoWork mỗi phút
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            _logger.LogInformation("Reminder Service is working.");

            // Vì đây là một background service, chúng ta cần tạo một scope riêng
            // để lấy ApplicationDbContext, tránh các vấn đề về luồng.
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Tìm các công việc cần nhắc nhở
                var tasksToRemind = dbContext.Tasks
                    .Where(t => t.ReminderTime.HasValue &&         // Có đặt thời gian nhắc nhở
                                t.ReminderTime.Value <= DateTime.UtcNow && // Đã đến hoặc qua giờ nhắc nhở
                                t.Status == "Pending" &&          // Công việc vẫn chưa hoàn thành
                                !t.IsReminded)                    // Và chưa được nhắc nhở trước đó
                    .ToList();

                if (tasksToRemind.Any())
                {
                    _logger.LogInformation($"Found {tasksToRemind.Count} tasks to remind.");
                    foreach (var task in tasksToRemind)
                    {
                        // =======================================================
                        // GỬI NHẮC NHỞ TẠI ĐÂY
                        // Hiện tại, chúng ta sẽ chỉ log ra console để kiểm tra.
                        // Sau này, bạn có thể tích hợp dịch vụ gửi email (SendGrid, MailKit)
                        // hoặc gửi thông báo (SignalR) tại đây.
                        // =======================================================
                        _logger.LogWarning($"REMINDER: Task '{task.Title}' is due! (ID: {task.Id})");

                        // Đánh dấu là đã nhắc nhở để không gửi lại
                        task.IsReminded = true;
                    }

                    // Lưu lại trạng thái 'IsReminded' vào database
                    dbContext.SaveChanges();
                }
                else
                {
                    _logger.LogInformation("No tasks to remind at this time.");
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}