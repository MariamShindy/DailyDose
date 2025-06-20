namespace News.Core.Profilers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterModel, RegisterDto>().ReverseMap();
            CreateMap<LoginModel, LoginDto>().ReverseMap();
            CreateMap<ApplicationUser,UserDto>().ReverseMap();
            CreateMap<FavoriteArticleDto,UserFavoriteArticle>().ReverseMap();
            CreateMap<Notification,NotificationDto>().ReverseMap();
            CreateMap<NewsArticleDto, NewsArticle>().ReverseMap();
            CreateMap<Survey,SurveyDto>().ReverseMap();
        }
    }
}
