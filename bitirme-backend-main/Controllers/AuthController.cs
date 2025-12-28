using ApiProject.Models.DTOs;
using ApiProject.Services;
using ApiProject.Data;
using ApiProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto? registerDto)
    {
        // Request body null kontrolü
        if (registerDto == null)
        {
            return BadRequest(new { message = "Request body boş olamaz." });
        }

        // ModelState'i temizle - otomatik validation'ı atla (null kontrolünden sonra)
        ModelState.Clear();

        // Manuel validation - Frontend'den username veya email gelebilir
        var errors = new List<string>();

        // Kullanıcı adı veya Email kontrolü (ikisi de optional, ama en az biri olmalı)
        var username = registerDto.Username ?? registerDto.Email;
        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Email veya kullanıcı adı zorunlu");
        }

        // Şifre kontrolü
        if (string.IsNullOrWhiteSpace(registerDto.Password))
        {
            errors.Add("Şifre zorunlu");
        }
        else if (registerDto.Password.Length < 6)
        {
            errors.Add("Şifre en az 6 karakter olmalıdır");
        }

        // Rol kontrolü
        if (string.IsNullOrWhiteSpace(registerDto.Role))
        {
            errors.Add("Rol zorunlu");
        }

        if (errors.Any())
        {
            return BadRequest(new { message = "Validation hatası", errors = errors });
        }

        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kayıt işlemi sırasında bir hata oluştu.", error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        // Request body null kontrolü
        if (loginDto == null)
        {
            return BadRequest(new { message = "Request body boş olamaz." });
        }

        // ModelState validation kontrolü
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(new { message = "Kullanıcı adı/e-posta veya şifre hatalı." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Rol uyuşmazlığı hatası
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Giriş yapılırken bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Email ve şifre gösterilmez)
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Role = u.RoleId == 0 ? "Student" : u.RoleId == 1 ? "AcademicStaff" : u.RoleId == 2 ? "Staff" : "Admin",
                    StudentNo = u.StaffId
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcılar getirilirken bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// Tüm öğretmenleri listeler (Public - öğrenciler için)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("teachers")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAllTeachers()
    {
        try
        {
            var teachers = await _context.Users
                .Where(u => u.RoleId == 1) // AcademicStaff = 1
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Role = "AcademicStaff",
                    StudentNo = u.StaffId,
                    Email = u.Email // Email eklendi (select box'ta gösterilmek için)
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(teachers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Öğretmenler getirilirken bir hata oluştu.", error = ex.Message });
        }
    }
}

