using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using SharedCookie;

namespace SAF.Security
{
    /// <summary>
    /// Custom ISecureDataFormat implementation that serialises/deserialises
    /// AuthenticationTickets using DataContractSerializer so that cookies
    /// emitted by Auth.Web (including legacy OWIN/.NET 4.8 apps) can be read
    /// transparently by SAF without any re-login.
    ///
    /// IMPORTANT: the DataContractSerializer namespaces and member ordering in
    /// SharedCookieTicketDto / ClaimDto MUST stay identical to the Auth.Web
    /// implementation. Any divergence will make cookie parsing silently fail.
    /// </summary>
    public sealed class SharedCookieTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
    {
        private readonly IDataProtector _protector;
        private static readonly DataContractSerializer Serializer =
            new DataContractSerializer(typeof(SharedCookieTicketDto));

        public SharedCookieTicketDataFormat(IDataProtector protector)
        {
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        }

        public string Protect(AuthenticationTicket data)
        {
            if (data == null) return null!;

            var dto = SharedCookieTicketDto.FromAspNetCore(data);
            if (dto is null) return null!;

            var serialized = Serialize(dto);
            var protectedBytes = _protector.Protect(serialized);
            return Base64UrlEncode(protectedBytes);
        }

        public string Protect(AuthenticationTicket data, string? purpose) => Protect(data);

        public AuthenticationTicket? Unprotect(string? protectedText)
        {
            if (string.IsNullOrWhiteSpace(protectedText)) return null;

            try
            {
                var protectedBytes = Base64UrlDecode(protectedText);
                var unprotectedBytes = _protector.Unprotect(protectedBytes);
                var dto = Deserialize(unprotectedBytes);
                return dto?.ToAspNetCore();
            }
            catch
            {
                return null;
            }
        }

        public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
            => Unprotect(protectedText);

        private static byte[] Serialize(SharedCookieTicketDto dto)
        {
            using var ms = new MemoryStream();
            Serializer.WriteObject(ms, dto);
            return ms.ToArray();
        }

        private static SharedCookieTicketDto? Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            return Serializer.ReadObject(ms) as SharedCookieTicketDto;
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            return Convert.FromBase64String(s);
        }
    }
}
