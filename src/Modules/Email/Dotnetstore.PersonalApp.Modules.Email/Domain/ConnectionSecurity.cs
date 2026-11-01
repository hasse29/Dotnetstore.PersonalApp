namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

internal enum ConnectionSecurity
{
    /// <summary>Plain text. Only sensible on a trusted network.</summary>
    None,

    /// <summary>Starts unencrypted and upgrades the connection with STARTTLS (SMTP) or STLS (POP3).</summary>
    StartTls,

    /// <summary>TLS from the first byte (implicit TLS, e.g. SMTPS on 465 and POP3S on 995).</summary>
    SslOnConnect,
}
