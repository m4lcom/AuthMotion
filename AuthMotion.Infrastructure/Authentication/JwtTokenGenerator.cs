using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthMotion.Application.Interfaces;
using AuthMotion.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthMotion.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new Exception("JWT Secret is missing in appsettings.");

        // 1. Transformamos la clave de texto a bytes
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        // 2. Definimos los "Claims" (Datos del usuario que van en el token)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID único del token
        };

        // 3. Elegimos el algoritmo de encriptación (HS256 es el estándar)
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 4. Armamos el token con sus fechas de vencimiento
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        // 5. Lo serializamos a string para mandarlo por HTTP
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}