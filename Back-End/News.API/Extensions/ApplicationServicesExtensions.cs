using News.Core.Contracts.Repositories;
using News.Core.Contracts.Repositories.UnitOfWork;

namespace News.API.Extensions
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationsService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ImageUploader>();
            services.AddControllers();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISocialMediaService, SocialMediaService>();
            services.AddScoped<ISummarizationService, SummarizationService>();
            services.AddHttpClient<ISummarizationService,SummarizationService>(client => { client.Timeout = TimeSpan.FromMinutes(20); });
            services.AddScoped<ISearchService,SearchService>();
            services.AddHttpClient<ISearchService,SearchService>(client => { client.Timeout = TimeSpan.FromMinutes(20); });
            services.AddScoped<IRecommendationService,RecommendationService>();
            services.AddHttpClient<IRecommendationService,RecommendationService>(client => { client.Timeout = TimeSpan.FromMinutes(20); });
            services.AddHttpClient<ISentimentService, SentimentService>();
            services.AddHostedService<AccountDeletionService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<ISpeechService, SpeechService>();
            services.AddScoped<ITranslationService,TranslationService>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
            services.AddScoped(typeof(ILoggingService), typeof(LoggingService<>));
            services.AddTransient<IMailSettings, EmailSettings>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddMemoryCache();
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddSingleton<IHostedService, ArticleNotificationService>();
            services.AddSingleton(new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new News.Core.Profilers.MappingProfile());
            }).CreateMapper());
           services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", policyOptioons =>
                {
                    policyOptioons.AllowAnyHeader().AllowAnyMethod().WithOrigins(configuration["FrontBaseUrl"]!);
                });
            }
                );
            return services;
        }
        public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
             options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")).EnableSensitiveDataLogging());
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
             {
                 options.Password.RequireDigit = true;
                 options.Password.RequiredLength = 6;
                 options.Password.RequireNonAlphanumeric = false;
                 options.Password.RequireUppercase = true;
                 options.Password.RequireLowercase = false;
             })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
            services.AddHttpClient();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = configuration["Jwt:Issuer"],
                                ValidAudience = configuration["Jwt:Audience"],
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                            };
                        });
            return services;
        }
    }
}
