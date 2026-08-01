# SAF — Seguimiento Administrativo Financiero

## 1) Propósito

`SAF` es una aplicación interna (Blazor Server + Radzen) para el seguimiento de pagos y el estado contable de devengados. Consume datos de solo lectura de la base **IVC** y los combina con datos propios y editables del SAF.

La autenticación **no** se resuelve en este proyecto: SAF actúa como aplicación cliente del SSO central [`Auth.Web`](../Auth.Web). Comparte la cookie de identidad mediante DataProtection apuntando a la base de `Auth.Web`, y lee los permisos desde el claim `perms_json` emitido por `Auth.Web`. Ver `Extensions/SafBuilderExtensions.cs` y `Security/`.

Al redirigir al login, SAF manda el `clientId` configurado en `AuthWeb:ClientId`; si está vacío, `Auth.Web` deduce la aplicación matcheando el `returnUrl` contra sus URLs registradas. No hardcodear un `clientId` de fallback: si no coincide exacto con el registrado en la base de `Auth.Web` del entorno, el login corta con `invalid_client` ("Aplicación destino inválida").

## 2) Estructura del proyecto

Se sigue la misma metodología de capas que `Auth.Web` para preservar separación de responsabilidades y escalabilidad.

```
Application/                 # Modelos de aplicación (DTOs por feature)
├─ Caf/Dtos/
├─ Common/                   # Utilidades transversales (diff de auditoría, atributos)
├─ Pagos/Dtos/
├─ Seguros/Dtos/
└─ StatusContabilidad/Dtos/

Components/                  # UI Blazor (.razor + code-behind .razor.cs)
├─ Account/                  # Login / AccessDenied (delegan en Auth.Web)
├─ Layout/
├─ Pages/
│  ├─ Caf/
│  ├─ Pagos/
│  ├─ Seguros/
│  └─ StatusContabilidad/
├─ Shared/                   # Componentes reutilizables (historial de auditoría, etc.)
├─ GridPageBase.cs           # Base de páginas con grilla (carga, edición, auditoría)
└─ PermissionPageBase.cs     # Base de páginas con control de permisos

Data/                        # EF Core
├─ Entities/                 # Entidades propias del SAF
├─ Migrations/
├─ AppDbContext.cs           # Base propia del SAF
├─ IvcDbContext.cs           # Base IVC (solo lectura)
└─ DataProtectionDbContext.cs# Apunta a la base de Auth.Web (key ring compartido)

Extensions/                  # Program.cs modular (builder y pipeline)
Repositories/                # Acceso a datos (Abstractions / Implementations)
Services/                    # Lógica de aplicación (Abstractions / Implementations)
Security/                    # Cookie compartida + claims de admin
Shared/                      # Helpers compartidos (notificaciones)
Tests/                       # xUnit + bUnit (Application, Components, harness de integración)
```

## 3) Convenciones

- **Capas:** `Components` → `Services` → `Repositories` → `Data`. Los `Services` devuelven DTOs de `Application/<Feature>/Dtos/`; nunca exponen tipos de UI hacia abajo.
- **Code-behind:** cada página es `Pagina.razor` + `Pagina.razor.cs` (`partial class`).
- **DI:** registro explícito en `Extensions/SafBuilderExtensions.cs` (repos `Transient`, services `Scoped`).
- **Inglés** para nombres de capas/carpetas/clases; el dominio funcional puede ir en español.
- **Auditoría:** toda alta/edición/baja manual queda registrada en `CambiosAuditoria` (quién, cuándo, campo, valor anterior y nuevo). El sync con IVC no se audita.

## 4) Ejecución

Requiere las cadenas de conexión `DefaultConnection`, `IvcConnection` y `AuthWebConnection`, además de la sección `SharedCookie` y `AuthWeb:BaseUrl` (ver `appsettings.json`; en desarrollo se cargan por user secrets).

```bash
dotnet run
```

La app redirige al login de `Auth.Web` si el usuario no está autenticado.

## 5) Publicación (IIS / Web Deploy)

Se publica con la tarea de VS Code **"Publicar SAF a IIS (Web Deploy)"**, que usa el perfil `Properties/PublishProfiles/IISProfile.pubxml` (pide la contraseña al ejecutar; no se guarda).

**Importante — configuración del servidor:**

- La configuración real del sitio (connection strings, `AuthWeb__BaseUrl`) vive como variables de entorno en el **`web.config` del servidor**, no en el repo. Ese archivo **no debe pisarse** en los deploys.
- El perfil `.pubxml` está **fuera del control de versiones** (`.gitignore` excluye `**/PublishProfiles/`). Si se recrea desde cero, debe incluir sí o sí:
  - `IsTransformWebConfigDisabled=true` — el publish no genera `web.config`, así el del servidor queda intacto. Excluirlo con `ExcludeFilesFromDeployment` **no funciona**: el archivo se genera durante el publish y se deploya igual (el 31/7/2026 un deploy sin esta propiedad pisó el `web.config` del servidor y dejó el sitio caído hasta restaurarlo a mano).
  - `SkipExtraFilesOnServer=true` — los archivos que están en el servidor pero no en el publish (como su `web.config`) no se borran.
