using System.Text;
using BLL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApi.HaTang;

var builder = WebApplication.CreateBuilder(args);
var cauHinh = builder.Configuration;

// ---- Ket noi CSDL va thong tin cua hang (BLL / DAL dung chung voi app admin)
HeThongBLL.CauHinh(cauHinh.GetConnectionString("QLBanThoiTrang")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:QLBanThoiTrang trong appsettings.json"),
    cauHinh.GetValue("NhatKySql:InRaConsole", false));
ThongTinCuaHang.Ten = cauHinh["CuaHang:Ten"] ?? ThongTinCuaHang.Ten;
ThongTinCuaHang.DiaChi = cauHinh["CuaHang:DiaChi"] ?? "";
ThongTinCuaHang.Sdt = cauHinh["CuaHang:Sdt"] ?? "";

// ---- JWT: token chua ma TK, vai tro, ma NV / ma KH
var jwt = new CauHinhJwt(cauHinh["Jwt:Key"]!, cauHinh["Jwt:Issuer"] ?? "QLBanThoiTrang", cauHinh.GetValue("Jwt:SoGio", 12));
builder.Services.AddSingleton(jwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        RoleClaimType = TenClaim.VaiTro, NameClaimType = TenClaim.TenDangNhap
    };
    // Tai khoan bi khoa / nhan vien nghi viec: token cu het hieu luc ngay (khong doi het 12 gio)
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = ctx =>
        {
            var maTk = ctx.Principal?.FindFirst(TenClaim.MaTk)?.Value;
            if (maTk == null || !new TaiKhoanBLL().ConHoatDong(maTk)) ctx.Fail("Tài khoản đã bị khóa");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers(o => o.Filters.Add<BoLocLoi>())
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase)
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = BoLocLoi.LoiDuLieuGuiLen);

// ---- 2 web (React) goi API tu cong khac
var cacNguon = cauHinh.GetSection("Cors:CacNguon").Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:5174" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(cacNguon).AllowAnyHeader().AllowAnyMethod()
    .WithExposedHeaders(DemCauSql.TenHeaderSoCau, DemCauSql.TenHeaderViTri, "Content-Disposition")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "QL Bán Thời Trang API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Đăng nhập ở /api/tai-khoan/dang-nhap/..., dán token vào đây"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseMiddleware<DemCauSql>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }
