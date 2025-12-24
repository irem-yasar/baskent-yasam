using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OccupancyController : ControllerBase
{
    private readonly AppDbContext _context;

    public OccupancyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("update")]
    public async Task<ActionResult> UpdateOccupancy([FromBody] OccupancyUpdateDto dto)
    {
        try
        {
            var occupancyLog = new OccupancyLog
            {
                ZoneName = dto.ZoneName,
                Count = dto.Count,
                Capacity = dto.Capacity,
                LogTime = DateTime.UtcNow
            };

            _context.OccupancyLogs.Add(occupancyLog);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Doluluk bilgisi kaydedildi.",
                data = new
                {
                    id = occupancyLog.Id,
                    zoneName = occupancyLog.ZoneName,
                    count = occupancyLog.Count,
                    capacity = occupancyLog.Capacity,
                    occupancyRate = Math.Round((double)occupancyLog.Count / occupancyLog.Capacity * 100, 2),
                    logTime = occupancyLog.LogTime
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Doluluk bilgisi kaydedilirken bir hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("{zoneName}")]
    public async Task<ActionResult> GetOccupancy(string zoneName)
    {
        try
        {
            // En son kaydedilmiş doluluk bilgisini getir
            var latestLog = await _context.OccupancyLogs
                .Where(o => o.ZoneName == zoneName)
                .OrderByDescending(o => o.LogTime)
                .FirstOrDefaultAsync();

            if (latestLog == null)
                return NotFound(new { message = $"'{zoneName}' bölgesi için doluluk bilgisi bulunamadı." });

            return Ok(new
            {
                zoneName = latestLog.ZoneName,
                count = latestLog.Count,
                capacity = latestLog.Capacity,
                occupancyRate = Math.Round((double)latestLog.Count / latestLog.Capacity * 100, 2),
                logTime = latestLog.LogTime,
                isFull = latestLog.Count >= latestLog.Capacity
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Doluluk bilgisi alınırken bir hata oluştu.", error = ex.Message });
        }
    }
}

