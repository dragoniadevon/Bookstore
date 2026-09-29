using AutoMapper;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    // Service содержит логику работы с жанрами.
    // О HTTP-кодах он не знает: "не найдено" сообщает через null / false.
    internal class GenreService : IBaseService<GenreDTO>
    {
        // Repository для доступа к БД.
        private readonly IBaseRepository<Genre> _repository;

        // AutoMapper: преобразование Entity <-> DTO.
        private readonly IMapper _mapper;

        // Зависимости передаёт Dependency Injection.
        public GenreService(IBaseRepository<Genre> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Получить все жанры.
        public async Task<IEnumerable<GenreDTO>> GetAllAsync()
        {
            var genres = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<GenreDTO>>(genres);
        }

        // Получить жанр по ID. null = не найден (Controller вернёт 404).
        public async Task<GenreDTO?> GetByIdAsync(int id)
        {
            var genre = await _repository.GetByIdAsync(id);
            if (genre == null)
            {
                return null;
            }

            return _mapper.Map<GenreDTO>(genre);
        }

        // Создать жанр: DTO -> Entity -> сохранить -> DTO.
        public async Task<GenreDTO?> CreateAsync(GenreDTO entity)
        {
            var genre = _mapper.Map<Genre>(entity);
            genre.Id = 0; // Id генерирует база

            var created = await _repository.CreateAsync(genre);
            return _mapper.Map<GenreDTO>(created);
        }

        // Полное обновление (PUT). null = жанр не найден.
        public async Task<GenreDTO?> UpdateAsync(GenreDTO entity)
        {
            var genre = await _repository.GetByIdAsync(entity.Id);
            if (genre == null)
            {
                return null;
            }

            // Переносим значения в существующую Entity,
            // чтобы не потерять служебные поля из BaseEntity.
            _mapper.Map(entity, genre);

            var updated = await _repository.UpdateAsync(genre);
            return _mapper.Map<GenreDTO>(updated);
        }

        // Частичное обновление (PATCH): меняем только переданные поля.
        public async Task<GenreDTO?> PatchAsync(int id, GenreDTO dto)
        {
            var genre = await _repository.GetByIdAsync(id);
            if (genre == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                genre.Name = dto.Name;
            }

            var updated = await _repository.UpdateAsync(genre);
            return _mapper.Map<GenreDTO>(updated);
        }

        // Удалить жанр. false = не найден.
        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}