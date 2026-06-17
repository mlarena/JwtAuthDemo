# Техническая документация: JwtAuthDemo

## 1. Обзор архитектуры

JwtAuthDemo — ASP.NET Core MVC приложение с JWT-аутентификацией, хранением токенов в HttpOnly cookies и моделью авторизации RBAC + PBAC.

### Стек технологий

| Компонент | Технология | Версия |
|-----------|-----------|--------|
| Runtime | .NET | 10.0 |
| Web-фреймворк | ASP.NET Core MVC | 10.0 |
| ORM | Entity Framework Core | 10.0.0-preview.7 |
| База данных | PostgreSQL (Npgsql) | 18 |
| Аутентификация | JWT Bearer (HMAC-SHA256) | 10.0.0-preview.5 |
| Хеширование паролей | BCrypt.Net-Next | 4.2.0 |
| Логирование | Serilog | 10.0.0 |
| Валидация | FluentValidation + DataAnnotations | 11.3.1 |
| API-документация | Swashbuckle (Swagger) | 6.9.0 |

---

## 2. Структура проекта

```
JwtAuthDemo/
├── Program.cs                          — точка входа, DI, middleware, seed
├── Configuration/
│   └── JwtSettings.cs                  — модель настроек JWT
├── Infrastructure/
│   ├── ApplicationDbContext.cs          — DbContext, конфигурация сущностей, seed-данные
│   └── Migrations/                     — миграции EF Core
├── Models/Entities/
│   ├── User.cs                         — пользователь
│   ├── Role.cs                         — роль
│   ├── Permission.cs                   — разрешение
│   ├── UserRole.cs                     — связь many-to-many
│   ├── RolePermission.cs               — связь many-to-many
│   ├── RefreshToken.cs                 — refresh-токен
│   └── AuditLog.cs                     — запись аудита
├── Services/
│   ├── JwtTokenService.cs              — генерация JWT access/refresh токенов
│   ├── IUserService.cs                 — CRUD пользователей
│   ├── IRoleService.cs                 — CRUD ролей и разрешений
│   ├── IAuditLogService.cs             — запись в журнал аудита
│   └── IAuditLogQueryableService.cs    — запросы к журналу аудита
├── ViewModels/                         — модели представлений
├── Controllers/
│   ├── AccountController.cs            — аутентификация
│   ├── UsersController.cs              — управление пользователями
│   ├── RolesController.cs              — управление ролями
│   ├── HomeController.cs               — dashboard, о системе
│   └── AuditController.cs              — журнал аудита
└── Views/                              — Razor-представления
```

---

## 3. Модель данных

### ER-диаграмма (текстовая)

```
User ──< UserRole >── Role ──< RolePermission >── Permission
 │                                             
 ├──< RefreshToken                             
 └──< AuditLog                                 
```

### Таблицы

| Таблица | Описание | Ключ |
|---------|----------|------|
| Users | Пользователи | Id (serial) |
| Roles | Роли | Id (serial) |
| Permissions | Разрешения | Id (serial) |
| UserRoles | Связь пользователь-роль | (UserId, RoleId) |
| RolePermissions | Связь роль-разрешение | (RoleId, PermissionId) |
| RefreshTokens | Refresh-токены | Id (serial) |
| AuditLogs | Журнал аудита | Id (bigint) |

### Сид-данные

**Роли:** SuperAdmin, Admin, Manager, User, Guest

**Разрешения (13 штук):**

| Разрешение | Ресурс | Действие |
|------------|--------|----------|
| users:read | users | read |
| users:create | users | create |
| users:update | users | update |
| users:delete | users | delete |
| users:lock | users | lock |
| roles:read | roles | read |
| roles:create | roles | create |
| roles:update | roles | update |
| roles:delete | roles | delete |
| permissions:manage | permissions | manage |
| audit:read | audit | read |
| profile:edit | profile | edit |
| dashboard:read | dashboard | read |

---

## 4. Система аутентификации (JWT)

### Процесс входа

```
Пользователь → POST /Account/Login
    ↓
Валидация пароля (BCrypt.Verify)
    ↓
Проверка: IsActive && !IsLocked
    ↓
Генерация JWT Access Token (60 мин)
Генерация Refresh Token (7 дней)
    ↓
Refresh Token → сохранение в БД (RefreshTokens)
JWT → установка в HttpOnly Cookie ("jwt")
    ↓
Редирект на Dashboard
    ↓
Запись в AuditLog: "Login Success"
```

### Структура JWT-токена

**Claims:**
- `sub` — ID пользователя
- `email` — email пользователя
- `jti` — уникальный ID токена
- `username` — имя пользователя
- `firstName`, `lastName` — имя и фамилия
- `role` — роли пользователя (множественные)
- `permission` — разрешения пользователя (множественные)

**Параметры:**
- Алгоритм: HMAC-SHA256
- Issuer: `JwtAuthDemo`
- Audience: `JwtAuthDemoUsers`
- Время жизни Access Token: 60 минут
- Время жизни Refresh Token: 7 дней

### Хранение токена

JWT хранится в **HttpOnly cookie** с параметрами:
- `HttpOnly = true` — недоступен из JavaScript
- `Secure = true` — только HTTPS
- `SameSite = Strict` — защита от CSRF
- `Path = "/"`

### Обновление токена

```
Access Token истекает
    ↓
Клиент отправляет POST /Account/RefreshToken
    ↓
Проверка Refresh Token из cookie
    ↓
Поиск в БД (не отозван, не истек)
    ↓
Генерация новой пары токенов
    ↓
Обновление записи RefreshToken в БД
    ↓
Отправка нового JWT в cookie
```

---

## 5. Система авторизации (RBAC + PBAC)

### Модель разрешений

```
Пользователь → UserRole → Роль → RolePermission → Разрешение
```

Каждое разрешение имеет формат `resource:action` (например, `users:read`).

### Authorization Policies

| Policy | Требование |
|--------|------------|
| CanManageUsers | Claim `permission` = `users:read` |
| CanManageRoles | Claim `permission` = `roles:read` |
| CanManageAudit | Claim `permission` = `audit:read` |
| CanManagePermissions | Claim `permission` = `permissions:manage` |

### Доступ к контроллерам

| Контроллер | Действие | Доступ |
|-----------|----------|--------|
| Account | Login, Register, ForgotPassword, ResetPassword, ConfirmEmail | Public |
| Account | Logout | Authorized |
| Home | About | Public |
| Home | Dashboard | Authorized |
| Users | Profile, Edit | Authorized |
| Users | Index, Create, Delete, ToggleLock, AssignRoles, Details | Admin |
| Roles | All actions | Admin |
| Audit | Index | Admin |

---

## 6. Журнал аудита

Каждое критическое действие записывается в таблицу `AuditLogs`:

| Поле | Описание |
|------|----------|
| UserId | ID пользователя (nullable) |
| Action | Тип действия (Login Success, Logout, User Registered, etc.) |
| Resource | Ресурс (auth, users, roles, profile, etc.) |
| IpAddress | IP-адрес клиента |
| UserAgent | User-Agent браузера |
| Status | Success / Failed |
| Error | Описание ошибки (при Failed) |
| Details | Дополнительные данные (JSON) |
| CreatedAt | Время создания записи |

### Типы записей

- `Login Success` / `Login Failed` — вход в систему
- `Logout` — выход из системы
- `User Registered` — регистрация нового пользователя
- `Email Confirmed` — подтверждение email
- `Password Reset Requested` / `Password Reset Completed` — сброс пароля
- `Profile Updated` — обновление профиля
- `User Created` / `User Deleted` / `User Lock Toggled` — управление пользователями
- `Role Created` / `Role Updated` / `Role Deleted` — управление ролями
- `Roles Assigned` — назначение ролей
- `Permissions Updated` — обновление разрешений

---

## 7. Конфигурация

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=jwtauthdemodb;Username=postgres;Password=12345678"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!!",
    "Issuer": "JwtAuthDemo",
    "Audience": "JwtAuthDemoUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Сервисы (DI)

| Сервис | Интерфейс | Жизненный цикл |
|--------|-----------|----------------|
| ApplicationDbContext | DbContext | Scoped |
| JwtTokenService | IJwtTokenService | Scoped |
| UserService | IUserService | Scoped |
| RoleService | IRoleService | Scoped |
| AuditLogService | IAuditLogService | Scoped |
| AuditLogQueryableService | IAuditLogQueryableService | Scoped |

---

## 8. Seed-логика

При старте приложения (`Program.cs`) выполняется:

1. Проверка是否存在 пользователя с ролью SuperAdmin (RoleId=1)
2. Если нет — создание пользователя:
   - UserName: `superadmin`
   - Email: `superadmin@admin.com`
   - Пароль: `Ps$$word01!` (хеширован BCrypt)
   - Роль: SuperAdmin

---

## 9. Защита

| Механизм | Реализация |
|----------|-----------|
| Хеширование паролей | BCrypt (автоматическая соль) |
| JWT в cookies | HttpOnly, Secure, SameSite=Strict |
| Антифоргery | `[ValidateAntiForgeryToken]` на POST-действиях |
| Блокировка аккаунта | Поля IsLocked, LockoutEnd, FailedLoginAttempts |
| Проверка email | Токен подтверждения email |
| Логирование | Полный аудит всех действий |
