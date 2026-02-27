using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TodoApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options; // נדרש להמרות JSON

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. הגדרת בסיס הנתונים (כמו אצלך בפרויקט)
// ==============================================================================
var connectionString = builder.Configuration.GetConnectionString("todoapidb");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 25))));

// ==============================================================================
// 2. הגדרת Swagger + מנעול 
// ==============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

// ==============================================================================
// 3. הגדרת אימות (Authentication)
// *** תואם לצילום מסך
// ==============================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // כאן אנחנו שולפים את ההגדרות מה-appsettings.json כמו המורה
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

// הגדרת CORS (כמו אצלך)
builder.Services.AddCors(options =>
{
    options.AddPolicy("general", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ==============================================================================
// 4. ה-Pipeline (המידל-וור)
// *** תואם לצילום מסך: מידל וואר.jpg (שורות 98-100) ***
// ==============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("general");

app.UseAuthentication(); // חובה: הפקודה שמפעילה את בדיקת הטוקן
app.UseAuthorization();  // חובה: הפקודה שבודקת הרשאות

// ==============================================================================
// 5. פונקציות עזר (לוגיקה של המורה)
// מכיוון שאנחנו ב-Minimal API, נגדיר אותן כפונקציות מקומיות או נשתמש בהן בתוך הנתיב
// ==============================================================================

// פונקציה ליצירת JWT - *** תואם לצילום מסך: create and setSession.jpg (שורות 52-73) ***
string CreateJWT(User user)
{
    var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])); // שליפת המפתח מההגדרות
    var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim("id", user.Id.ToString()),
        new Claim("name", user.Username ?? "Unknown"), // הגנה מפני Null
        // new Claim("role", user.Role) // תוסיפי את זה רק אם יש לך שדה Role במודל
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddDays(7), // המורה שמה 30 יום, כאן שמתי 7
        Issuer = builder.Configuration["Jwt:Issuer"],
        Audience = builder.Configuration["Jwt:Audience"],
        SigningCredentials = signingCredentials
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

// ==============================================================================
// 6. נתיבים (Endpoints) - Login & Register
// ==============================================================================

// *** תואם לצילום מסך: log in.jpg (Login Func) ***
app.MapPost("/login", async (ToDoDbContext db, User loginModel) =>
{
    // 1. בדיקה מול הדאטה-בייס (בדיוק כמו המורה)
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginModel.Username && u.Password == loginModel.Password);

    // 2. בדיקה אם המשתמש נמצא
    if (user != null)
    {
        // 3. יצירת הטוקן באמצעות הפונקציה למעלה
        var tokenString = CreateJWT(user);

        // 4. החזרת תשובה בפורמט שהמורה מחזירה (אובייקט עם שדה Token)
        return Results.Ok(new { token = tokenString });
    }

    // 5. אם לא נמצא - מחזירים שגיאה
    return Results.Unauthorized();
});

// הרשמה (פשוטה)
// הרשמה - עם טוקן מיידי (כמו Login)
app.MapPost("/register", async (ToDoDbContext db, User user) =>
{
    // 1. בדיקה אם המשתמש כבר קיים
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
    if (existingUser != null) return Results.BadRequest("User already exists");

    // 2. שמירת המשתמש החדש ב-DB
    db.Users.Add(user);
    await db.SaveChangesAsync();

    // 3. יצירת טוקן מיד (בדיוק כמו Login!)
    var tokenString = CreateJWT(user);

    // 4. החזרת הטוקן - בדיוק כמו בהתחברות
    return Results.Ok(new { token = tokenString });
});


// // ==============================================================================
// // 7. נתיבי המשימות (Items) - מוגנים ע"י RequireAuthorization
// // ==============================================================================

// app.MapGet("/", () => "API is running!");

// app.MapGet("/items", async (ToDoDbContext db) =>
//     await db.Items.ToListAsync()).RequireAuthorization(); // המנעול!

// app.MapPost("/items", async (ToDoDbContext db, Item item) =>
// {
//     db.Items.Add(item);
//     await db.SaveChangesAsync();
//     return Results.Created($"/items/{item.Id}", item);
// }).RequireAuthorization(); // המנעול!

// app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item inputItem) =>
// {
//     var item = await db.Items.FindAsync(id);
//     if (item is null) return Results.NotFound();


//     if (inputItem.Name != null)
//         item.Name = inputItem.Name;

//     if (inputItem.IsComplete != null)
//         item.IsComplete = inputItem.IsComplete;

//     await db.SaveChangesAsync();
//     return Results.NoContent();
// }).RequireAuthorization();

// app.MapDelete("/items/{id}", async (ToDoDbContext db, int id) =>
// {
//     if (await db.Items.FindAsync(id) is Item item)
//     {
//         db.Items.Remove(item);
//         await db.SaveChangesAsync();
//         return Results.Ok(item);
//     }
//     return Results.NotFound();
// }).RequireAuthorization();



// ==============================================================================
// 7. נתיבי המשימות (Items) - מוגנים ומסוננים לפי משתמש
// ==============================================================================

// פונקציית עזר פשוטה - נגדיר אותה כמשתנה (Lambda) כדי למנוע בעיות הקשר
var getUserId = (ClaimsPrincipal user) =>
{
    var idClaim = user.FindFirst("id")?.Value;
    return int.TryParse(idClaim, out int id) ? id : 0;
};

app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) =>
{
    var userId = getUserId(user);
    var tasks = await db.Items.Where(i => i.UserId == userId).ToListAsync();
    return Results.Ok(tasks);
}).RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext db, Item item, ClaimsPrincipal user) =>
{
    item.UserId = getUserId(user);
    db.Items.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
}).RequireAuthorization();

app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item inputItem, ClaimsPrincipal user) =>
{
    var userId = getUserId(user);
    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (item is null) return Results.NotFound("Task not found or unauthorized");

    item.Name = inputItem.Name ?? item.Name;
    item.IsComplete = inputItem.IsComplete ?? item.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/items/{id}", async (ToDoDbContext db, int id, ClaimsPrincipal user) =>
{
    var userId = getUserId(user);
    var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (item is null) return Results.NotFound("Task not found or unauthorized");

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Deleted successfully", Item = item });
}).RequireAuthorization();

app.Run();