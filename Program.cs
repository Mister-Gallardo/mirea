using CampusHub.ConfigCenter.Configuration;
using CampusHub.ConfigCenter.Middleware;
using CampusHub.ConfigCenter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

// ============================================================
// ШАБЛОН: Empty Web App (.NET 10)
// ПРОЕКТ: CampusHub.ConfigCenter
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. ПОРЯДОК ПОДКЛЮЧЕНИЯ ИСТОЧНИКОВ КОНФИГУРАЦИИ
//    WebApplication.CreateBuilder уже подключает:
//      [1] appsettings.json
//      [2] appsettings.{Environment}.json
//      [3] User Secrets (только Development)
//      [4] Environment Variables
//      [5] Command Line Args
//    Мы ОЧИЩАЕМ существующие источники и строим цепочку явно,
//    чтобы продемонстрировать осознанный порядок и конфликты.
// ============================================================

builder.Configuration.Sources.Clear();

// [1] Базовая конфигурация JSON
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// [2] Переопределение для текущего окружения (Development/Production)
//     КОНФЛИКТ #1: Portal:Title, Portal:Semester переопределяются здесь
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: true);

// [3] XML-файл с дополнительными настройками портала
builder.Configuration.AddXmlFile("portal.xml", optional: false, reloadOnChange: true);

// [4] INI-файл с настройками уведомлений
builder.Configuration.AddIniFile("notifications.ini", optional: false, reloadOnChange: true);

// [5] In-memory коллекция
//     КОНФЛИКТ #2: Notifications:Sender переопределяется (был в INI, теперь in-memory)
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Notifications:Sender"]    = "inmemory-override@campus.local",
    ["Notifications:Channel"]   = "InMemory-Push",
    ["AppMeta:Source"]          = "InMemoryCollection",
    ["AppMeta:Version"]         = "1.0.0-inmemory"
});

// [6] Собственный провайдер — читает customsettings.txt
builder.Configuration.AddTextFile("customsettings.txt");

// [7] Переменные среды (из launchSettings.json)
//     КОНФЛИКТ #3: Portal:SupportEmail переопределяется здесь
//     Двойное подчёркивание __ = разделитель иерархии (Portal__SupportEmail → Portal:SupportEmail)
builder.Configuration.AddEnvironmentVariables();

// [8] Аргументы командной строки — наивысший приоритет
//     КОНФЛИКТ #1 продолжение: Portal:Title, Portal:Semester — если переданы через CLI, они побеждают
builder.Configuration.AddCommandLine(args);

// ============================================================
// 2. РЕГИСТРАЦИЯ СЕРВИСОВ
// ============================================================

// Регистрируем IOptions<PortalOptions> — привязка к секции "Portal"
builder.Services.Configure<PortalOptions>(
    builder.Configuration.GetSection(PortalOptions.Section));

// Программное переопределение одного значения после чтения из файлов (доп. задание)
builder.Services.PostConfigure<PortalOptions>(opt =>
{
    if (string.IsNullOrWhiteSpace(opt.Title))
        opt.Title = "CampusHub [PostConfigure Fallback]";
});

// Регистрируем IOptions<NotificationOptions> — привязка к секции "Notifications"
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.Section));

// ============================================================
// 3. ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ
// ============================================================

var app = builder.Build();

// Подключаем собственный middleware (использует IOptions<PortalOptions>)
app.UseMiddleware<PortalHeaderMiddleware>();

// ============================================================
// ВСПОМОГАТЕЛЬНАЯ ФУНКЦИЯ: рекурсивный обход GetChildren()
// ============================================================
static void BuildTree(IConfigurationSection section, StringBuilder sb, int depth = 0)
{
    var indent = new string(' ', depth * 2);
    var value = section.Value;

    if (value is not null)
    {
        sb.AppendLine($"{indent}\"{section.Key}\": \"{value}\"");
    }
    else
    {
        sb.AppendLine($"{indent}\"{section.Key}\": {{");
        foreach (var child in section.GetChildren())
            BuildTree(child, sb, depth + 1);
        sb.AppendLine($"{indent}}}");
    }
}

// ============================================================
// 4. МАРШРУТЫ
// ============================================================

// GET / — стартовая страница с описанием и ссылками
app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1"/>
  <title>CampusHub.ConfigCenter</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Segoe UI', system-ui, sans-serif;
      background: linear-gradient(135deg, #0f0c29, #302b63, #24243e);
      min-height: 100vh;
      color: #e2e8f0;
      padding: 2rem;
    }
    .container { max-width: 900px; margin: 0 auto; }
    h1 {
      font-size: 2.4rem;
      font-weight: 700;
      background: linear-gradient(90deg, #a78bfa, #60a5fa);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      margin-bottom: 0.5rem;
    }
    .subtitle {
      color: #94a3b8;
      font-size: 1rem;
      margin-bottom: 2rem;
    }
    .card {
      background: rgba(255,255,255,0.05);
      backdrop-filter: blur(12px);
      border: 1px solid rgba(255,255,255,0.1);
      border-radius: 16px;
      padding: 1.5rem;
      margin-bottom: 1.5rem;
    }
    .card h2 {
      font-size: 1.1rem;
      font-weight: 600;
      color: #a78bfa;
      margin-bottom: 1rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .route-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 0.75rem;
    }
    a.route-link {
      display: block;
      padding: 0.75rem 1rem;
      background: rgba(167,139,250,0.08);
      border: 1px solid rgba(167,139,250,0.25);
      border-radius: 10px;
      text-decoration: none;
      color: #c4b5fd;
      font-family: 'Courier New', monospace;
      font-size: 0.9rem;
      transition: all 0.2s ease;
    }
    a.route-link:hover {
      background: rgba(167,139,250,0.2);
      border-color: #a78bfa;
      color: #fff;
      transform: translateY(-2px);
    }
    .desc {
      font-size: 0.78rem;
      color: #64748b;
      margin-top: 0.25rem;
    }
    .badge {
      display: inline-block;
      padding: 0.15rem 0.5rem;
      border-radius: 999px;
      font-size: 0.7rem;
      font-weight: 600;
      margin-right: 0.3rem;
      vertical-align: middle;
    }
    .badge-get { background: #1d4ed8; color: #bfdbfe; }
    footer { text-align: center; color: #475569; font-size: 0.8rem; margin-top: 2rem; }
  </style>
</head>
<body>
  <div class="container">
    <h1>🎓 CampusHub.ConfigCenter</h1>
    <p class="subtitle">Учебный сервис диагностики конфигурации внутреннего портала колледжа · ASP.NET Core (.NET 10)</p>

    <div class="card">
      <h2>📋 Описание проекта</h2>
      <p style="color:#cbd5e1;line-height:1.6">
        Приложение демонстрирует сборку конфигурации из нескольких источников:
        <strong style="color:#a78bfa">JSON · XML · INI · InMemory · Env Variables · CommandLine · Custom TXT</strong>.
        Показывает конфликты ключей, привязку к классам через <code style="color:#60a5fa">Bind/Get&lt;T&gt;</code>
        и передачу через <code style="color:#60a5fa">IOptions&lt;T&gt;</code> в middleware.
      </p>
    </div>

    <div class="card">
      <h2>🔗 Тестовые маршруты</h2>
      <div class="route-grid">
        <a class="route-link" href="/config/raw">
          <span class="badge badge-get">GET</span>/config/raw
          <div class="desc">Значения через индексатор IConfiguration</div>
        </a>
        <a class="route-link" href="/config/section/portal">
          <span class="badge badge-get">GET</span>/config/section/portal
          <div class="desc">Секция Portal через GetSection()</div>
        </a>
        <a class="route-link" href="/config/tree">
          <span class="badge badge-get">GET</span>/config/tree
          <div class="desc">Рекурсивное дерево через GetChildren()</div>
        </a>
        <a class="route-link" href="/config/tree?section=Notifications">
          <span class="badge badge-get">GET</span>/config/tree?section=Notifications
          <div class="desc">Дерево произвольной секции</div>
        </a>
        <a class="route-link" href="/config/connection">
          <span class="badge badge-get">GET</span>/config/connection
          <div class="desc">GetConnectionString("DefaultConnection")</div>
        </a>
        <a class="route-link" href="/config/providers">
          <span class="badge badge-get">GET</span>/config/providers
          <div class="desc">Список реальных провайдеров конфигурации</div>
        </a>
        <a class="route-link" href="/config/custom">
          <span class="badge badge-get">GET</span>/config/custom
          <div class="desc">Данные из customsettings.txt (Custom Provider)</div>
        </a>
        <a class="route-link" href="/config/bind">
          <span class="badge badge-get">GET</span>/config/bind
          <div class="desc">Привязка через Bind() / Get&lt;PortalOptions&gt;()</div>
        </a>
        <a class="route-link" href="/config/options">
          <span class="badge badge-get">GET</span>/config/options
          <div class="desc">IOptions&lt;PortalOptions&gt; + IOptions&lt;NotificationOptions&gt;</div>
        </a>
        <a class="route-link" href="/config/effective">
          <span class="badge badge-get">GET</span>/config/effective
          <div class="desc">Итоговые значения конфликтующих ключей + окружение</div>
        </a>
      </div>
    </div>

    <footer>Практическая работа №6 · ASP.NET Core Configuration · .NET 10</footer>
  </div>
</body>
</html>
""", "text/html; charset=utf-8"));

// ---------------------------------------------------------------
// GET /config/raw — значения через индексатор IConfiguration и DI
// ---------------------------------------------------------------
app.MapGet("/config/raw", (IConfiguration config) =>
{
    var result = new
    {
        description = "Значения получены через индексатор IConfiguration[]",
        values = new Dictionary<string, string?>
        {
            ["Portal:Title"]            = config["Portal:Title"],
            ["Portal:Semester"]         = config["Portal:Semester"],
            ["Portal:SupportEmail"]     = config["Portal:SupportEmail"],
            ["Portal:Admin:Name"]       = config["Portal:Admin:Name"],
            ["Notifications:Sender"]    = config["Notifications:Sender"],
            ["Notifications:Channel"]   = config["Notifications:Channel"],
            ["AppMeta:Source"]          = config["AppMeta:Source"],
            ["AppMeta:DeployedBy"]      = config["AppMeta:DeployedBy"],
            ["Custom:AppVersion"]       = config["Custom:AppVersion"],
            ["Custom:Theme"]            = config["Custom:Theme"]
        }
    };
    return Results.Json(result, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/section/portal — данные секции через GetSection()
// ---------------------------------------------------------------
app.MapGet("/config/section/portal", (IConfiguration config) =>
{
    var section = config.GetSection("Portal");
    var result = new
    {
        description = "Данные получены через config.GetSection(\"Portal\")",
        sectionKey = section.Key,
        sectionPath = section.Path,
        exists = section.Exists(),
        values = new Dictionary<string, string?>
        {
            ["Title"]        = section["Title"],
            ["Semester"]     = section["Semester"],
            ["SupportEmail"] = section["SupportEmail"],
            ["Admin:Name"]   = section["Admin:Name"],
            ["Admin:Email"]  = section["Admin:Email"]
        },
        modules = section.GetSection("Modules").GetChildren().Select(c => c.Value).ToArray()
    };
    return Results.Json(result, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/tree?section=Portal — рекурсивное дерево GetChildren()
// ---------------------------------------------------------------
app.MapGet("/config/tree", (IConfiguration config, string? section) =>
{
    var sectionName = section ?? "Portal";
    var configSection = config.GetSection(sectionName);

    if (!configSection.Exists())
        return Results.NotFound(new { error = $"Секция '{sectionName}' не найдена в конфигурации." });

    var sb = new StringBuilder();
    BuildTree(configSection, sb);

    return Results.Json(new
    {
        description     = $"Рекурсивный обход секции '{sectionName}' через GetChildren()",
        section         = sectionName,
        hint            = "Используйте ?section=ИмяСекции для анализа любой секции",
        tree            = sb.ToString()
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/connection — строка подключения через GetConnectionString()
// ---------------------------------------------------------------
app.MapGet("/config/connection", (IConfiguration config) =>
{
    var cs = config.GetConnectionString("DefaultConnection");
    return Results.Json(new
    {
        description       = "Строка подключения получена через GetConnectionString(\"DefaultConnection\")",
        connectionString  = cs,
        note              = "Эквивалентно config.GetSection(\"ConnectionStrings\")[\"DefaultConnection\"]"
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/providers — реальные провайдеры из IConfigurationRoot
// ---------------------------------------------------------------
app.MapGet("/config/providers", (IConfiguration config) =>
{
    if (config is not IConfigurationRoot root)
        return Results.Problem("IConfiguration не является IConfigurationRoot.");

    var providers = root.Providers
        .Select((p, i) => new
        {
            order        = i + 1,
            providerType = p.GetType().Name,
            fullType     = p.GetType().FullName,
            notes        = p.GetType().Name switch
            {
                "JsonConfigurationProvider"      => "appsettings*.json файлы",
                "XmlConfigurationProvider"       => "portal.xml — XML источник",
                "IniConfigurationProvider"       => "notifications.ini — INI источник",
                "MemoryConfigurationProvider"    => "AddInMemoryCollection — in-memory данные",
                "TextConfigurationProvider"      => "customsettings.txt — собственный провайдер",
                "EnvironmentVariablesConfigurationProvider" => "Переменные среды (launchSettings / OS)",
                "CommandLineConfigurationProvider"          => "Аргументы командной строки",
                _                                => "Стандартный или иной провайдер"
            }
        })
        .ToList();

    return Results.Json(new
    {
        description     = "Список реальных провайдеров из IConfigurationRoot.Providers (в порядке приоритета — последний побеждает)",
        totalProviders  = providers.Count,
        providers
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/custom — данные из собственного TextConfigurationProvider
// ---------------------------------------------------------------
app.MapGet("/config/custom", (IConfiguration config) =>
{
    var customKeys = new[] { "Custom:AppVersion", "Custom:MaintenanceMode", "Custom:SupportPhone", "Custom:Theme", "Custom:MaxUploadMb" };
    var values = customKeys.ToDictionary(k => k, k => config[k]);

    return Results.Json(new
    {
        description     = "Данные прочитаны из customsettings.txt через собственный TextConfigurationProvider",
        format          = "Формат файла: нечётная строка — ключ, следующая строка — значение. Ключи с ':' образуют иерархию.",
        providerClass   = nameof(CampusHub.ConfigCenter.Configuration.TextConfigurationProvider),
        values
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/bind — проекция на PortalOptions через Bind() и Get<T>()
// ---------------------------------------------------------------
app.MapGet("/config/bind", (IConfiguration config) =>
{
    // Способ 1: Bind()
    var viaBindMethod = new PortalOptions();
    config.GetSection(PortalOptions.Section).Bind(viaBindMethod);

    // Способ 2: Get<T>()
    var viaGetGeneric = config.GetSection(PortalOptions.Section).Get<PortalOptions>();

    return Results.Json(new
    {
        description = "Проекция конфигурации на класс PortalOptions",
        note        = "PortalOptions содержит вложенный объект AdminOptions и коллекцию List<string> Modules",
        viaBind     = viaBindMethod,
        viaGetT     = viaGetGeneric
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/options — значения через IOptions<T>
// ---------------------------------------------------------------
app.MapGet("/config/options",
    (IOptions<PortalOptions> portalOpts,
     IOptions<NotificationOptions> notifOpts) =>
{
    return Results.Json(new
    {
        description = "Настройки получены через механизм IOptions<T> (Dependency Injection)",
        differences = new
        {
            bindOrGetT = "Bind()/Get<T>() — разовое чтение конфигурации в любом месте кода",
            iOptions   = "IOptions<T> — синглтон, зарегистрированный в DI; значение фиксируется при старте"
        },
        portalOptions       = portalOpts.Value,
        notificationOptions = notifOpts.Value
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ---------------------------------------------------------------
// GET /config/effective — итоговые значения конфликтующих ключей
// ---------------------------------------------------------------
app.MapGet("/config/effective", (IConfiguration config, IWebHostEnvironment env) =>
{
    // Читаем финальные (побеждающие) значения конфликтующих ключей
    var portalTitle   = config["Portal:Title"];
    var portalSemester = config["Portal:Semester"];
    var supportEmail  = config["Portal:SupportEmail"];
    var notifSender   = config["Notifications:Sender"];

    return Results.Json(new
    {
        description    = "Итоговые значения конфликтующих ключей после объединения всех источников",
        environment    = env.EnvironmentName,
        sourceOrder    = new[]
        {
            "[1] appsettings.json (базовый — наименьший приоритет)",
            "[2] appsettings.Development.json (переопределяет [1])",
            "[3] portal.xml",
            "[4] notifications.ini",
            "[5] AddInMemoryCollection (переопределяет [4] для Notifications:Sender)",
            "[6] customsettings.txt (собственный провайдер)",
            "[7] Environment Variables (переопределяет Portal:SupportEmail)",
            "[8] Command Line Args (наивысший приоритет — побеждает всех)"
        },
        conflicts = new object[]
        {
            new {
                key          = "Portal:Title",
                source1      = "appsettings.json → CampusHub Portal [appsettings.json]",
                source2      = "appsettings.Development.json → CampusHub Portal [appsettings.Development.json] — DEV MODE",
                source3      = "commandLineArgs → CampusHub [CommandLine WIN] (если передан через args)",
                finalValue   = portalTitle,
                winner       = "CommandLine [8] > Development.json [2] > appsettings.json [1]"
            },
            new {
                key          = "Portal:SupportEmail",
                source1      = "appsettings.json → support@campus.local",
                source2      = "appsettings.Development.json → dev-support@campus.local",
                source3      = "EnvironmentVariables → env-override@campus.local (Portal__SupportEmail)",
                finalValue   = supportEmail,
                winner       = "Environment Variables [7] переопределяет JSON [1] и Dev.json [2]"
            },
            new {
                key          = "Notifications:Sender",
                source1      = "notifications.ini → noreply-ini@campus.local",
                source2      = "InMemoryCollection → inmemory-override@campus.local",
                source3      = (string?)null,
                finalValue   = notifSender,
                winner       = "InMemoryCollection [5] переопределяет INI [4]"
            }
        }
    }, new JsonSerializerOptions { WriteIndented = true });
});

// ============================================================
// 5. ЗАПУСК
// ============================================================

app.Run();
