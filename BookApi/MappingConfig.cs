using AutoMapper;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;

namespace Bookstore.Api
{
    // MappingConfig — класс с настройками AutoMapper.
    //
    // Наследуемся от Profile — специального класса AutoMapper,
    // в котором можно описывать правила преобразования
    // одного типа объекта в другой.
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            // Создаём правило преобразования:
            //
            // Author → AuthorDTO
            //
            // AutoMapper будет автоматически искать свойства
            // с одинаковыми именами и переносить их значения.
            //
            // Например:
            //
            // Author:
            // Id = 1
            // Name = "Толстой"
            //
            // превратится в:
            //
            // AuthorDTO:
            // Id = 1
            // Name = "Толстой"
            CreateMap<Author, AuthorDTO>();
        }
    }
}