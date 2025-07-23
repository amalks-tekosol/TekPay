using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class JwtValidator
    {
        public static bool ValidateToken(string token, string secretKey)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            Log(key.ToString());
            Log(token);
            Log(secretKey);

            try
            {
                Log("Starting token validation");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = true,
                    ValidIssuer = "TekPay",

                    ValidateAudience = true,
                    ValidAudience = "TrustedClient",

                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero 
                }, out SecurityToken validatedToken);

                return true;
            }
            catch(Exception ex)
            {
                Log(ex.Message);
                return false;
            }
        }

        private static void Log(string message)
        {
            try
            {
                var path = @"C:\Terminal Connector V1\middleware_logs.txt";
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during logging
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }
    }
}
