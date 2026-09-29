using AutoMapper;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;

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
            //CreateMap<Author, AuthorDTO>();

            // Author ↔ AuthorDTO
            //
            // Раньше было только правило Author → AuthorDTO (Entity в DTO).
            // Добавлен .ReverseMap(), чтобы работало и обратное преобразование
            // AuthorDTO → Author: оно нужно в AuthorService при создании
            // и обновлении автора (_mapper.Map<Author>(dto)).
            CreateMap<Author, AuthorDTO>().ReverseMap();

            // Book ↔ BookDTO
            //
            // Book → BookDTO: используется при получении книг (GET),
            // чтобы возвращать клиенту DTO, а не Entity.
            //
            // BookDTO → Book (через .ReverseMap()): используется в BookService
            // при создании (POST) и обновлении (PUT) книги, когда данные
            // из запроса нужно перенести в Entity.
            CreateMap<Book, BookDTO>().ReverseMap();

            // Genre ↔ GenreDTO
            //
            // Genre → GenreDTO: используется при получении жанров (GET),
            // чтобы возвращать клиенту DTO, а не Entity.
            //
            // GenreDTO → Genre (через .ReverseMap()): используется в GenreService
            // при создании (POST) и обновлении (PUT) жанра, когда данные
            // из запроса нужно перенести в Entity.
            CreateMap<Genre, GenreDTO>().ReverseMap();
        }
    }
}