# SAF — Seguimiento Administrativo Financiero

## 1) Propósito

`SAF` es una aplicación interna (Blazor Server + Radzen) para el seguimiento de pagos y el estado contable de devengados. Consume datos de solo lectura de la base **IVC** y los combina con datos propios y editables del SAF.

La autenticación **no** se resuelve en este proyecto: SAF actúa como aplicación cliente del SSO central [`Auth.Web`](../Auth.Web). Comparte la cookie de identidad mediante DataProtection apuntando a la base de `Auth.Web`, y lee los permisos desde el claim `perms_json` emitido por `Auth.Web`. Ver `Program.cs` y `Security/`.

## 2) Estructura del proyecto

Se sigue la misma metodología de capas que `Auth.Web` para preservar separación de responsabilidades y escalabilidad.

```
Application/                 # Modelos de aplicación (DTOs por feature)
├─ Pagos/Dtos/
└─ StatusContabilidad/Dtos/

Components/                  # UI Blazor (.razor + code-behind .razor.cs)
├─ Account/                  # Login / AccessDenied (delegan en Auth.Web)
├─ Layout/
├─ Pages/
│  ├─ Pagos/
│  └─ StatusContabilidad/
└─ PermissionPageBase.cs     # Base de páginas con control de permisos

Data/                        # EF Core
├─ Entities/                 # Entidades propias del SAF
├─ AppDbContext.cs           # Base propia del SAF
├─ IvcDbContext.cs           # Base IVC (solo lectura)
└─ DataProtectionDbContext.cs# Apunta a la base de Auth.Web (key ring compartido)

Repositories/                # Acceso a datos
├─ Abstractions/             # Interfaces
└─ Implementations/

Services/                    # Lógica de aplicación
├─ Abstractions/             # Interfaces
└─ Implementations/

Security/                    # Cookie compartida + claims de admin
```

## 3) Convenciones

- **Capas:** `Components` → `Services` → `Repositories` → `Data`. Los `Services` devuelven DTOs de `Application/<Feature>/Dtos/`; nunca exponen tipos de UI hacia abajo.
- **Code-behind:** cada página es `Pagina.razor` + `Pagina.razor.cs` (`partial class`).
- **DI:** registro explícito en `Program.cs` (repos `Transient`, services `Scoped`).
- **Inglés** para nombres de capas/carpetas/clases; el dominio funcional puede ir en español.

## 4) Ejecución

Requiere las cadenas de conexión `DefaultConnection`, `IvcConnection` y `AuthWebConnection`, además de la sección `SharedCookie` y `AuthWeb:BaseUrl` (ver `appsettings.json`).

```bash
dotnet run
```

La app redirige al login de `Auth.Web` si el usuario no está autenticado.
