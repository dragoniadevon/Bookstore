using AutoMapper;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    // Service содержит логику работы с книгами.
    // Получает DTO от Controller, работает с Entity через Repository
    // и возвращает DTO. О HTTP-кодах Service ничего не знает:
    // "не найдено" и "ошибка" он сообщает через null / false.
    internal class BookService : IBaseService<BookDTO>
    {
        // Repository для книг.
        private readonly IBaseRepository<Book> _repository;

        // Repository для авторов и жанров: нужны, чтобы проверять,
        // что AuthorId и GenreId из запроса существуют.
        private readonly IBaseRepository<Author> _authorRepository;
        private readonly IBaseRepository<Genre> _genreRepository;
        private readonly IBaseRepository<Publisher> _publisherRepository;

        // AutoMapper: преобразование Entity <-> DTO.
        private readonly IMapper _mapper;

        // Все зависимости передаёт Dependency Injection.
        public BookService(
            IBaseRepository<Book> repository,
            IBaseRepository<Author> authorRepository,
            IBaseRepository<Genre> genreRepository,
            IBaseRepository<Publisher> publisherRepository,
            IMapper mapper)
        {
            _repository = repository;
            _authorRepository = authorRepository;
            _genreRepository = genreRepository;
            _publisherRepository = publisherRepository;
            _mapper = mapper;
        }

        // Получить все книги: Entity из БД -> список DTO.
        public async Task<IEnumerable<BookDTO>> GetAllAsync()
        {
            var books = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        // Получить книгу по ID. null = не найдена (Controller вернёт 404).
        public async Task<BookDTO?> GetByIdAsync(int id)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null)
            {
                return null;
            }

            return _mapper.Map<BookDTO>(book);
        }

        // Создать книгу.
        // null = указанный автор или жанр не существует (Controller вернёт 400).
        public async Task<BookDTO?> CreateAsync(BookDTO entity)
        {
            if (!await AuthorGenrePublisherExistAsync(entity.AuthorId, entity.GenreId, entity.PublisherId))
            {
                return null;
            }

            // DTO -> Entity
            var book = _mapper.Map<Book>(entity);
            book.Id = 0; // Id генерирует база, а не клиент

            var created = await _repository.CreateAsync(book);
            return _mapper.Map<BookDTO>(created);
        }

        // Полное обновление (PUT).
        // null = книга не найдена ИЛИ автор/жанр не существуют.
        public async Task<BookDTO?> UpdateAsync(BookDTO entity)
        {
            var book = await _repository.GetByIdAsync(entity.Id);
            if (book == null)
            {
                return null;
            }

            if (!await AuthorGenrePublisherExistAsync(entity.AuthorId, entity.GenreId, entity.PublisherId))
            {
                return null;
            }

            // Переносим значения из DTO в уже существующую Entity,
            // чтобы не потерять служебные поля из BaseEntity.
            _mapper.Map(entity, book);

            var updated = await _repository.UpdateAsync(book);
            return _mapper.Map<BookDTO>(updated);
        }

        // Частичное обновление (PATCH): меняем только переданные поля.
        // "Не передано" = null / пустая строка / 0.
        public async Task<BookDTO?> PatchAsync(int id, BookDTO dto)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null)
            {
                return null;
            }

            // Если меняется автор или жанр, сначала проверяем, что они есть.
            if (dto.AuthorId > 0)
            {
                if (await _authorRepository.GetByIdAsync(dto.AuthorId) == null) return null;
                book.AuthorId = dto.AuthorId;
            }

            if (dto.GenreId > 0)
            {
                if (await _genreRepository.GetByIdAsync(dto.GenreId) == null) return null;
                book.GenreId = dto.GenreId;
            }

            if (dto.PublisherId > 0)
            {
                if (await _publisherRepository.GetByIdAsync(dto.PublisherId) == null) return null;
                book.PublisherId = dto.PublisherId;
            }

            // Остальные поля обновляем, только если они переданы.
            if (!string.IsNullOrEmpty(dto.Title)) book.Title = dto.Title;
            if (dto.Description != null) book.Description = dto.Description;
            if (dto.Price > 0) book.Price = dto.Price;
            if (dto.StockQuantity > 0) book.StockQuantity = dto.StockQuantity;
            if (dto.ImageUrl != null) book.ImageUrl = dto.ImageUrl;

            var updated = await _repository.UpdateAsync(book);
            return _mapper.Map<BookDTO>(updated);
        }

        // Удалить книгу. false = книга не найдена (Controller вернёт 404).
        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Проверка: существуют ли и автор, и жанр с такими ID.
        private async Task<bool> AuthorGenrePublisherExistAsync(int authorId, int genreId, int publisherId)
        {
            return await _authorRepository.GetByIdAsync(authorId) != null
                && await _genreRepository.GetByIdAsync(genreId) != null
                && await _publisherRepository.GetByIdAsync(publisherId) != null;
        }
    }
}