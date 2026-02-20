using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EasySave.LogServer.Controllers
{
    // ===== LOG CENTRALIZATION CONTROLLER =====
    // Handles HTTP requests to centralize logs from multiple EasySave instances
    [ApiController]
    [Route("api/logs")] // URL : http://localhost:8080/api/logs
    public class LogController : ControllerBase
    {
        // ===== PRIVATE MEMBERS =====
        // Path inside the Docker container where logs are persisted
        private readonly string _logDirectory = "/app/logs";
        // Thread safety lock for concurrent writes to the same daily file
        private static readonly object _fileLock = new object();

        // ===== API ENDPOINTS =====

        /// <summary>
        /// Receives a log entry and appends it to the daily centralized file
        /// </summary>
        /// <param name="rawEntry">The JSON log data</param>
        /// <param name="machine">Originating machine name for differentiation</param>
        [HttpPost]
        public async Task<IActionResult> PostLog([FromBody] JsonElement rawEntry, [FromQuery] string machine)
        {
            try
            {

                if (System.IO.File.Exists(_logDirectory))
                {
                    System.IO.File.Delete(_logDirectory);
                }

                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
                // ===== FILE INITIALIZATION =====
                // Create a unique filename per day: yyyy-mm-dd.json
                string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
                string filePath = Path.Combine(_logDirectory, fileName);

                // ===== THREAD-SAFE WRITE OPERATION =====
                lock (_fileLock)
                {
                    List<object> logs = new();

                    // Read existing logs if the file already exists for today
                    if (System.IO.File.Exists(filePath))
                    {
                        var content = System.IO.File.ReadAllText(filePath);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            try { logs = JsonSerializer.Deserialize<List<object>>(content) ?? new(); }
                            catch { logs = new(); }
                        }
                    }

                    // ===== DATA WRAPPING =====
                    // Add metadata to identify which user/machine sent the log
                    var logEntry = new
                    {
                        ServerReceivedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Log = rawEntry
                    };

                    logs.Add(logEntry);

                    // ===== PERSISTENCE =====
                    // Save the updated list back to the JSON file with indentation
                    var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(filePath, json);
                }

                return Ok(new { message = "Log centralized successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error : {ex.Message} | StackTrace : {ex.StackTrace}");
            }
        }
    }
}